using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Benchmarks;

[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
//[DisassemblyDiagnoser(maxDepth: 0)]
public class Pgo1
{
    internal interface IValueProducer
    {
        int GetValue();
    }

    class Producer42 : IValueProducer
    {
        public int GetValue() => 42;
    }

    class Producer43 : IValueProducer
    {
        public int GetValue() => 43;
    }

    class Producer44 : IValueProducer
    {
        public int GetValue() => 44;
    }

    private IValueProducer _valueProducer = new Producer42();
    private int _factor = 2;
    static int i = 0;

    [GlobalSetup]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Setup()
    {
        // _valueProducer = new Producer44();
        _valueProducer = (i & 1) == 0 ? new Producer42() : new Producer44();
        i++;
    }

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetValue()
    {
        return _valueProducer.GetValue() * _factor;
    }
}