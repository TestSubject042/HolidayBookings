using Bookings.Data.Enums;
using System.ComponentModel.DataAnnotations;

namespace Bookings.API.Models.Requests
{
    public class AddTripUserRequest
    {
        [Required]
        public int userId { get; set; }

        [property: EnumDataType(typeof(TripRole))]
        public TripRole Role { get; set; }
    }
}
