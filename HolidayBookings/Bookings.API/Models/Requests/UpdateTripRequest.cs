using System.ComponentModel.DataAnnotations;

namespace Bookings.API.Models.Requests
{
    public class UpdateTripRequest
    {
        [Required]
        public int Id { get; set; }

        [property: Required, StringLength(320)]
        public required string Name { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        [Required]
        public bool Live { get; set; }
    }
}
