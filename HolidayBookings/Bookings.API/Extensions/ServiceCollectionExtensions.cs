using Bookings.API.Services;
using Bookings.API.Services.Interfaces;

namespace Bookings.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<ITripService, TripService>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
