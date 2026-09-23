using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using static TestModel;

internal class Program
{
    public static int Main(string[] args)
    {
        var sw = Stopwatch.StartNew();
        //Console.WriteLine($"{RuntimeInformation.FrameworkDescription}, {RuntimeInformation.ProcessArchitecture} ");
        //Console.WriteLine($"Sse2.IsSupported = {Sse2.IsSupported}, Sse41.IsSupported = {Sse41.IsSupported}, Sse42.IsSupported = {Sse42.IsSupported}, Avx.IsSupported = {Avx.IsSupported}");
        //Console.WriteLine($"Vector.IsHardwareAccelerated = {Vector.IsHardwareAccelerated}, Vector<byte>.Count = {Vector<byte>.Count}");
        if (args.Length < 1 || !File.Exists(args[0]))
        {
            args = [ "pnrs.json" ];
        }


        if (args.Length < 1 || !File.Exists(args[0]))
        {
            Console.WriteLine("Please provide a path to a JSON file as the first argument.");
            var pnrs = PnrFactory.CreateMany(100);
            var str = JsonSerializer.Serialize(new PnrList { Pnrs = pnrs.ToArray() }, PnrListJsonContext.Default.PnrList);
            File.WriteAllText("pnrs.json", str);
            return 0;
        }

        var json = File.ReadAllText(args[0]);
        var pnrList = JsonSerializer.Deserialize<PnrList>(json, PnrListJsonContext.Default.PnrList);
        var flighsByFlightNumber = pnrList!.Pnrs
           .SelectMany(x => x.Flights.Select(f => new { f.FlightNumber, Pnr = x }))
           .GroupBy(x => x.FlightNumber)
           .Where(x => x.Count() > 1)
           .OrderBy(x => x.Key)
           //.SelectMany(x => x.Select(x => x.))
           .Select(x => x.Key)
           .ToArray();

        var str2 = JsonSerializer.Serialize(flighsByFlightNumber, StringArrayJsonContext.Default.StringArray);
        //var sw2 = Stopwatch.StartNew();
        // Console.WriteLine(str2);
        //sw2.Stop();
        Process currentProcess = Process.GetCurrentProcess();
        // Здесь можно получить различные свойства памяти процесса
        long privateMemory = currentProcess.PrivateMemorySize64; // приватная память в байтах
        long workingSet = currentProcess.WorkingSet64;
        Console.WriteLine($"Private Memory: {privateMemory / 1024.0 / 1024.0:N1} MB, Physical memory usage (Working Set): {workingSet / 1024.0 / 1024.0:N1} MB");
        Console.WriteLine($"Elapsed time: {sw.Elapsed.TotalMilliseconds:N2} ms. / {str2.Substring(1, 7)}");
        return str2.Length;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PnrList))]
internal partial class PnrListJsonContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(string[]))]
internal partial class StringArrayJsonContext : JsonSerializerContext
{
}