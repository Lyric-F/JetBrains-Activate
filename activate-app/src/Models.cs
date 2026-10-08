using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace ActivateDesktop
{
    public sealed class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Software { get; set; }
        public string Script { get; set; }
        public string VmOptions { get { return Id + "64.exe.vmoptions"; } }
        public string Key { get { return Id + ".key"; } }
        public string LicenseCode { get; set; }
        public string[] ProductCodes { get; set; }
        public override string ToString() { return Name; }

        public static readonly Product[] All = {
            Make("idea", "IntelliJ IDEA", "IntelliJIdea", "IDEA", "II", "IU", "IC"),
            Make("pycharm", "PyCharm", "PyCharm", "PyCharm", "PC", "PY", "PC"),
            Make("webstorm", "WebStorm", "WebStorm", "WebStorm", "WS", "WS"),
            Make("phpstorm", "PhpStorm", "PhpStorm", "PhpStorm", "PS", "PS"),
            Make("goland", "GoLand", "GoLand", "GoLand", "GO", "GO"),
            Make("clion", "CLion", "CLion", "CLion", "CL", "CL"),
            Make("datagrip", "DataGrip", "DataGrip", "DataGrip", "DB", "DB"),
            Make("dataspell", "DataSpell", "DataSpell", "DataSpell", "DS", "DS"),
            Make("rider", "Rider", "Rider", "Rider", "RD", "RD")
        };
        private static Product Make(string id, string name, string software, string script, string code, params string[] productCodes)
        {
            return new Product { Id = id, Name = name, Software = software, Script = script + "激活.vbs", LicenseCode = code, ProductCodes = productCodes };
        }
    }

    public sealed class AppPaths
    {
        public string Roaming { get; private set; }
        public string State { get; private set; }
        public string Temp { get; private set; }
        public string FixtureRoot { get; private set; }
        public bool IsFixture { get { return FixtureRoot != null; } }
        public string JetBrains { get { return Path.Combine(Roaming, "JetBrains"); } }
        public string Agent(Product p) { return Path.Combine(Roaming, p.Software); }
        public string BackupRoot(Product p) { return Path.Combine(State, "backups", p.Id); }
        public static AppPaths Current()
        {
            return new AppPaths {
                Roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                State = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ActivateDesktop"),
                Temp = Path.Combine(Path.GetTempPath(), "ActivateDesktop", "work")
            };
        }
        public static AppPaths Fixture(string root)
        {
            root = Path.GetFullPath(root);
            if (!Fs.IsWithin(Path.GetTempPath(), root) || !File.Exists(Path.Combine(root, ".activate-test-profile")) ||
                File.ReadAllText(Path.Combine(root, ".activate-test-profile")).Trim() != "ActivateDesktop isolated test profile v1")
                throw new InvalidOperationException("测试配置必须位于临时目录，且包含有效的测试标记。");
            Fs.NoReparse(root);
            return new AppPaths { Roaming = Path.Combine(root, "Roaming"), State = Path.Combine(root, "Local", "ActivateDesktop"), Temp = Path.Combine(root, "Temp", "work"), FixtureRoot = root };
        }
        public List<string> Targets(Product p)
        {
            if (!Directory.Exists(JetBrains)) return new List<string>();
            return Directory.GetDirectories(JetBrains).Where(d => Path.GetFileName(d).IndexOf(p.Software, StringComparison.Ordinal) >= 0)
                .OrderByDescending(d => Path.GetFileName(d), StringComparer.Ordinal).ToList();
        }
    }

    public static class Json
    {
        public static string Write(object value) { return new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Serialize(value); }
        public static T Read<T>(string text) { return new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 }.Deserialize<T>(text); }
        public static void Save(string path, object value)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".new";
            File.WriteAllText(temp, Write(value), new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }
    }

    public static class Fs
    {
        public static string Hash(byte[] data)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
        }
        public static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
        public static bool IsWithin(string root, string path)
        {
            return Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        public static void NoReparse(string path)
        {
            string current = Path.GetFullPath(path);
            while (!String.IsNullOrEmpty(current)) {
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException("目标包含符号链接或目录联接，已停止操作：" + current);
                current = Path.GetDirectoryName(current);
            }
        }
        public static void CopyTree(string source, string destination)
        {
            NoReparse(source); NoReparse(destination);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source)) { NoReparse(file); File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true); }
            foreach (string dir in Directory.GetDirectories(source)) CopyTree(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
        public static Dictionary<string,string> Fingerprint(string path)
        {
            var map = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(path)) { NoReparse(path); map["@file"] = HashFile(path); }
            else if (Directory.Exists(path)) { map["@directory"] = ""; FingerprintTree(path, path, map); }
            return map;
        }
        private static void FingerprintTree(string root, string dir, Dictionary<string,string> map)
        {
            NoReparse(dir);
            foreach (string file in Directory.GetFiles(dir)) { NoReparse(file); map["f:" + file.Substring(root.Length + 1)] = HashFile(file); }
            foreach (string child in Directory.GetDirectories(dir)) { map["d:" + child.Substring(root.Length + 1)] = ""; FingerprintTree(root, child, map); }
        }
        public static bool Equal(Dictionary<string,string> a, Dictionary<string,string> b)
        { return a.Count == b.Count && a.All(kv => b.ContainsKey(kv.Key) && b[kv.Key] == kv.Value); }
        public static void DeleteTree(string allowedParent, string path)
        {
            if (!IsWithin(allowedParent, path)) throw new IOException("拒绝清理范围外目录：" + path);
            NoReparse(path);
            if (!Directory.Exists(path)) return;
            // Walk first so a junction nested inside the directory is never followed.
            Fingerprint(path);
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(path, true);
        }
        public static string Text(byte[] bytes)
        {
            // The supplied .key files use FF FF, followed by UTF-16LE text.
            // Preserve the bytes on disk; only accommodate this legacy marker when reading.
            if (bytes.Length >= 4 && bytes[0] == 0xff && bytes[1] == 0xff)
                return Encoding.Unicode.GetString(bytes,2,bytes.Length-2);
            using (var ms = new MemoryStream(bytes)) using (var reader = new StreamReader(ms, Encoding.UTF8, true)) return reader.ReadToEnd();
        }
    }

    public sealed class LicenseInfo
    {
        public string Name = "未解析";
        public string Date = "未解析";
        public static LicenseInfo Parse(byte[] data, Product p)
        {
            var info = new LicenseInfo();
            try {
                foreach (string part in Fs.Text(data).Trim().Split('-')) {
                    try {
                        string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(part)).Trim();
                        if (!decoded.StartsWith("{",StringComparison.Ordinal)) continue;
                        var json = Json.Read<Dictionary<string,object>>(decoded);
                        if (json == null || !json.ContainsKey("products")) continue;
                        object name; if (json.TryGetValue("licenseeName", out name)) info.Name = Convert.ToString(name);
                        foreach (var obj in (IEnumerable)json["products"]) {
                            var item = obj as Dictionary<string,object>;
                            if (item != null && item.ContainsKey("code") && Convert.ToString(item["code"]) == p.LicenseCode && item.ContainsKey("paidUpTo"))
                                info.Date = Convert.ToString(item["paidUpTo"]);
                        }
                        return info;
                    } catch (Exception) { /* A header, certificate or signature segment is not a license JSON object. */ }
                }
            } catch (Exception) { }
            return info;
        }
    }

    public sealed class Installation
    {
        public string ProductId { get; set; }
        public string Version { get; set; }
        public string Build { get; set; }
        public string Path { get; set; }
        public string DataDirectory { get; set; }
    }

    public static class Discovery
    {
        public static Installation ReadInstallation(string directory)
        {
            try {
                string file = System.IO.Path.Combine(directory, "product-info.json");
                if (!File.Exists(file) || new FileInfo(file).Length > 1024 * 1024) return null;
                var data = Json.Read<Dictionary<string,object>>(File.ReadAllText(file));
                string code = Convert.ToString(data["productCode"]);
                Product product = Product.All.FirstOrDefault(p => p.ProductCodes.Contains(code));
                if (product == null) return null;
                return new Installation { ProductId = product.Id, Version = Convert.ToString(data["version"]),
                    Build = data.ContainsKey("buildNumber") ? Convert.ToString(data["buildNumber"]) : "",
                    DataDirectory = data.ContainsKey("dataDirectoryName") ? Convert.ToString(data["dataDirectoryName"]) : "",
                    Path = System.IO.Path.GetFullPath(directory) };
            } catch (Exception) { return null; }
        }
        public static List<Installation> Scan(AppPaths paths, IEnumerable<string> manualRoots)
        {
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (paths.IsFixture) roots.Add(System.IO.Path.Combine(paths.FixtureRoot, "Installs"));
            else {
                roots.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "JetBrains"));
                roots.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "JetBrains"));
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                roots.Add(System.IO.Path.Combine(local, "JetBrains", "Toolbox", "apps"));
                roots.Add(System.IO.Path.Combine(local, "Programs"));
                foreach (DriveInfo drive in DriveInfo.GetDrives()) if (drive.DriveType == DriveType.Fixed && drive.IsReady) {
                    roots.Add(System.IO.Path.Combine(drive.RootDirectory.FullName, "Program Files", "JetBrains"));
                    roots.Add(System.IO.Path.Combine(drive.RootDirectory.FullName, "JetBrains"));
                }
                foreach (RegistryKey hive in new[] { Registry.CurrentUser, Registry.LocalMachine })
                    foreach (string keyPath in new[] { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall" })
                        try { using (RegistryKey key = hive.OpenSubKey(keyPath)) if (key != null) foreach (string sub in key.GetSubKeyNames())
                            using (RegistryKey entry = key.OpenSubKey(sub)) {
                                if (entry == null) continue;
                                string publisher = Convert.ToString(entry.GetValue("Publisher"));
                                string location = Convert.ToString(entry.GetValue("InstallLocation"));
                                if (publisher.IndexOf("JetBrains", StringComparison.OrdinalIgnoreCase) >= 0 && !String.IsNullOrWhiteSpace(location)) roots.Add(location.Trim('"'));
                            }
                        } catch (Exception) { }
            }
            foreach (string directory in manualRoots) roots.Add(directory);
            var found = new Dictionary<string,Installation>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<Tuple<string,int>>();
            foreach (string root in roots) if (Directory.Exists(root)) queue.Enqueue(Tuple.Create(root, 0));
            int count = 0;
            while (queue.Count > 0 && count++ < 2000) {
                var item = queue.Dequeue();
                try {
                    if ((File.GetAttributes(item.Item1) & FileAttributes.ReparsePoint) != 0) continue;
                    Installation install = ReadInstallation(item.Item1);
                    if (install != null) { found[install.Path] = install; continue; }
                    if (item.Item2 >= 4) continue;
                    foreach (string child in Directory.GetDirectories(item.Item1)) queue.Enqueue(Tuple.Create(child, item.Item2 + 1));
                } catch (Exception) { }
            }
            return found.Values.OrderByDescending(i => VersionRank(i.Version), StringComparer.Ordinal).ToList();
        }
        public static string VersionRank(string version)
        {
            return String.Join(".", Regex.Matches(version ?? "", @"\d+").Cast<Match>().Take(5).Select(m => m.Value.PadLeft(8, '0')));
        }
        public static bool IsRunning(Product product)
        {
            foreach (Process process in Process.GetProcesses()) using (process) {
                try { string name = process.ProcessName; if (name.Equals(product.Id + "64", StringComparison.OrdinalIgnoreCase) || name.Equals(product.Id, StringComparison.OrdinalIgnoreCase)) return true; }
                catch (Exception) { }
            }
            return false;
        }
    }
}
