using Bookings.API.Models.Requests;
using Bookings.API.Services.Interfaces;
using Bookings.Data;
using Bookings.Data.Entities;
using Bookings.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bookings.API.Services
{
    public class TripService : ITripService
    {
        private readonly BookingsDbContext _db;
        public TripService(BookingsDbContext db) => _db = db;

        public async Task<Trip> CreateAsync(CreateTripRequest request, int userId)
        {
            var trip = new Trip
            {
                Name = request.Name,
                MaxParticipants = request.MaxParticipants,
                Live = request.Live
            };

            // The creator becomes the first organizer
            trip.TripUsers.Add(new UserTrip
            {
                UserId = userId,
                Role = TripRole.Organiser
            });

            _db.Set<Trip>().Add(trip);
            await _db.SaveChangesAsync();

            return trip;
        }

        public async Task AddUserAsync(int tripId, int userId, TripRole role)
        {
            _db.Set<UserTrip>().Add(new UserTrip
            {
                TripId = tripId,
                UserId = userId,
                Role = role
            });
            await _db.SaveChangesAsync();
        }

        public async Task RemoveUserAsync(int tripId, int userId)
        {
            var target = await _db.Set<UserTrip>()
                .FirstOrDefaultAsync(ut => ut.TripId == tripId && ut.UserId == userId);

            if (target is not null)
            {
                _db.Set<UserTrip>().Remove(target);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<UserTrip>> GetUserTripsAsync(int userId)
        {
            var trips = await _db.Set<UserTrip>()
                .Include(ut => ut.Trip)
                .Where(ut => ut.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            return trips;
        }

        public async Task<bool> UserOnTripAsync(int tripId, int userId)
        {
            return await _db.Set<UserTrip>().AnyAsync(ut => ut.TripId == tripId && ut.UserId == userId);
        }

        public async Task<List<Trip>> GetLiveTripsAsync()
        {
            var trips = await _db.Set<Trip>()
                .Include(t => t.Destinations)
                .Where(t => t.Live)
                .AsNoTracking()
                .ToListAsync();

            return trips;
        }

        public async Task<Trip?> GetTripAsync(int tripId)
        {
            return await _db.Set<Trip>()
                .Include(t => t.Destinations)
                .Include(t => t.TripUsers)
                .FirstOrDefaultAsync(t => t.Id == tripId);
        }

        public async Task UpdateTripAsync(Trip trip, UpdateTripRequest request)
        {
            trip.Name = request.Name;
            trip.MaxParticipants = request.MaxParticipants;
            trip.Live = request.Live;

            await _db.SaveChangesAsync();
        }

        public async Task DeleteTripAsync(Trip target)
        {
            _db.Set<Trip>().Remove(target);
            await _db.SaveChangesAsync();
        }

        public async Task<TripRole?> GetUserRoleOnTripAsync(int tripId, int userId)
        {
            var role = await _db.Set<UserTrip>()
                .Where(p => p.TripId == tripId && p.UserId == userId)
                .Select(p => (TripRole?)p.Role)
                .FirstOrDefaultAsync();

            return role;
        }
    }
}
