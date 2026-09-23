using BenchmarkDotNet.Attributes;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Benchmarks;

[HideColumns("StdDev", "PowerPlanMode")]
/////[MemoryDiagnoser]
public partial class RegexBench1
{
    //    The Adventures of Sherlock Holmes by Arthur Conan Doyle
    private static readonly string book = new HttpClient().GetStringAsync("https://www.gutenberg.org/files/1661/1661-0.txt").Result;

    private const string pattern = @"\byear\b";

    private readonly Regex _regexInterpreted = new Regex(pattern, RegexOptions.IgnoreCase);

    private readonly Regex _regexCompiled = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

    [GeneratedRegex(pattern, RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex Regex1Sourcegenerated();

    [Benchmark]
    public int Count1Interpret() => _regexInterpreted.Count(book);

    [Benchmark]
    public int Count1Compiled() => _regexCompiled.Count(book);

    [Benchmark]
    public int Count1SourceGenerated() => Regex1Sourcegenerated().Count(book);
}