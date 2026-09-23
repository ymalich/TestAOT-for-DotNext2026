using System.IO.Hashing;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks;

[MemoryDiagnoser(true)]
[HideColumns("StdDev", "Median", "RatioSD")]
////[HardwareCounters(HardwareCounter.BranchMispredictions, HardwareCounter.BranchInstructions, HardwareCounter.CacheMisses)]
////  [RPlotExporter]
public class HashesBench
{
    private SHA256 sha256 = SHA256.Create();
    private SHA1 sha1 = SHA1.Create();

    private byte[] _buffer1 = [];

    [Params(256 * 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        _buffer1 = new byte[N];
        new Random(13).NextBytes(_buffer1);
    }

    ////[Benchmark]
    public byte[] Sha1()
    {
        return sha1.ComputeHash(_buffer1);
    }

    [Benchmark]
    public byte[] Sha256()
    {
        return sha256.ComputeHash(_buffer1);
    }

    [Benchmark]
    public byte[] XxHash_128()
    {
        return XxHash128.Hash(new ReadOnlySpan<byte>(_buffer1));
    }
}