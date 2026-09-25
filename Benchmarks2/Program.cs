using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarksAOT2.Benchmarks;
using Microsoft.Win32;

namespace ClassLibraryForDeserialization;

internal class Program
{
    public static Job NewJob => Job.Default;

    public static void Main(string[] args)
    {
        // Balanced schema
        var guidFix = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

        var cpuName = GetProcessorName();
        if (cpuName.Contains("Ryzen 5 5600U"))
        {
            guidFix = new Guid("126bd836-ee9b-4b63-95cd-68448d8e5905"); // ryzen
        }

        var jobNet10Jit = NewJob
            .WithPowerPlan(guidFix)
            .WithRuntime(CoreRuntime.Core10_0)
            .WithId("NET 10.0 RyuJIT")
            .AsBaseline();

        var config = DefaultConfig.Instance.AddJob(jobNet10Jit);

        // BenchmarkRunner.Run<Pgo1>();
        // BenchmarkRunner.Run<Pgo2>();

        BenchmarkRunner.Run<OneShotRunV10>(config);
        BenchmarkRunner.Run<OneShotRunV11>(config);
    }

    private static string GetProcessorName()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0\");
            return key?.GetValue("ProcessorNameString")?.ToString() ?? "";
        }

        return string.Empty;
    }
}