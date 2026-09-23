
using BenchmarkDotNet.Attributes;
using Microsoft.IO;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using static TestModel;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode", "Code Size")]
////[DisassemblyDiagnoser]
[MemoryDiagnoser]
public class JsonSerializationBench
{
    private string Json { get; set; } = string.Empty;
    private string? Json2 { get; set; }
    public PnrList? PnrList { get; set; }
    public PnrList? PnrList2 { get; set; }

    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

    public JsonSerializerOptions? JsonSerializerOptionsReflection { get; set; }

    [GlobalSetup]
    [RequiresUnreferencedCode("Uses JsonSerializer which generates code at runtime.")]
    [RequiresDynamicCode("Uses JsonSerializer which generates code at runtime.")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<FlightSegment>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<HotelStay>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<CarRental>))]
    public void Setup()
    {
        JsonSerializerOptionsReflection = new JsonSerializerOptions()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = true
        };

        PnrList = new PnrList { Pnrs = PnrFactory.CreateMany(100).ToArray() };
        Json = JsonSerializer.Serialize(PnrList, PnrListSourceGenerationContext.Default.PnrList);
        Json2 = JsonSerializer.Serialize(PnrList, JsonSerializerOptionsReflection);
        if (Json != Json2)
        {
            throw new Exception("Setup failed: JSON strings do not match.");
        }
    }

    [Benchmark]
    public void SerializeJsonUsingSourceGen()
    {
        Json2 = JsonSerializer.Serialize(PnrList, PnrListSourceGenerationContext.Default.PnrList);
    }


    [Benchmark]
    public void DeserializeJsonUsingSourceGen()
    {
        PnrList = JsonSerializer.Deserialize<PnrList>(Json, PnrListSourceGenerationContext.Default.PnrList);
    }

    [Benchmark]
    [RequiresUnreferencedCode("RequiresUnreferencedCode")]
    [RequiresDynamicCode("RequiresUnreferencedCode")]
    public void SerializeJsonUsingReflection()
    {
        Json2 = JsonSerializer.Serialize(PnrList, JsonSerializerOptionsReflection);
    }

    [Benchmark]
    [RequiresUnreferencedCode("RequiresUnreferencedCode")]
    [RequiresDynamicCode("Calls System.Text.Json.JsonSerializer.Deserialize<TValue>(string, JsonSerializerOptions)")]
    public void DeserializeJsonUsingReflection()
    {
        PnrList = JsonSerializer.Deserialize<PnrList>(Json, JsonSerializerOptionsReflection);
    }

    ////JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
    ////{
    ////    TypeInfoResolver = SourceGenerationContext.Default
    ////    ,
    ////    WriteIndented = true
    ////};
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PnrList))]
internal partial class PnrListSourceGenerationContext : JsonSerializerContext
{
}
