namespace Bookings.API.Models.Requests
{
    public class DestinationRequest
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public required string Name { get; set; }
        public required string Country { get; set; }
        public DateTime Arrival { get; set; }
        public DateTime Departure { get; set; }
    }
}
