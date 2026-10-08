using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ActivateDesktop;

public static class IntegrationTests
{
    private static string runRoot;
    private static Payload payload;
    private static List<object> results = new List<object>();
    private static int failures;
    public static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        runRoot = Path.Combine(Path.GetTempPath(),"ActivateDesktop-tests",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(runRoot); payload = new Payload();
        Console.WriteLine("TEST_ROOT=" + runRoot);
        foreach (Product p in Product.All) {
            Product current = p;
            Test("original-script-" + p.Id,() => OriginalFlow(current));
            Test("license-info-" + p.Id,() => {
                LicenseInfo info = LicenseInfo.Parse(payload.Read(current.Key),current);
                Assert(info.Date == "2023-07-01" || info.Date == "2025-08-01", "License date did not parse: " + info.Date);
            });
        }
        Test("no-target-does-not-touch-profile",() => {
            AppPaths paths = Profile("no-target"); var before = Fs.Fingerprint(paths.Roaming);
            var result = Engine(paths,new OriginalScriptRunner(true)).Apply(Product.All[0],CancellationToken.None,Ignore);
            Assert(!result.Success && result.Backup == null && result.Work == null,"No-target apply must fail before snapshot/extract");
            Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"No-target profile changed");
        });
        Test("false-success-rolls-back",() => FailureFlow("false-success",new NoopRunner(),false));
        Test("partial-failure-rolls-back",() => FailureFlow("partial-failure",new PartialRunner(false),false));
        Test("cancel-after-write-rolls-back",() => FailureFlow("cancel-partial",new PartialRunner(true),true));
        Test("pre-canceled-does-not-touch-profile",() => {
            AppPaths paths = Profile("pre-cancel"); Seed(paths,Product.All[0]); var before = Fs.Fingerprint(paths.Roaming);
            var cts = new CancellationTokenSource(); cts.Cancel();
            var result = Engine(paths,new OriginalScriptRunner(true)).Apply(Product.All[0],cts.Token,Ignore);
            Assert(result.Canceled && result.Work == null && result.Backup == null,"Pre-cancel did not stop before writes");
            Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Pre-cancel profile changed");
        });
        Test("restore-protects-later-user-edit",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("later-edit"); Seed(paths,p); var engine = Engine(paths,new OriginalScriptRunner(true));
            Assert(engine.Apply(p,CancellationToken.None,Ignore).Success,"Initial apply failed");
            string vm = Path.Combine(paths.Targets(p)[0],p.VmOptions); File.AppendAllText(vm,"\r\n-Xmx4096m");
            var afterEdit = Fs.Fingerprint(paths.Roaming);
            bool refused = false; try { engine.RestoreLatest(p,Ignore); } catch (IOException) { refused = true; }
            Assert(refused && Fs.Equal(afterEdit,Fs.Fingerprint(paths.Roaming)),"Restore overwrote a later edit");
        });
        Test("reapply-restores-one-transaction-at-a-time",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("reapply"); Seed(paths,p); var before = Fs.Fingerprint(paths.Roaming); var engine = Engine(paths,new OriginalScriptRunner(true));
            Assert(engine.Apply(p,CancellationToken.None,Ignore).Success,"First apply failed"); var first = Fs.Fingerprint(paths.Roaming);
            Assert(engine.Apply(p,CancellationToken.None,Ignore).Success,"Second apply failed");
            engine.RestoreLatest(p,Ignore); Assert(Fs.Equal(first,Fs.Fingerprint(paths.Roaming)),"Second transaction restore mismatch");
            engine.RestoreLatest(p,Ignore); Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"First transaction restore mismatch");
        });
        Test("installation-metadata-and-version-order",() => {
            AppPaths paths = Profile("discovery");
            foreach (string version in new[] { "2025.9", "2025.10" }) {
                string dir = Path.Combine(paths.FixtureRoot,"Installs","IDEA-" + version); Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir,"product-info.json"),Json.Write(new {productCode="IU",version=version,buildNumber="test",dataDirectoryName="IntelliJIdea"+version}));
            }
            var found = Discovery.Scan(paths,new string[0]);
            Assert(found.Count == 2 && found[0].Version == "2025.10" && found[0].ProductId == "idea","Metadata detection/order mismatch");
        });
        Test("temp-path-guard",() => {
            AppPaths paths = Profile("guard"); string sibling = Path.Combine(Path.GetDirectoryName(paths.Temp),"must-stay"); Directory.CreateDirectory(sibling);
            bool refused = false; try { Fs.DeleteTree(paths.Temp,sibling); } catch (IOException) { refused = true; }
            Assert(refused && Directory.Exists(sibling),"Out-of-scope cleanup was not refused");
        });
        Test("first-install-restores-absent-files",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("fresh");
            Directory.CreateDirectory(Path.Combine(paths.JetBrains,p.Software+"2025.3"));
            var before = Fs.Fingerprint(paths.Roaming); var engine = Engine(paths,new OriginalScriptRunner(true));
            Assert(engine.Apply(p,CancellationToken.None,Ignore).Success,"Fresh apply failed");
            engine.RestoreLatest(p,Ignore);
            Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Restore left newly-created license, VM options or Agent files");
        });
        Test("unconfirmed-process-exit-preserves-recovery-material",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("unconfirmed-exit"); Seed(paths,p);
            var before = Fs.Fingerprint(paths.Roaming); var engine = Engine(paths,new UnconfirmedExitRunner());
            var result = engine.Apply(p,CancellationToken.None,Ignore);
            Assert(!result.Success && !result.RolledBack && !result.TempCleaned && Directory.Exists(result.Work),"Unconfirmed process exit triggered destructive cleanup");
            var retry = engine.Apply(p,CancellationToken.None,Ignore);
            Assert(!retry.Success && retry.Backup == null && retry.Work == null,"An unresolved transaction did not block reapply");
            // The simulated runner has no live process, so recovery is now safe.
            engine.RestoreLatest(p,Ignore);
            Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Manual recovery after a terminated runner failed");
            Fs.DeleteTree(paths.Temp,result.Work);
        });
        Test("damaged-backup-does-not-overwrite-current-files",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("damaged-backup"); Seed(paths,p); var engine = Engine(paths,new OriginalScriptRunner(true));
            var result = engine.Apply(p,CancellationToken.None,Ignore); Assert(result.Success,"Apply failed");
            var current = Fs.Fingerprint(paths.Roaming);
            File.AppendAllText(Path.Combine(result.Backup,"files","001"),"damaged");
            bool refused = false; try { engine.RestoreLatest(p,Ignore); } catch (IOException) { refused = true; }
            Assert(refused && Fs.Equal(current,Fs.Fingerprint(paths.Roaming)),"A damaged backup overwrote current files");
        });
        Test("real-wscript-cancel-terminates-child-and-restores",() => {
            Product p = Product.All[0]; AppPaths paths = Profile("real-cancel"); Seed(paths,p);
            var before = Fs.Fingerprint(paths.Roaming); var cts = new CancellationTokenSource(); cts.CancelAfter(1500);
            var result = Engine(paths,new OriginalScriptRunner(false)).Apply(p,cts.Token,Ignore);
            Assert(result.Canceled && result.RolledBack && result.TempCleaned,"The real wscript child was not canceled and cleaned up");
            Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Real script cancellation left modified configuration");
            cts.Dispose();
        });
        string report = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(runRoot,"results.json");
        Json.Save(report,new {createdUtc=DateTime.UtcNow.ToString("o"),testRoot=runRoot,passed=results.Count-failures,failed=failures,cases=results,
            productionProfileModified=false,jarExecuted=false,originalVbsExecuted=true,scriptHost="cscript //B //Nologo (isolated APPDATA)"});
        Console.WriteLine("RESULTS=" + report); Console.WriteLine("Passed " + (results.Count-failures) + "/" + results.Count);
        return failures == 0 ? 0 : 1;
    }
    private static void Ignore(string s) { }
    private static Deployment Engine(AppPaths paths,IScriptRunner runner) { return new Deployment(paths,payload,runner); }
    private static void Assert(bool ok,string message) { if (!ok) throw new Exception(message); }
    private static void Test(string name,Action run)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try { run(); results.Add(new {name=name,passed=true,milliseconds=watch.ElapsedMilliseconds}); Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failures++; results.Add(new {name=name,passed=false,milliseconds=watch.ElapsedMilliseconds,error=ex.ToString()}); Console.WriteLine("FAIL " + name + " : " + ex.Message); }
    }
    private static AppPaths Profile(string name)
    {
        string root = Path.Combine(runRoot,name); Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,".activate-test-profile"),"ActivateDesktop isolated test profile v1");
        AppPaths paths = AppPaths.Fixture(root); Directory.CreateDirectory(paths.Roaming); return paths;
    }
    private static void Seed(AppPaths paths,Product p)
    {
        foreach (string version in new[] { "2024.3", "2025.3" }) {
            string config = Path.Combine(paths.JetBrains,p.Software+version); Directory.CreateDirectory(config);
            File.WriteAllText(Path.Combine(config,p.VmOptions),"-Xmx2048m\r\n-Duser.setting=original\r\n");
            if (version == "2024.3") File.WriteAllText(Path.Combine(config,p.Key),"original-license-data");
            File.WriteAllText(Path.Combine(config,"options-must-stay.xml"),"<original/>");
        }
        string unrelated = Path.Combine(paths.JetBrains,"UnrelatedIDE2025.1"); Directory.CreateDirectory(unrelated); File.WriteAllText(Path.Combine(unrelated,"settings.json"),"untouched");
        Directory.CreateDirectory(Path.Combine(paths.Agent(p),"empty-folder")); File.WriteAllText(Path.Combine(paths.Agent(p),"preexisting.txt"),"preserve the previous product directory");
    }
    private static void OriginalFlow(Product p)
    {
        AppPaths paths = Profile(p.Id); Seed(paths,p); var before = Fs.Fingerprint(paths.Roaming);
        var engine = Engine(paths,new OriginalScriptRunner(true)); var log = new List<string>();
        var result = engine.Apply(p,CancellationToken.None,s => log.Add(s));
        Json.Save(Path.Combine(paths.FixtureRoot,"apply-result.json"),new { result=result, log=log });
        Assert(result.Success,result.Message);
        Assert(result.TempCleaned && !Directory.Exists(result.Work),"Temporary extraction remains");
        Assert(File.Exists(Path.Combine(paths.Agent(p),"active-agt.jar")),"Persistent Agent was deleted with the temp directory");
        Assert(payload.VerifyDeployment(paths,p,paths.Targets(p)).Count == 0,"Post-deployment verification failed");
        foreach (string target in paths.Targets(p)) {
            string vm = File.ReadAllText(Path.Combine(target,p.VmOptions));
            Assert(!vm.Contains("Xmx2048m"),"Original script overwrite semantics were changed");
            Assert(!vm.Contains(result.Work),"VM options point to the temporary source directory");
        }
        Assert(File.ReadAllText(Path.Combine(paths.JetBrains,"UnrelatedIDE2025.1","settings.json"))=="untouched","Unrelated product modified");
        engine.RestoreLatest(p,Ignore);
        Assert(Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Restore did not reproduce the original profile content/tree");
    }
    private static void FailureFlow(string name,IScriptRunner runner,bool canceled)
    {
        AppPaths paths = Profile(name); Seed(paths,Product.All[0]); var before = Fs.Fingerprint(paths.Roaming);
        var result = Engine(paths,runner).Apply(Product.All[0],CancellationToken.None,Ignore);
        Assert(!result.Success && result.RolledBack && result.Canceled==canceled,"Failure/cancellation was not rolled back");
        Assert(result.TempCleaned && Fs.Equal(before,Fs.Fingerprint(paths.Roaming)),"Failure left changed data or temporary resources");
    }
    private sealed class NoopRunner : IScriptRunner
    { public void Run(AppPaths paths,Product p,string work,CancellationToken token,Action<string> log) { } }
    private sealed class UnconfirmedExitRunner : IScriptRunner
    {
        public void Run(AppPaths paths,Product p,string work,CancellationToken token,Action<string> log)
        {
            File.WriteAllText(Path.Combine(paths.Targets(p)[0],p.VmOptions),"partial write");
            throw new ScriptStillRunningException("simulated unconfirmed process termination");
        }
    }
    private sealed class PartialRunner : IScriptRunner
    {
        private bool canceled; public PartialRunner(bool canceled) { this.canceled=canceled; }
        public void Run(AppPaths paths,Product p,string work,CancellationToken token,Action<string> log)
        {
            File.WriteAllText(Path.Combine(paths.Targets(p)[0],p.VmOptions),"partial write");
            Fs.DeleteTree(paths.Roaming,paths.Agent(p)); Directory.CreateDirectory(paths.Agent(p)); File.WriteAllText(Path.Combine(paths.Agent(p),"partial.jar"),"partial");
            if (canceled) throw new OperationCanceledException(); throw new IOException("injected I/O failure");
        }
    }
}
