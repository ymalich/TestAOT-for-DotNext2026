using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;


namespace Benchmarks;


[HideColumns("Error", "StdDev", "Median", "RatioSD", "EnvironmentVariables")]
//[DisassemblyDiagnoser]
public class Pgo2
{
    private readonly A _a = new();
    private readonly B _b = new();
    private readonly C _c = new();

    [Benchmark]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Multiple()
    {
        int c = 0;
        c += DoWork(_a);
        c += DoWork(_b);
        c += DoWork(_c);
        return c;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int DoWork(IMyInterface i)
    {
        return i.GetValue();
    }

    private interface IMyInterface { int GetValue(); }
    private class A : IMyInterface { public int GetValue() => 123; }
    private class B : IMyInterface { public int GetValue() => 456; }
    private class C : IMyInterface { public int GetValue() => 789; }
}