using BenchmarkDotNet.Attributes;

namespace Benchmarks;

// [WarmupCount(3)]
[IterationCount(8)]
[HideColumns("StdDev", "PowerPlanMode")]
public class SequenceEqualBench
{
    private byte[] _buffer1 = Array.Empty<byte>();
    private byte[] _buffer2 = Array.Empty<byte>();

    public volatile bool B;

    [Params(500 * 1024)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        _buffer1 = new byte[N];
        _buffer2 = new byte[N];
    }

    [Benchmark]
    public void SequenceEqual()
    {
        var buf1Span = _buffer1.AsSpan();
        var buf2Span = _buffer2.AsSpan();
        B = buf1Span.SequenceEqual(buf2Span);
    }
}