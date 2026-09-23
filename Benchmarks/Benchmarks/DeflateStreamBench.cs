using BenchmarkDotNet.Attributes;
using Microsoft.IO;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode")]
[MemoryDiagnoser]
public class DeflateStreamBench
{
    private static readonly RecyclableMemoryStreamManager RecyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
    private byte[] content = [];

    [GlobalSetup]
    public void Setup()
    {
        content = File.ReadAllBytes(@"TestData/System.Drawing.Common.xml");
    }

    [Benchmark]
    public void Optimal()
    {
        using var cachedStream = RecyclableMemoryStreamManager.GetStream("1");
        using var compressor = new System.IO.Compression.DeflateStream(cachedStream, System.IO.Compression.CompressionLevel.Optimal);

        compressor.Write(content, 0, content.Length);
    }

    [Benchmark]
    public void SmallestSize()
    {
        using var cachedStream = RecyclableMemoryStreamManager.GetStream("1");
        using var compressor = new System.IO.Compression.DeflateStream(cachedStream, System.IO.Compression.CompressionLevel.SmallestSize);

        compressor.Write(content, 0, content.Length);
    }
}