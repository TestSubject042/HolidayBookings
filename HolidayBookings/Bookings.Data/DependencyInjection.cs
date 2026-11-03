using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Bookings.Data
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDataLayer(
        this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<BookingsDbContext>(options =>
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure()));

            return services;
        }
    }
}
