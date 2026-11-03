using Bookings.Data;
using Bookings.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.API.Services
{
    public class UserService
    {
        private readonly BookingsDbContext _db;
        public UserService(BookingsDbContext db) => _db = db;

        public async Task<User?> GetUserAsync(int userId)
        {
            var user = await _db.Set<User>()
                .FindAsync(userId);

            return user;
        }
    }
}
