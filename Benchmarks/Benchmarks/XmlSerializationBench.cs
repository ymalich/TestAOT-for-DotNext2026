using BenchmarkDotNet.Attributes;
using Microsoft.IO;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using static TestModel;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode", "Code Size")]
[MemoryDiagnoser]
public class XmlSerializationBench
{
    private string Xml { get; set; } = string.Empty;
    private XmlSerializer? XmlSerializer { get; set; }
    public PnrList? PnrList { get; set; }
    public PnrList? PnrList2 { get; set; }

    public string? Res { get; set; }

    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();

    [GlobalSetup]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<FlightSegment>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<HotelStay>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<CarRental>))]
    [RequiresUnreferencedCode("Uses XmlSerializer which generates code at runtime.")]
    [RequiresDynamicCode("Uses XmlSerializer which generates code at runtime.")]
    public void Setup()
    {
        PnrList = new PnrList { Pnrs = PnrFactory.CreateMany(100).ToArray() };
        XmlSerializer = new XmlSerializer(typeof(PnrList));

        var stringWriter = new StringWriter();
        XmlSerializer?.Serialize(stringWriter, PnrList);
        Xml = stringWriter.ToString();
        PnrList2 = XmlSerializer?.Deserialize(new StringReader(Xml)) as PnrList;
        if (PnrList2?.Pnrs.Length != 100)
        {
            throw new Exception("Setup failed: Deserialized PnrList2 has fewer than 100 Pnrs.");
        }
    }

    [Benchmark]
    [RequiresUnreferencedCode("Uses XmlSerializer which generates code at runtime.")]
    [RequiresDynamicCode("Uses XmlSerializer which generates code at runtime.")]
    public void SerializeXmlInStringWriter()
    {
        var stringWriter = new StringWriter();
        XmlSerializer?.Serialize(stringWriter, PnrList);
        var str = stringWriter.ToString();
        if (string.IsNullOrEmpty(str))
        {
            throw new Exception("Serialization failed in SerializeXmlStringWriter.");
        }

        Res = str;
    }

    [Benchmark]
    [RequiresUnreferencedCode("Uses XmlSerializer which generates code at runtime.")]
    [RequiresDynamicCode("Uses XmlSerializer which generates code at runtime.")]
    public void DeserializeXml()
    {
        PnrList2 = XmlSerializer?.Deserialize(new StringReader(Xml)) as PnrList;
    }

    ////[Benchmark]
    [RequiresDynamicCode("Uses XmlSerializer which generates code at runtime.")]
    [RequiresUnreferencedCode("Uses XmlSerializer which generates code at runtime.")]
    public void SerializeXmlInRecMemStream()
    {
        using var cachedStream = RecyclableMemoryStreamManager.GetStream("1");
        XmlSerializer?.Serialize(cachedStream, PnrList);
    }
}