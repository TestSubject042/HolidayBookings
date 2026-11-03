namespace Bookings.API.Models.Responses
{
    public class TripsResponse
    {
        public List<TripResponse> CustomerTrips { get; set; } = new();
        public List<TripResponse> OrganisedTrips { get; set; } = new();
    }
}
