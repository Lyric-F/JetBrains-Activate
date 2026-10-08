using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;

namespace ActivateDesktop
{
    public sealed class Payload
    {
        private readonly Dictionary<string,byte[]> files = new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string,string> hashes;
        public IEnumerable<string> Names { get { return files.Keys; } }
        public byte[] Read(string name) { return (byte[])files[name].Clone(); }
        public string Hash(string name) { return hashes[name]; }
        public Payload()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("Activate.Payload.manifest.json"))
            using (var reader = new StreamReader(stream)) hashes = Json.Read<Dictionary<string,string>>(reader.ReadToEnd());
            using (var stream = assembly.GetManifestResourceStream("Activate.Payload.zip"))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read)) {
                foreach (var entry in archive.Entries) {
                    string name = entry.FullName.Replace('\\', '/');
                    if (!hashes.ContainsKey(name) || files.ContainsKey(name) || name.StartsWith("/") || name.Split('/').Any(p => p == "..") || name.Contains(":"))
                        throw new InvalidDataException("内置资源目录不一致。");
                    using (var input = entry.Open()) using (var output = new MemoryStream()) {
                        input.CopyTo(output); byte[] data = output.ToArray();
                        if (Fs.Hash(data) != hashes[name]) throw new InvalidDataException("内置资源校验失败：" + name);
                        files.Add(name, data);
                    }
                }
            }
            if (files.Count != hashes.Count) throw new InvalidDataException("内置资源缺失。");
            foreach (Product p in Product.All) if (!files.ContainsKey(p.Script) || !files.ContainsKey(p.Key) || !files.ContainsKey(p.VmOptions)) throw new InvalidDataException("缺少产品资源：" + p.Name);
        }
        public string Extract(AppPaths paths)
        {
            string work = Path.Combine(paths.Temp, Guid.NewGuid().ToString("N"));
            Fs.NoReparse(work);
            Directory.CreateDirectory(work);
            try {
                foreach (var file in files) {
                    string target = Path.GetFullPath(Path.Combine(work, file.Key.Replace('/', Path.DirectorySeparatorChar)));
                    if (!Fs.IsWithin(work, target)) throw new IOException("非法资源路径。");
                    Directory.CreateDirectory(Path.GetDirectoryName(target)); File.WriteAllBytes(target, file.Value);
                    if (Fs.HashFile(target) != hashes[file.Key]) throw new IOException("临时资源写入校验失败。");
                }
                return work;
            } catch { Fs.DeleteTree(paths.Temp, work); throw; }
        }
        public List<string> VerifyDeployment(AppPaths paths, Product p, IEnumerable<string> targets)
        {
            var errors = new List<string>();
            foreach (string name in files.Keys.Where(n => n == "active-agt.jar" || n.StartsWith("config/") || n.StartsWith("plugins/"))) {
                string file = Path.Combine(paths.Agent(p), name.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file) || Fs.HashFile(file) != hashes[name]) errors.Add("运行依赖缺失或内容不一致：" + name);
            }
            foreach (string target in targets) {
                string key = Path.Combine(target, p.Key), vm = Path.Combine(target, p.VmOptions);
                if (!File.Exists(key) || Fs.HashFile(key) != hashes[p.Key]) errors.Add(Path.GetFileName(target) + "：许可文件不一致");
                if (!File.Exists(vm)) { errors.Add(Path.GetFileName(target) + "：缺少启动配置"); continue; }
                string[] lines = File.ReadAllLines(vm).Select(s => s.Trim()).ToArray();
                foreach (string line in new[] { "--add-opens=java.base/jdk.internal.org.objectweb.asm=ALL-UNNAMED", "--add-opens=java.base/jdk.internal.org.objectweb.asm.tree=ALL-UNNAMED", "-javaagent:" + paths.Agent(p) + "\\active-agt.jar" })
                    if (!lines.Contains(line)) errors.Add(Path.GetFileName(target) + "：启动参数不匹配");
            }
            return errors;
        }
    }
}
