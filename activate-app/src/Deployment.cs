using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ActivateDesktop
{
    public sealed class BackupEntry
    {
        public string Kind { get; set; }
        public string Folder { get; set; }
        public Dictionary<string,string> Before { get; set; }
        public Dictionary<string,string> After { get; set; }
    }
    public sealed class BackupRecord
    {
        public int Schema { get; set; }
        public string Id { get; set; }
        public string Product { get; set; }
        public string Roaming { get; set; }
        public string CreatedUtc { get; set; }
        public string Status { get; set; }
        public List<BackupEntry> Entries { get; set; }
    }
    public sealed class OperationResult
    {
        public bool Success;
        public bool Canceled;
        public bool RolledBack;
        public string Message;
        public string Backup;
        public string Work;
        public bool TempCleaned;
    }
    public interface IScriptRunner
    {
        void Run(AppPaths paths, Product product, string work, CancellationToken cancellation, Action<string> log);
    }
    public sealed class ScriptStillRunningException : IOException
    {
        public ScriptStillRunningException(string message) : base(message) { }
    }
    public sealed class OriginalScriptRunner : IScriptRunner
    {
        private readonly bool batch;
        public OriginalScriptRunner(bool batchMode) { batch = batchMode; }
        public void Run(AppPaths paths, Product product, string work, CancellationToken cancellation, Action<string> log)
        {
            if (batch && !paths.IsFixture) throw new InvalidOperationException("批处理测试只允许在隔离测试环境运行。");
            string host = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), batch ? "cscript.exe" : "wscript.exe");
            if (!File.Exists(host)) throw new FileNotFoundException("Windows Script Host 不可用。", host);
            var start = new ProcessStartInfo(host, (batch ? "//B //Nologo " : "//Nologo ") + "\"" + Path.Combine(work, product.Script) + "\"") {
                WorkingDirectory = work, UseShellExecute = false, CreateNoWindow = true,
                WindowStyle = batch ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal, RedirectStandardOutput = batch, RedirectStandardError = batch
            };
            if (paths.IsFixture) start.EnvironmentVariables["APPDATA"] = paths.Roaming;
            cancellation.ThrowIfCancellationRequested();
            using (var process = new Process { StartInfo = start }) {
                if (!process.Start()) throw new IOException("无法启动原始脚本。");
                var deadline = DateTime.UtcNow.AddMinutes(10);
                while (!process.WaitForExit(100)) {
                    if (cancellation.IsCancellationRequested || DateTime.UtcNow > deadline) {
                        try { process.Kill(); }
                        catch (Exception stopError) {
                            bool exited = false; try { exited = process.HasExited; } catch (Exception) { }
                            if (!exited) throw new ScriptStillRunningException("无法确认原脚本已停止：" + stopError.Message);
                        }
                        if (!process.WaitForExit(10000)) throw new ScriptStillRunningException("脚本仍未退出，暂时无法自动恢复。请关闭脚本后从备份恢复。");
                        if (cancellation.IsCancellationRequested) throw new OperationCanceledException(cancellation);
                        throw new TimeoutException("原脚本等待超过 10 分钟，已终止并准备恢复。");
                    }
                }
                if (batch) {
                    string text = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                    if (!String.IsNullOrWhiteSpace(text)) log(text.Trim());
                }
                if (process.ExitCode != 0) throw new IOException("原脚本退出代码：" + process.ExitCode + "。请检查 Windows 的 VBScript 组件是否可用。");
            }
        }
    }
    public sealed class Deployment
    {
        private readonly AppPaths paths;
        private readonly Payload payload;
        private readonly IScriptRunner runner;
        public Deployment(AppPaths paths, Payload payload, IScriptRunner runner) { this.paths = paths; this.payload = payload; this.runner = runner; }
        private string RecordFile(Product p, BackupRecord r) { return Path.Combine(paths.BackupRoot(p), r.Id, "record.json"); }
        private string DataFile(Product p, BackupRecord r, int index) { return Path.Combine(paths.BackupRoot(p), r.Id, "files", index.ToString("D3")); }
        private void Save(Product p, BackupRecord r) { Json.Save(RecordFile(p, r), r); }
        private static string LockName(AppPaths paths) { return @"Local\ActivateDesktop-operation-" + Fs.Hash(System.Text.Encoding.UTF8.GetBytes(paths.Roaming.ToLowerInvariant())).Substring(0,24); }
        private string Target(Product p, BackupEntry e)
        {
            if (e.Kind == "agent" && String.IsNullOrEmpty(e.Folder)) return paths.Agent(p);
            if (String.IsNullOrWhiteSpace(e.Folder) || e.Folder.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 || e.Folder == "." || e.Folder == ".." || e.Folder.IndexOf(p.Software, StringComparison.Ordinal) < 0)
                throw new InvalidDataException("备份中的产品目录无效。");
            if (e.Kind != "vm" && e.Kind != "key") throw new InvalidDataException("未知备份条目。");
            return Path.Combine(paths.JetBrains, e.Folder, e.Kind == "vm" ? p.VmOptions : p.Key);
        }
        public BackupRecord Latest(Product product)
        {
            string root = paths.BackupRoot(product);
            if (!Directory.Exists(root)) return null;
            foreach (string directory in Directory.GetDirectories(root).OrderByDescending(d => d, StringComparer.Ordinal)) {
                try {
                    string file = Path.Combine(directory, "record.json");
                    if (!File.Exists(file)) continue;
                    var r = Json.Read<BackupRecord>(File.ReadAllText(file));
                    ValidateRecord(product, r);
                    if (r.Status == "Applied" || r.Status == "Pending" || r.Status == "RollbackFailed") return r;
                } catch (Exception) { }
            }
            return null;
        }
        private void ValidateRecord(Product p, BackupRecord r)
        {
            if (r == null || r.Schema != 1 || r.Product != p.Id || !Regex.IsMatch(r.Id ?? "", @"^\d{8}T\d{9}-[a-f0-9]{32}$") ||
                !String.Equals(r.Roaming, paths.Roaming, StringComparison.OrdinalIgnoreCase) || r.Entries == null || r.Entries.Count < 3)
                throw new InvalidDataException("备份记录无效或来自其他用户目录。");
            foreach (var e in r.Entries) { Target(p,e); if (e.Before == null) throw new InvalidDataException("备份摘要缺失。"); }
        }
        private BackupRecord Snapshot(Product p, List<string> targets, Action<string> log)
        {
            var r = new BackupRecord { Schema = 1, Id = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N"),
                Product = p.Id, Roaming = paths.Roaming, CreatedUtc = DateTime.UtcNow.ToString("o"), Status = "Pending", Entries = new List<BackupEntry>() };
            r.Entries.Add(new BackupEntry { Kind = "agent" });
            foreach (string dir in targets) {
                r.Entries.Add(new BackupEntry { Kind = "vm", Folder = Path.GetFileName(dir) });
                r.Entries.Add(new BackupEntry { Kind = "key", Folder = Path.GetFileName(dir) });
            }
            Fs.NoReparse(paths.State);
            for (int i = 0; i < r.Entries.Count; i++) {
                var e = r.Entries[i]; string target = Target(p,e), backup = DataFile(p,r,i);
                Fs.NoReparse(target); e.Before = Fs.Fingerprint(target);
                if (File.Exists(target)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(target, backup); }
                else if (Directory.Exists(target)) Fs.CopyTree(target,backup);
                if (!Fs.Equal(e.Before, Fs.Fingerprint(backup))) throw new IOException("备份内容校验失败，原脚本尚未执行。");
            }
            Save(p,r); log("执行前备份已校验：" + Path.GetDirectoryName(RecordFile(p,r))); return r;
        }
        private void RestoreRecord(Product p, BackupRecord r, bool checkChanges, Action<string> log)
        {
            ValidateRecord(p,r);
            for (int i = 0; i < r.Entries.Count; i++) {
                var e = r.Entries[i]; string target = Target(p,e);
                Fs.NoReparse(target);
                if (!Fs.Equal(e.Before, Fs.Fingerprint(DataFile(p,r,i)))) throw new IOException("备份已损坏，未修改当前配置。");
                if (checkChanges && r.Status == "Applied" && e.After != null && !Fs.Equal(e.After,Fs.Fingerprint(target)))
                    throw new IOException("执行后有文件被其他程序修改，已停止自动恢复。请从备份手动核对：" + target);
            }
            for (int i = 0; i < r.Entries.Count; i++) {
                var e = r.Entries[i]; string target = Target(p,e), backup = DataFile(p,r,i);
                if (Directory.Exists(target)) Fs.DeleteTree(paths.Roaming, target);
                else if (File.Exists(target)) { File.SetAttributes(target,FileAttributes.Normal); File.Delete(target); }
                if (File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(backup,target); }
                else if (Directory.Exists(backup)) Fs.CopyTree(backup,target);
                if (!Fs.Equal(e.Before,Fs.Fingerprint(target))) throw new IOException("恢复后的内容校验失败：" + target);
            }
            log("原配置与原运行目录已恢复，内容校验通过。");
        }
        private static void TakeLock(Mutex mutex)
        {
            bool acquired; try { acquired = mutex.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) throw new InvalidOperationException("另一个操作正在执行，请稍后重试。");
        }
        public OperationResult Apply(Product p, CancellationToken cancellation, Action<string> log)
        {
            using (var mutex = new Mutex(false,LockName(paths))) {
                TakeLock(mutex);
                try { return ApplyLocked(p,cancellation,log); } finally { mutex.ReleaseMutex(); }
            }
        }
        private OperationResult ApplyLocked(Product p, CancellationToken cancellation, Action<string> log)
        {
            var result = new OperationResult(); BackupRecord backup = null; bool scriptStarted = false, scriptStillRunning = false;
            try {
                var previous = Latest(p);
                if (previous != null && (previous.Status == "Pending" || previous.Status == "RollbackFailed"))
                    throw new IOException("检测到未完成的操作，请先使用“恢复上次配置”。");
                List<string> targets = paths.Targets(p);
                if (targets.Count == 0) throw new IOException("未找到该产品的用户配置目录。请先启动并关闭 " + p.Name + "，再刷新。");
                if (!paths.IsFixture && Discovery.IsRunning(p)) throw new IOException("请先退出所有正在运行的 " + p.Name + "，再执行。");
                Fs.NoReparse(paths.JetBrains); Fs.NoReparse(paths.Agent(p));
                foreach (string target in targets) Fs.NoReparse(target);
                cancellation.ThrowIfCancellationRequested();
                log("按原脚本处理 " + targets.Count + " 个目录：" + String.Join("、",targets.Select(Path.GetFileName)));
                backup = Snapshot(p,targets,log); result.Backup = Path.GetDirectoryName(RecordFile(p,backup));
                cancellation.ThrowIfCancellationRequested();
                result.Work = payload.Extract(paths); log("临时资源已解包并校验，原始脚本内容未修改。");
                if (!targets.SequenceEqual(paths.Targets(p),StringComparer.Ordinal)) throw new IOException("目标目录发生变化，请刷新后重试。");
                cancellation.ThrowIfCancellationRequested(); scriptStarted = true;
                log("原脚本运行中。若出现原脚本弹窗，请关闭弹窗后等待校验。");
                runner.Run(paths,p,result.Work,cancellation,log);
                var errors = payload.VerifyDeployment(paths,p,targets);
                if (errors.Count != 0) throw new IOException("部署校验未通过：" + String.Join("；",errors.Distinct().Take(6)));
                foreach (var e in backup.Entries) e.After = Fs.Fingerprint(Target(p,e));
                backup.Status = "Applied"; Save(p,backup);
                result.Success = true; result.Message = "配置部署完成。请重新打开 IDE，在产品内确认授权状态。";
                log("JAR、插件、许可文件和启动参数校验通过。授权结果仍需在 IDE 内确认。");
            } catch (Exception ex) {
                scriptStillRunning = ex is ScriptStillRunningException;
                result.Canceled = ex is OperationCanceledException;
                result.Message = result.Canceled ? "操作已取消。" : ex.Message; log(result.Message);
                if (backup != null && scriptStillRunning) {
                    backup.Status = "RollbackFailed"; try { Save(p,backup); } catch (Exception) { }
                    result.Message += " 运行文件和临时目录已保留，备份位于：" + result.Backup;
                } else if (backup != null) {
                    try {
                        if (scriptStarted) RestoreRecord(p,backup,false,log);
                        result.RolledBack = true; backup.Status = "RolledBack"; Save(p,backup);
                        result.Message += " 已保留执行前的配置。";
                    } catch (Exception restoreError) {
                        backup.Status = "RollbackFailed"; try { Save(p,backup); } catch (Exception) { }
                        result.Message += " 自动恢复未完成：" + restoreError.Message + "。备份位于：" + result.Backup;
                        log(result.Message);
                    }
                }
            } finally {
                if (result.Work != null && !scriptStillRunning) {
                    try { Fs.DeleteTree(paths.Temp,result.Work); result.TempCleaned = !Directory.Exists(result.Work); log("本次临时目录已清理。"); }
                    catch (Exception ex) { log("临时目录暂未清理：" + result.Work + "；" + ex.Message); result.Message += " 临时目录仍被占用，详情见日志。"; }
                }
            }
            return result;
        }
        public void RestoreLatest(Product p, Action<string> log)
        {
            using (var mutex = new Mutex(false,LockName(paths))) {
                TakeLock(mutex);
                try {
                    if (!paths.IsFixture && Discovery.IsRunning(p)) throw new IOException("请先退出 " + p.Name + " 后恢复。");
                    var record = Latest(p); if (record == null) throw new IOException("没有可恢复的执行记录。");
                    RestoreRecord(p,record,true,log); record.Status = "Restored"; Save(p,record);
                } finally { mutex.ReleaseMutex(); }
            }
        }
    }
}
