using Bookings.Data.Entities;

namespace Bookings.API.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserAsync(int userId);
    }
}
