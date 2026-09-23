using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.NativeAot;
using Microsoft.Win32;
using System.Text.Json;
using static TestModel;

namespace Benchmarks;

internal class Program
{
    public static Job NewJob => Job.Default;

    //[RequiresUnreferencedCode("Calls ClassLibraryForDeserialization.Benchmarks.XmlSerializationBench2.Setup()")]
    //[RequiresDynamicCode("Calls ClassLibraryForDeserialization.Benchmarks.XmlSerializationBench2.Setup()")]
    public static void Main(string[] args)
    {
        var pnrList = new PnrList { Pnrs = PnrFactory.CreateMany(1).ToArray() };
        var json = JsonSerializer.Serialize(pnrList, PnrListSourceGenerationContext.Default.PnrList);

        /*
        JsonSerializationBench jsonSerializationBench = new JsonSerializationBench();
        jsonSerializationBench.Setup();
        JsonSerializationSourceGenBench x = new();
        x.Setup();
        */

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
            .WithAffinity(0x0001) // use only the first available processor
            .AsBaseline();

        var config = DefaultConfig.Instance.AddJob(jobNet10Jit);

        {
            var aotToolchainBase = NativeAotToolchain.CreateBuilder()
                .UseNuGet()
                .IlcInstructionSet("base")
                .DisplayName("AOT base")
                .TargetFrameworkMoniker("net10.0")
                .ToToolchain();

            var jobAotBase = NewJob.WithToolchain(aotToolchainBase)
                .WithId("1 AOT base")
                .WithPowerPlan(guidFix)
                .WithRuntime(NativeAotRuntime.Net10_0)
                .WithAffinity(0x0001);

            config.AddJob(jobAotBase);
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            var aotToolchainSse4_2 = NativeAotToolchain.CreateBuilder()
                .UseNuGet()
                .IlcInstructionSet("avx")
                .DisplayName("AOT sse4.2")
                .TargetFrameworkMoniker("net10.0")
                .ToToolchain();

            var jobAotSse4_2 = NewJob.WithToolchain(aotToolchainSse4_2)
                .WithId("2 AOT sse4.2")
                .WithPowerPlan(guidFix)
                .WithRuntime(NativeAotRuntime.Net10_0)
                .WithAffinity(0x0001);

            config.AddJob(jobAotSse4_2);
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            var aotToolchainAvx2 = NativeAotToolchain.CreateBuilder()
               .UseNuGet()
               .IlcInstructionSet("avx2")
               .DisplayName("AOT avx2")
               .TargetFrameworkMoniker("net10.0")
               .ToToolchain();

            var jobAotAvx2 = NewJob.WithToolchain(aotToolchainAvx2)
                .WithId("3 AOT avx2")
                .WithPowerPlan(guidFix)
                .WithRuntime(NativeAotRuntime.Net10_0)
                .WithAffinity(0x0001);

            config.AddJob(jobAotAvx2);
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            var aotToolchainNative = NativeAotToolchain.CreateBuilder()
                .UseNuGet()
                .IlcInstructionSet("native")
                .DisplayName("AOT native")
                .TargetFrameworkMoniker("net10.0")
                .ToToolchain();

            var jobAotNative = NewJob.WithToolchain(aotToolchainNative)
                .WithId("4 AOT native")
                .WithPowerPlan(guidFix)
                .WithRuntime(NativeAotRuntime.Net10_0)
                .WithAffinity(0x0001);

            // config.AddJob(jobAotNative);
        }

        /*
        var configJitBaseNative = DefaultConfig.Instance.AddJob(jobNet10Jit);
        configJitBaseNative.AddJob(jobAotBase);
        configJitBaseNative.AddJob(jobAotNative);
        */

        BenchmarkRunner.Run<SequenceEqualBench>(config, args);

        BenchmarkRunner.Run<StringBenchOrdinal>(config);
        BenchmarkRunner.Run<StringBenchCurrentCulture>(config);

        BenchmarkRunner.Run<RegexBench1>(config);
        BenchmarkRunner.Run<RegexBench2>(config);

        BenchmarkRunner.Run<LinqBenchmarks>(config);

        BenchmarkRunner.Run<JsonSerializationBench>(config);
        //BenchmarkRunner.Run<JsonSerializationNsj>(config);
        BenchmarkRunner.Run<XmlSerializationBench>(config);

        //BenchmarkRunner.Run<HashesBench>(config, args); 

        // BenchmarkRunner.Run<DeflateStreamBench>(config);

        // BenchmarkRunner.Run<DictionaryBench>(config);
        // BenchmarkRunner.Run<AesBench>(config);
        // BenchmarkRunner.Run<MatrixMultiplication>(config);
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