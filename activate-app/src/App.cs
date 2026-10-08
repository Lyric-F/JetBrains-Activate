using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Microsoft.Win32;

namespace ActivateDesktop
{
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            try {
                AppPaths paths = args.Length == 2 && args[0] == "--test-profile" ? AppPaths.Fixture(args[1]) : AppPaths.Current();
                if (args.Length != 0 && !(args.Length == 2 && args[0] == "--test-profile")) throw new ArgumentException("不支持的启动参数。");
                bool created;
                using (var mutex = new Mutex(true, @"Local\ActivateDesktop-ui-" + Fs.Hash(System.Text.Encoding.UTF8.GetBytes(paths.Roaming)).Substring(0,24),out created)) {
                    if (!created) { MessageBox.Show("Activate 已在运行。", "Activate", MessageBoxButton.OK, MessageBoxImage.Information); return 0; }
                    var app = new Application();
                    app.DispatcherUnhandledException += (s,e) => { MessageBox.Show(e.Exception.Message,"Activate",MessageBoxButton.OK,MessageBoxImage.Error); e.Handled = true; };
                    var controller = new MainController(paths,new Payload());
                    return app.Run(controller.Window);
                }
            } catch (Exception e) { MessageBox.Show("无法启动 Activate：\n" + e.Message,"Activate",MessageBoxButton.OK,MessageBoxImage.Error); return 1; }
        }
    }
    public sealed class MainController
    {
        public Window Window { get; private set; }
        private readonly AppPaths paths;
        private readonly Payload payload;
        private readonly Deployment deployment;
        private List<Installation> installations = new List<Installation>();
        private readonly List<string> manualRoots = new List<string>();
        private readonly object logLock = new object();
        private CancellationTokenSource cancellation;
        private bool busy;
        private bool scanning;
        private bool closingAfterCancel;
        private bool initializing = true;
        private Product Selected { get { return C<ComboBox>("ProductBox").SelectedItem as Product; } }
        private T C<T>(string name) where T : FrameworkElement { return (T)Window.FindName(name); }
        private void Text(string name, string text) { C<TextBlock>(name).Text = text; }
        public MainController(AppPaths paths, Payload payload)
        {
            this.paths = paths; this.payload = payload; deployment = new Deployment(paths,payload,new OriginalScriptRunner(false));
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Activate.MainWindow.xaml")) Window = (Window)XamlReader.Load(resource);
            if (paths.IsFixture) { Window.Title += " · 隔离测试"; Text("EnvironmentLabel","隔离测试环境"); }
            try { string f = Path.Combine(paths.State,"install-roots.json"); if (File.Exists(f)) manualRoots.AddRange(Json.Read<List<string>>(File.ReadAllText(f))); } catch (Exception) { }
            C<ComboBox>("ProductBox").ItemsSource = Product.All;
            C<ComboBox>("ProductBox").SelectedIndex = 0;
            C<ComboBox>("ProductBox").SelectionChanged += (s,e) => { if (!initializing) UpdateView(true); };
            C<Button>("RefreshButton").Click += async (s,e) => await Refresh();
            C<Button>("LocateButton").Click += async (s,e) => await Locate();
            C<Button>("ActivateButton").Click += async (s,e) => { if (busy) { if (cancellation != null) { cancellation.Cancel(); Text("MessageText","正在停止脚本并恢复，请稍候…"); } } else await Apply(); };
            C<Button>("RestoreButton").Click += async (s,e) => await Restore();
            C<Button>("OpenDataButton").Click += (s,e) => { Directory.CreateDirectory(paths.State); Process.Start(new ProcessStartInfo("explorer.exe","\"" + paths.State + "\"") { UseShellExecute = true }); };
            Window.Loaded += async (s,e) => { initializing = false; await Refresh(); };
            Window.Closing += (s,e) => {
                if (!busy) return;
                e.Cancel = true;
                if (cancellation != null && MessageBox.Show(Window,"操作仍在执行。取消并恢复完成后退出？","Activate",MessageBoxButton.YesNo,MessageBoxImage.Question) == MessageBoxResult.Yes) {
                    closingAfterCancel = true; cancellation.Cancel(); Text("MessageText","正在取消并恢复，完成后退出…");
                }
            };
        }
        private async Task Refresh()
        {
            if (busy || scanning) return;
            scanning = true; Controls(); Text("MessageText","正在读取本机产品版本与配置…");
            try { var roots = manualRoots.ToArray(); installations = await Task.Run(() => Discovery.Scan(paths,roots)); UpdateView(true); }
            catch (Exception ex) { Text("MessageText","检测未完成：" + ex.Message); }
            finally { scanning = false; Controls(); }
        }
        private void UpdateView(bool resetMessage)
        {
            Product p = Selected; if (p == null) return;
            try {
                var installed = installations.Where(i => i.ProductId == p.Id).ToList();
                var targets = paths.Targets(p);
                if (installed.Count > 0) {
                    Text("VersionText",installed[0].Version);
                    Text("VersionDetail",installed.Count == 1 ? "Build " + installed[0].Build : "已检测版本：" + String.Join("、",installed.Select(i => i.Version).Distinct()));
                    C<TextBlock>("VersionDetail").ToolTip = String.Join("\n",installed.Select(i => i.Path));
                } else if (targets.Count > 0) {
                    Text("VersionText",Path.GetFileName(targets[0]).Replace(p.Software,""));
                    Text("VersionDetail","根据用户配置目录推断；可定位安装以读取完整版本");
                    C<TextBlock>("VersionDetail").ToolTip = String.Join("\n",targets);
                } else { Text("VersionText","未检测到"); Text("VersionDetail","可定位安装目录，或先启动一次该产品"); }
                var errors = targets.Count > 0 ? payload.VerifyDeployment(paths,p,targets) : new List<string>();
                bool configured = targets.Count > 0 && errors.Count == 0;
                bool partial = targets.Any(t => File.Exists(Path.Combine(t,p.Key))) || Directory.Exists(paths.Agent(p));
                Text("StatusText",configured ? "配置已部署" : (targets.Count == 0 ? "等待初始化" : (partial ? "待检查" : "未部署")));
                C<Border>("StatusBadge").Background = (Brush)new BrushConverter().ConvertFromString(configured ? "#EAF5EF" : "#F0F3F6");
                C<TextBlock>("StatusText").Foreground = (Brush)new BrushConverter().ConvertFromString(configured ? "#347451" : "#667587");
                Text("ConfigText",configured ? "运行依赖与启动参数一致" : targets.Count == 0 ? "尚无可处理的用户配置" : partial ? "存在文件，尚未通过完整校验" : "尚未写入本包配置");
                C<TextBlock>("ConfigText").ToolTip = errors.Count == 0 ? null : String.Join("\n",errors.Distinct());
                string localKey = targets.Select(t => Path.Combine(t,p.Key)).FirstOrDefault(File.Exists);
                byte[] key = localKey == null ? payload.Read(p.Key) : File.ReadAllBytes(localKey);
                var license = LicenseInfo.Parse(key,p);
                Text("LicenseText",localKey == null ? "内置文件 · 尚未部署" : (Fs.Hash(key) == payload.Hash(p.Key) ? "本地文件 · 与内置一致" : "本地文件 · 与内置不同"));
                Text("LicenseDateText",license.Date);
                Text("LicenseDetail",(localKey == null ? "显示内置许可记录" : "读取 " + Path.GetFileName(Path.GetDirectoryName(localKey)) + " 的许可记录") + "；日期未验签，不代表 IDE 认定的有效期。");
                C<TextBlock>("LicenseText").ToolTip = "许可名称：" + license.Name;
                Text("TargetText",targets.Count == 0 ? "请先启动并关闭 " + p.Name + "，再刷新检测。" : "原脚本将处理 " + targets.Count + " 个配置目录：" + String.Join("、",targets.Select(Path.GetFileName)));
                if (resetMessage) {
                    var last = deployment.Latest(p);
                    Text("MessageText",last != null && last.Status != "Applied" ? "检测到未完成的操作，请先恢复上次配置。" :
                        configured ? "配置已就绪；实际授权结果请在 IDE 内确认。" : targets.Count == 0 ? "等待产品生成用户配置目录。" : "执行前请退出该产品；原脚本弹窗需手动关闭。");
                }
            } catch (Exception ex) {
                Text("StatusText","读取失败"); Text("ConfigText","暂时无法读取"); Text("LicenseText","—"); Text("LicenseDateText","—");
                Text("MessageText","读取配置失败：" + ex.Message);
            }
            Controls();
        }
        private void Controls()
        {
            C<ComboBox>("ProductBox").IsEnabled = !busy && !scanning;
            C<Button>("RefreshButton").IsEnabled = !busy && !scanning;
            C<Button>("LocateButton").IsEnabled = !busy && !scanning;
            C<Button>("ActivateButton").Content = busy ? "取消操作" : "开始激活";
            bool targetExists = false, hasBackup = false;
            try { if (Selected != null) { targetExists = paths.Targets(Selected).Count > 0; hasBackup = deployment.Latest(Selected) != null; } } catch (Exception) { }
            C<Button>("ActivateButton").IsEnabled = !scanning && (busy ? cancellation != null : targetExists);
            C<Button>("RestoreButton").IsEnabled = !busy && !scanning && hasBackup;
            C<ProgressBar>("Progress").Visibility = busy || scanning ? Visibility.Visible : Visibility.Collapsed;
            C<ProgressBar>("Progress").IsIndeterminate = busy || scanning;
        }
        private void Log(string text)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + text;
            lock (logLock) {
                try { string dir = Path.Combine(paths.State,"logs"); Directory.CreateDirectory(dir); File.AppendAllText(Path.Combine(dir,DateTime.Today.ToString("yyyy-MM-dd") + ".log"),line + Environment.NewLine,System.Text.Encoding.UTF8); } catch (Exception) { }
            }
            Window.Dispatcher.BeginInvoke(new Action(() => { C<TextBox>("LogText").AppendText(line + Environment.NewLine); C<TextBox>("LogText").ScrollToEnd(); Text("MessageText",text); }));
        }
        private async Task Apply()
        {
            Product p = Selected; if (p == null) return;
            busy = true; cancellation = new CancellationTokenSource(); Controls();
            try {
                var result = await Task.Run(() => deployment.Apply(p,cancellation.Token,Log));
                UpdateView(false); Text("MessageText",result.Message);
                if (!result.Success) C<Expander>("LogExpander").IsExpanded = true;
            } catch (Exception ex) { Text("MessageText",ex.Message); C<Expander>("LogExpander").IsExpanded = true; }
            finally { cancellation.Dispose(); cancellation = null; busy = false; Controls(); if (closingAfterCancel) Window.Close(); }
        }
        private async Task Restore()
        {
            Product p = Selected; if (p == null) return;
            if (MessageBox.Show(Window,"将恢复到最近一次执行前的配置与运行文件。若执行后存在其他修改，程序会停止自动恢复。\n\n继续恢复 " + p.Name + "？","恢复上次配置",MessageBoxButton.YesNo,MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            busy = true; Controls();
            try { await Task.Run(() => deployment.RestoreLatest(p,Log)); UpdateView(false); Text("MessageText","已恢复到上次执行前，内容校验通过。"); }
            catch (Exception ex) { Text("MessageText",ex.Message); C<Expander>("LogExpander").IsExpanded = true; }
            finally { busy = false; Controls(); }
        }
        private async Task Locate()
        {
            var dialog = new OpenFileDialog { Title = "选择已安装的 JetBrains 产品启动程序", Filter = "IDE 启动程序 (*.exe)|*.exe|产品信息 (product-info.json)|product-info.json", CheckFileExists = true };
            if (dialog.ShowDialog(Window) != true) return;
            string directory = Path.GetDirectoryName(dialog.FileName);
            var installation = Discovery.ReadInstallation(directory) ?? Discovery.ReadInstallation(Path.GetDirectoryName(directory));
            if (installation == null) { Text("MessageText","未识别该安装位置，请选择产品 bin 目录中的启动程序。"); return; }
            if (!manualRoots.Contains(installation.Path,StringComparer.OrdinalIgnoreCase)) manualRoots.Add(installation.Path);
            try { Json.Save(Path.Combine(paths.State,"install-roots.json"),manualRoots); } catch (Exception ex) { Log("安装位置未能保存：" + ex.Message); }
            C<ComboBox>("ProductBox").SelectedItem = Product.All.First(p => p.Id == installation.ProductId);
            await Refresh();
        }
    }
}
