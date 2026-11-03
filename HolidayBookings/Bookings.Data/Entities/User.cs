using Bookings.Data.Enums;
using Microsoft.AspNetCore.Identity;

namespace Bookings.Data.Entities
{
    public class User : IdentityUser<int>
    {
        public string Name { get; set; } = string.Empty;
        public bool Organiser { get; set; }
        public DateTime Created { get; set; }

        public List<UserTrip> UserTrips { get; set; } = new();
    }
}
