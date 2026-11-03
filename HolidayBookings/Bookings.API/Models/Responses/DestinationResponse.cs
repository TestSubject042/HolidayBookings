namespace Bookings.API.Models.Responses
{
    public class DestinationResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Country { get; set; }
        public DateTime Arrival { get; set; }
        public DateTime Departure { get; set; }
    }
}
