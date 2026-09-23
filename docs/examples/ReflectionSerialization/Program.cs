using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Serialization;
using static TestModel;

internal static class Program
{
    // DTO metadata is preserved by TrimmerRootAssembly. These closed collection
    // types belong to System.Private.CoreLib, not to the DTO assembly.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<FlightSegment>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<HotelStay>))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(List<CarRental>))]
    private static void Main()
    {
        Console.WriteLine($"Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}");
        Console.WriteLine($"JSON reflection default: {JsonSerializer.IsReflectionEnabledByDefault}");

        // Deliberately keep IL2026/IL3050 visible: this is an experimental
        // reflection path, not a declaration of general AOT compatibility.
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = true
        };
        var serializer = new XmlSerializer(typeof(PnrList));
        var cases = new[]
        {
            new PnrList { Pnrs = PnrFactory.CreateMany(3).ToArray() },
            new PnrList(),
            new PnrList
            {
                Pnrs = [new Pnr
                {
                    RecordLocator = "Проверка <&>",
                    CreatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                    Traveler = new Traveler { FirstName = "Имя", LastName = "Фамилия" }
                }]
            }
        };

        foreach (var original in cases)
        {
            var json = JsonSerializer.Serialize(original, options);
            var fromJson = JsonSerializer.Deserialize<PnrList>(json, options)
                ?? throw new Exception("JSON returned null");
            using var writer = new StringWriter();
            serializer.Serialize(writer, original);
            var fromXml = (PnrList?)serializer.Deserialize(new StringReader(writer.ToString()))
                ?? throw new Exception("XML returned null");
            Verify(original, fromJson);
            Verify(original, fromXml);
        }
        Console.WriteLine("PASS: JSON and XML round trips, 3 cases, all DTO properties compared.");
    }

    // Static property access does not use a source generator or a second
    // reflection-based comparison that could hide missing preservation rules.
    private static void Verify(PnrList expected, PnrList actual)
    {
        Check(expected.Pnrs.Length == actual.Pnrs.Length);
        foreach (var (a, b) in expected.Pnrs.Zip(actual.Pnrs))
        {
            Check((a.RecordLocator, a.CreatedAt) == (b.RecordLocator, b.CreatedAt));
            var x = a.Traveler;
            var y = b.Traveler;
            Check((x.FirstName, x.LastName, x.BirthDate, x.PassportNumber, x.Nationality)
                == (y.FirstName, y.LastName, y.BirthDate, y.PassportNumber, y.Nationality));
            Check((x.Contact is null) == (y.Contact is null));
            Check((x.Contact?.Email, x.Contact?.Phone) == (y.Contact?.Email, y.Contact?.Phone));
            Check(a.Flights.Count == b.Flights.Count);
            foreach (var (f, g) in a.Flights.Zip(b.Flights))
            {
                Check((f.FlightNumber, f.Airline, f.DepartureTime, f.ArrivalTime, f.BookingClass, f.Seat)
                    == (g.FlightNumber, g.Airline, g.DepartureTime, g.ArrivalTime, g.BookingClass, g.Seat));
                Check((f.Departure.Code, f.Departure.City, f.Departure.Country)
                    == (g.Departure.Code, g.Departure.City, g.Departure.Country));
                Check((f.Arrival.Code, f.Arrival.City, f.Arrival.Country)
                    == (g.Arrival.Code, g.Arrival.City, g.Arrival.Country));
            }
            Check(a.Hotels.Count == b.Hotels.Count);
            foreach (var (h, i) in a.Hotels.Zip(b.Hotels))
                Check((h.HotelName, h.City, h.CheckIn, h.CheckOut, h.RoomType, h.PricePerNight)
                    == (i.HotelName, i.City, i.CheckIn, i.CheckOut, i.RoomType, i.PricePerNight));
            Check(a.CarRentals.Count == b.CarRentals.Count);
            foreach (var (c, d) in a.CarRentals.Zip(b.CarRentals))
                Check((c.Company, c.CarModel, c.PickupDate, c.ReturnDate, c.PickupLocation)
                    == (d.Company, d.CarModel, d.PickupDate, d.ReturnDate, d.PickupLocation));
        }
    }

    private static void Check(bool condition)
    {
        if (!condition) throw new Exception("Round-trip property mismatch");
    }
}
