using System.ComponentModel.DataAnnotations;

namespace Bookings.API.Models.Requests
{
    public class UpdateTripRequest
    {
        public required string Name { get; set; }
        public required int MaxParticipants { get; set; }
        public required bool Live { get; set; }
    }
}
