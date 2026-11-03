using Bookings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bookings.Data.Configurations
{
    public class DestinationConfiguration : IEntityTypeConfiguration<Destination>
    {
        public void Configure(EntityTypeBuilder<Destination> builder)
        {
            builder.ToTable("Destinations");
            builder.Property(d => d.Name).HasMaxLength(320).IsRequired();
            builder.HasOne(d => d.Trip)
                   .WithMany(t => t.Destinations)
                   .HasForeignKey(d => d.TripId);
        }
    }
}
