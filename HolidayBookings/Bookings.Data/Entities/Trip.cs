using Bookings.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bookings.Data.Entities
{
    public class Trip
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int MaxParticipants { get; set; }
        public bool Live { get; set; }
        public List<Destination> Destinations { get; set; } = new();

        public List<UserTrip> TripUsers { get; set; } = new();
    }
}
