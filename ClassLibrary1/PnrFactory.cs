using static TestModel;

public static class PnrFactory
{
    private static readonly Random _random = new();

    public static List<Pnr> CreateMany(int count)
    {
        var list = new List<Pnr>(count);

        for (int i = 0; i < count; i++)
        {
            list.Add(CreateOne(i));
        }

        return list;
    }

    private static Pnr CreateOne(int index)
    {
        var baseDate = DateTime.UtcNow.AddDays(index);

        return new Pnr
        {
            RecordLocator = $"PNR{index:00000}",
            CreatedAt = baseDate,

            Traveler = new Traveler
            {
                FirstName = $"Name{index}",
                LastName = $"Surname{index}",
                BirthDate = new DateTime(1980 + index % 30, (index % 12) + 1, (index % 28) + 1),
                PassportNumber = $"P{_random.Next(1000000, 9999999)}",
                Nationality = "RU",
                Contact = new ContactInfo
                {
                    Email = $"user{index}@example.com",
                    Phone = $"+7{_random.Next(100000000, 999999999)}"
                }
            },

            Flights = GenerateFlights(baseDate),
            Hotels = GenerateHotels(baseDate),
            CarRentals = GenerateCarRentals(baseDate)
        };
    }

    private static List<FlightSegment> GenerateFlights(DateTime baseDate)
    {
        var airports = new[]
        {
            ("BER", "Berlin", "DE"),
            ("CDG", "Paris", "FR"),
            ("MAD", "Madrid", "ES"),
            ("FCO", "Rome", "IT"),
            ("AMS", "Amsterdam", "NL")
        };

        var flights = new List<FlightSegment>();

        for (int i = 0; i < 4; i++)
        {
            var dep = airports[i % airports.Length];
            var arr = airports[(i + 1) % airports.Length];

            flights.Add(new FlightSegment
            {
                FlightNumber = $"FL{_random.Next(100, 999)}",
                Airline = $"Airline{_random.Next(1, 5)}",
                Departure = new Airport { Code = dep.Item1, City = dep.Item2, Country = dep.Item3 },
                Arrival = new Airport { Code = arr.Item1, City = arr.Item2, Country = arr.Item3 },
                DepartureTime = baseDate.AddHours(i * 5),
                ArrivalTime = baseDate.AddHours(i * 5 + 2),
                BookingClass = i % 2 == 0 ? "Y" : "J",
                Seat = $"{_random.Next(1, 30)}{(char)('A' + _random.Next(0, 6))}"
            });
        }

        return flights;
    }

    private static List<HotelStay> GenerateHotels(DateTime baseDate)
    {
        return new List<HotelStay>
        {
            new HotelStay
            {
                HotelName = $"Hotel{_random.Next(1, 100)}",
                City = "Paris",
                CheckIn = baseDate,
                CheckOut = baseDate.AddDays(1),
                RoomType = "Standard",
                PricePerNight = _random.Next(80, 200)
            },
            new HotelStay
            {
                HotelName = $"Hotel{_random.Next(1, 100)}",
                City = "Rome",
                CheckIn = baseDate.AddDays(2),
                CheckOut = baseDate.AddDays(4),
                RoomType = "Deluxe",
                PricePerNight = _random.Next(120, 300)
            }
        };
    }

    private static List<CarRental> GenerateCarRentals(DateTime baseDate)
    {
        return new List<CarRental>
        {
            new CarRental
            {
                Company = "Hertz",
                CarModel = "VW Golf",
                PickupDate = baseDate.AddDays(2),
                ReturnDate = baseDate.AddDays(3),
                PickupLocation = "Airport"
            },
            new CarRental
            {
                Company = "Sixt",
                CarModel = "BMW 3",
                PickupDate = baseDate.AddDays(3),
                ReturnDate = baseDate.AddDays(4),
                PickupLocation = "City Center"
            }
        };
    }
}