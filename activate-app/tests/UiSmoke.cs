using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ActivateDesktop;

// Exercise the public controller and actual embedded XAML from Activate.exe.
// This is an in-process application test; it never controls another desktop app.
public static class UiSmoke
{
    [STAThread]
    public static int Main(string[] args)
    {
        string reportDir = Path.GetFullPath(args[0]);
        string root = Path.Combine(Path.GetTempPath(), "ActivateDesktop-render-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, ".activate-test-profile"), "ActivateDesktop isolated test profile v1");
        AppPaths paths = AppPaths.Fixture(root);
        for (int i = 0; i < Product.All.Length; i++) {
            Product p = Product.All[i];
            string version = i == 1 ? "2025.3.6" : "2025.3";
            Directory.CreateDirectory(Path.Combine(paths.JetBrains, p.Software + version));
            string install = Path.Combine(root, "Installs", p.Id); Directory.CreateDirectory(install);
            Json.Save(Path.Combine(install, "product-info.json"), new { productCode = p.ProductCodes[0], version = version, buildNumber = "253.28294.334", dataDirectoryName = p.Software + version });
        }
        var before = Fs.Fingerprint(paths.Roaming);
        var app = new Application();
        var payload = new Payload();
        var controller = new MainController(paths, payload);
        Window window = controller.Window;
        window.ShowInTaskbar = false;
        int exitCode = 1;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        timer.Tick += (s,e) => {
            try {
                if (!((Button)window.FindName("RefreshButton")).IsEnabled) {
                    if (DateTime.UtcNow > deadline) throw new Exception("Version discovery timed out.");
                    return;
                }
                timer.Stop();
                ComboBox products = (ComboBox)window.FindName("ProductBox");
                if (products.Items.Count != 9) throw new Exception("Expected nine products.");
                var observed = new List<object>();
                for (int i = 0; i < products.Items.Count; i++) {
                    products.SelectedIndex = i; window.UpdateLayout();
                    string version = ((TextBlock)window.FindName("VersionText")).Text;
                    string date = ((TextBlock)window.FindName("LicenseDateText")).Text;
                    if (version != (i == 1 ? "2025.3.6" : "2025.3")) throw new Exception("Version display mismatch for " + Product.All[i].Id);
                    if (date != "2023-07-01" && date != "2025-08-01") throw new Exception("Unparsed license date.");
                    if (!((Button)window.FindName("ActivateButton")).IsEnabled) throw new Exception("Seeded configuration did not enable the action.");
                    observed.Add(new { product = Product.All[i].Name, version = version, licenseDate = date });
                }
                products.SelectedIndex = 0;
                window.UpdateLayout();
                var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(window);
                var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
                Directory.CreateDirectory(reportDir);
                using (var output = File.Create(Path.Combine(reportDir, "ui-preview.png"))) encoder.Save(output);
                if (!Fs.Equal(before, Fs.Fingerprint(paths.Roaming))) throw new Exception("Read-only UI changed configuration.");
                Json.Save(Path.Combine(reportDir, "ui-smoke.json"), new {
                    createdUtc = DateTime.UtcNow.ToString("o"), passed = true,
                    executable = typeof(MainController).Assembly.Location,
                    executableSha256 = Fs.HashFile(typeof(MainController).Assembly.Location),
                    scope = "Actual executable controller and embedded WPF XAML; isolated synthetic installation metadata; no script execution",
                    products = observed, profileUnchanged = true, preview = "ui-preview.png",
                    embeddedFileHashes = payload.Names.ToDictionary(n => n, n => payload.Hash(n))
                });
                Console.WriteLine("PASS actual EXE UI: 9 products, version and license rendering, read-only profile.");
                exitCode = 0;
            } catch (Exception ex) { timer.Stop(); Console.Error.WriteLine(ex); }
            window.Close();
        };
        window.Loaded += (s,e) => timer.Start();
        app.Run(window);
        Fs.DeleteTree(Path.GetTempPath(), root);
        return exitCode;
    }
}
