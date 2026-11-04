using Bookings.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bookings.API.Models.Requests
{
    public class AddTripUserRequest
    {
        public required int UserId { get; set; }
        public TripRole Role { get; set; }
    }
}
