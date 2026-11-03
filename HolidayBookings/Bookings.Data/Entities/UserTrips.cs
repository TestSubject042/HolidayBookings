using Bookings.Data.Enums;

namespace Bookings.Data.Entities
{
    public class UserTrip
    {
        public int TripId { get; set; }
        public int UserId { get; set; }
        public TripRole Role { get; set; }

        public Trip Trip { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
