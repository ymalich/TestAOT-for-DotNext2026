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

        // Balanced schema
        var guidFix = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");

        var cpuName = GetProcessorName();
        if (cpuName.Contains("Ryzen 5 5600U"))
        {
            guidFix = new Guid("126bd836-ee9b-4b63-95cd-68448d8e5905"); // ryzen
        }

        var jobNet11Jit = NewJob
            .WithPowerPlan(guidFix)
            .WithRuntime(CoreRuntime.Core11_0)
            .WithId("NET 11.0 RyuJIT")
            .WithAffinity(0x0001) // use only the first available processor
            .AsBaseline();

        var config = DefaultConfig.Instance.AddJob(jobNet11Jit);

        {
            var aotToolchainBase = CreateNativeAotToolchain("base");

            var jobAotBase = NewJob.WithToolchain(aotToolchainBase)
                .WithId("1 AOT base")
                .WithPowerPlan(guidFix)
                .WithAffinity(0x0001);

            config.AddJob(jobAotBase);
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            var aotToolchainAvx2 = CreateNativeAotToolchain("avx2");

            var jobAotAvx2 = NewJob.WithToolchain(aotToolchainAvx2)
                .WithId("2 AOT avx2")
                .WithPowerPlan(guidFix)
                .WithAffinity(0x0001);

            config.AddJob(jobAotAvx2);
        }

        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            var aotToolchainNative = CreateNativeAotToolchain("native");

            var jobAotNative = NewJob.WithToolchain(aotToolchainNative)
                .WithId("2 AOT native")
                .WithPowerPlan(guidFix)
                .WithAffinity(0x0001);

            config.AddJob(jobAotNative);
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

    private static CsProjNativeAotToolchain CreateNativeAotToolchain(string instructionSet)
    {
        return CsProjNativeAotToolchain.From(
            NativeAotRuntime.Net11_0,
            new NativeAotSettings
            {
                //InstructionSet = instructionSet,
                TargetFrameworkMoniker = "net11.0"
            });
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
