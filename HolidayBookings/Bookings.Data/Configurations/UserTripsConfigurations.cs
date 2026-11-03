using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Bookings.Data.Entities;

namespace Bookings.Data.Configurations
{
    internal class UserTripsConfigurations : IEntityTypeConfiguration<UserTrip>
    {
        public void Configure(EntityTypeBuilder<UserTrip> builder)
        {
            builder.ToTable("UserTrips");

            builder.HasKey(p => new { p.TripId, p.UserId });

            builder.Property(p => p.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.HasOne(p => p.Trip)
                .WithMany(t => t.TripUsers)
                .HasForeignKey(p => p.TripId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.User)
                .WithMany(c => c.UserTrips)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(p => p.UserId);
        }
    }
}
