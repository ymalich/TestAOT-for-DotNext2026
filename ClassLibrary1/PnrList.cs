public class TestModel
{
    public class PnrList
    {
        public Pnr[] Pnrs { get; set; } = [];
    }

    public class Pnr
    {
        public string RecordLocator { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Traveler Traveler { get; set; } = null!;

        public List<FlightSegment> Flights { get; set; } = new();
        public List<HotelStay> Hotels { get; set; } = new();
        public List<CarRental> CarRentals { get; set; } = new();
    }

    public class Traveler
    {
        public Traveler() { }

        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime BirthDate { get; set; }

        public string? PassportNumber { get; set; }
        public string? Nationality { get; set; }

        public ContactInfo? Contact { get; set; }
    }

    public class ContactInfo
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }

    public class FlightSegment
    {
        public string FlightNumber { get; set; } = null!;
        public string Airline { get; set; } = null!;

        public Airport Departure { get; set; } = null!;
        public Airport Arrival { get; set; } = null!;

        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }

        public string? BookingClass { get; set; }
        public string? Seat { get; set; }
    }

    public class Airport
    {
        public string Code { get; set; } = null!;
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class HotelStay
    {
        public string HotelName { get; set; } = null!;
        public string? City { get; set; }

        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }

        public string? RoomType { get; set; }
        public decimal PricePerNight { get; set; }
    }

    public class CarRental
    {
        public string Company { get; set; } = null!;
        public string? CarModel { get; set; }

        public DateTime PickupDate { get; set; }
        public DateTime ReturnDate { get; set; }

        public string? PickupLocation { get; set; }
    }
}