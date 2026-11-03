namespace Bookings.Data.Entities
{
    public class Destination
    {
        public int Id { get; set; }
        public int TripId { get; set; }
        public required string Name { get; set; }
        public required string Country { get; set; }
        public DateTime Arrival {  get; set; }
        public DateTime Departure { get; set; }

        public Trip Trip { get; set; } = null!;
    }
}
