using BenchmarkDotNet.Attributes;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode", "Gen0", "Gen1", "Code Size")]
////[DisassemblyDiagnoser]
public class StringBenchOrdinal
{
    public string StringToTest = string.Empty;

    public volatile int res;
    public volatile string? res3;

    [Params("unboxed")]
    public string StringToSearch = string.Empty;
    public string StringToSearch2 = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        StringToSearch2 = StringToSearch.ToUpper();
        
        // 558 bytes string
        StringToTest = "The object type is an alias for System.Object in .NET. In the unified type system of C#, all types, predefined and user-defined, reference types and value types, inherit directly or indirectly from System.Object. You can assign values of any type (except ref struct, see ref struct) to variables of type object. Any object variable can be assigned to its default value using the literal null. When a variable of a value type is converted to object, it's said to be boxed. When a variable of type object is converted to a value type, it's said to be unboxed.";
    }

    [Benchmark]
    public void StringIndexOfOrdinal()
    {
        res = StringToTest.IndexOf(StringToSearch, StringComparison.Ordinal);
    }

    [Benchmark]
    public void StringIndexOfOrdinalIgnoreCase()
    {
        res = StringToTest.IndexOf(StringToSearch, StringComparison.OrdinalIgnoreCase);
    }

    ////[Benchmark]
    public void StringIndexOfOrdinalIgnoreCase2()
    {
        res = StringToTest.IndexOf(StringToSearch2, StringComparison.OrdinalIgnoreCase);
    }
}


[HideColumns("StdDev", "PowerPlanMode", "Gen0", "Gen1", "Code Size")]
////[DisassemblyDiagnoser]
public class StringBenchCurrentCulture
{
    public string StringToTest = string.Empty;

    public volatile int res;
    public volatile string? res3;

    // [Params("type", "unboxed")]
    [Params("unboxed")]
    public string StringToSearch = string.Empty;
    public string StringToSearch2 = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        StringToSearch2 = StringToSearch.ToUpper();

        // 558 bytes string
        StringToTest = "The object type is an alias for System.Object in .NET. In the unified type system of C#, all types, predefined and user-defined, reference types and value types, inherit directly or indirectly from System.Object. You can assign values of any type (except ref struct, see ref struct) to variables of type object. Any object variable can be assigned to its default value using the literal null. When a variable of a value type is converted to object, it's said to be boxed. When a variable of type object is converted to a value type, it's said to be unboxed.";
    }

    [Benchmark]
    public void StringIndexOfCurrentCulture()
    {
        res = StringToTest.IndexOf(StringToSearch, StringComparison.CurrentCulture);
    }

    [Benchmark]
    public void StringIndexOfCurrentCultureIgnoreCase()
    {
        res = StringToTest.IndexOf(StringToSearch, StringComparison.CurrentCultureIgnoreCase);
    }
}