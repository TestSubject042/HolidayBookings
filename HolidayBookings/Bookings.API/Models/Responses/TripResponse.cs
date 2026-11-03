namespace Bookings.API.Models.Responses
{
    public class TripResponse
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int MaxParticipants { get; set; }
        public bool Live { get; set; }
    }
}
