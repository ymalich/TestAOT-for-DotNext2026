using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Benchmarks;

[MemoryDiagnoser(true)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class DictionaryBench
{
    private ConcurrentDictionary<string, string> _dict = new();

    private ConcurrentDictionary<int, int> _dict1 = new();

    private long _t;
    private int _index;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _dict = new();
        _index = 0;
    }

    ////[Benchmark]
    public void ConcurrentDictionaryAddRemove()
    {
        _dict["1"] = "2";
        _dict.TryRemove("1", out _);
    }

    [Benchmark]
    public void ConcurrentDictionaryAddGet1()
    {
        _index++;
        _t += _dict1.GetOrAdd(_index, x => x);
        _dict1.TryGetValue(_index-1, out var t);
        _t += t;
        if (_index % (16 * 1024) == 0)
        {
            _dict1.Clear();
            _index = 0;
        }
    }
}