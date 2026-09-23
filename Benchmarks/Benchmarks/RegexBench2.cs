using BenchmarkDotNet.Attributes;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode")]
/////[MemoryDiagnoser]
public partial class RegexBench2
{
    //    The Adventures of Sherlock Holmes by Arthur Conan Doyle
    private static readonly string book = new HttpClient().GetStringAsync("https://www.gutenberg.org/files/1661/1661-0.txt").Result;

    private const string pattern = "Holmes|Watson|Lestrade|Hudson|Moriarty|Adler|Moran|Morstan|Gregson";

    private readonly Regex _namesCompiled = new Regex(pattern, RegexOptions.Compiled);

    private readonly Regex _namesInterpreted = new Regex(pattern);

    [GeneratedRegex(pattern, RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NamesRegexSourcegenerated();

    [Benchmark]
    public int Count2Interpret() => _namesInterpreted.Count(book);

    [Benchmark]
    public int Count2Compiled() => _namesCompiled.Count(book);

    [Benchmark]
    public int Count2SourceGenerated() => NamesRegexSourcegenerated().Count(book);
}