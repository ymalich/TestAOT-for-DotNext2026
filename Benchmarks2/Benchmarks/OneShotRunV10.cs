using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace BenchmarksAOT2.Benchmarks;

//[SimpleJob(RuntimeMoniker.Net10_0)]
public class OneShotRunV10
{
    private string testappFolder = string.Empty;
    private string jitApp = string.Empty;
    private string r2rApp = string.Empty;
    private string r2rSelfContainedApp = string.Empty;
    private string aotBaseApp = string.Empty;
    private string aotNativeApp = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        var root = AppContext.BaseDirectory;
        Console.WriteLine($"ProcessPath: '{root}'");
        while (root != null && !Directory.Exists(Path.Combine(root, "testapp10")))
        {
            root = Path.GetDirectoryName(root);
        }

        if (root == null) 
        {
            throw new Exception("Can't find testapp folder");
        }

        testappFolder = Path.Combine(root, "testapp10");
        Console.WriteLine($"testapp folder: '{testappFolder}'");

        jitApp = Path.Combine(testappFolder, @"net10.0\TestAot2.exe");
        r2rApp = Path.Combine(testappFolder, @"r2r\TestAot2.exe");
        r2rSelfContainedApp = Path.Combine(testappFolder, @"r2r-self-contained\TestAot2.exe");
        aotBaseApp = Path.Combine(testappFolder, @"TestAot2-base.exe");
        aotNativeApp = Path.Combine(testappFolder, @"TestAot2-native.exe");

        if (!File.Exists(jitApp))
        {
            throw new Exception($"Can't find JIT app: '{jitApp}'");
        }

        if (!File.Exists(r2rApp))
        {
            throw new Exception($"Can't find R2R app: '{r2rApp}'");
        }

        if (!File.Exists(r2rSelfContainedApp))
        {
            throw new Exception($"Can't find R2R self-contained app: '{r2rSelfContainedApp}'");
        }

        if (!File.Exists(aotBaseApp))
        {
            throw new Exception($"Can't find AOT base app: '{aotBaseApp}'");
        }

        if (!File.Exists(aotNativeApp))
        {
            throw new Exception($"Can't find AOT native app: '{aotNativeApp}'");
        }
    }

    [Benchmark]
    public int Jit()
    {
        Process process = Process.Start(jitApp!);
        process.WaitForExit();
        return process.ExitCode;
    }

    [Benchmark]
    public int R2r_Simple()
    {
        Process process = Process.Start(r2rApp);
        process.WaitForExit();
        return process.ExitCode;
    }

    [Benchmark]
    public int R2r_SelfContained()
    {
        Process process = Process.Start(r2rSelfContainedApp);
        process.WaitForExit();
        return process.ExitCode;
    }

    [Benchmark]
    public int AotBase()
    {
        Process process = Process.Start(aotBaseApp);
        process.WaitForExit();
        return process.ExitCode;
    }

    [Benchmark]
    public int AotNative()
    {
        Process process = Process.Start(aotNativeApp);
        process.WaitForExit();
        return process.ExitCode;
    }
}