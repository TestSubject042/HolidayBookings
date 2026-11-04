using Bookings.API.Models.Requests;
using Bookings.Data.Entities;
using Bookings.Data.Enums;

namespace Bookings.API.Services.Interfaces
{
    public interface ITripService
    {
        Task<Trip> CreateAsync(CreateTripRequest request, int userId);
        Task AddUserAsync(int tripId, int userId, TripRole role);
        Task RemoveUserAsync(int tripId, int userId);
        Task<List<UserTrip>> GetUserTripsAsync(int userId);
        Task<bool> UserOnTripAsync(int tripId, int userId);
        Task<List<Trip>> GetLiveTripsAsync();
        Task<Trip?> GetTripAsync(int tripId);
        Task UpdateTripAsync(Trip trip, UpdateTripRequest request);
        Task DeleteTripAsync(Trip target);
        Task<TripRole?> GetUserRoleOnTripAsync(int tripId, int userId);

    }
}
