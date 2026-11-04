using Bookings.Data;
using Bookings.Data.Entities;
using Bookings.Data.Enums;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Bookings.API.Tests.Services;

public abstract class DbTestBase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<BookingsDbContext> _options;
    private int _userCounter;

    protected DbTestBase()
    {
        SQLitePCL.Batteries.Init();

        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        _connection.Open();

        _options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = NewContext();
        context.Database.EnsureCreated();
    }

    /// A new context per call. Use one for arranging, a different one for acting,
    /// and a third for asserting, so tracked entities can't hide bugs.
    protected BookingsDbContext NewContext() => new(_options);

    protected int AddUser(bool organiser = false)
    {
        using var db = NewContext();
        var n = Interlocked.Increment(ref _userCounter);
        var user = new User
        {
            UserName = $"user{n}",
            Email = $"user{n}@example.com",
            Name = $"User {n}",
            Organiser = organiser,
            Created = DateTime.UtcNow
        };
        db.Set<User>().Add(user);
        db.SaveChanges();
        return user.Id;
    }

    protected int AddTrip(string name = "Trip", bool live = true, int maxParticipants = 10)
    {
        using var db = NewContext();
        var trip = new Trip { Name = name, Live = live, MaxParticipants = maxParticipants };
        db.Set<Trip>().Add(trip);
        db.SaveChanges();
        return trip.Id;
    }

    protected void Link(int tripId, int userId, TripRole role)
    {
        using var db = NewContext();
        db.Set<UserTrip>().Add(new UserTrip { TripId = tripId, UserId = userId, Role = role });
        db.SaveChanges();
    }

    protected void AddDestination(int tripId, string name, string country = "Switzerland")
    {
        using var db = NewContext();
        db.Set<Destination>().Add(new Destination
        {
            TripId = tripId,
            Name = name,
            Country = country,
            Arrival = new DateTime(2026, 7, 1),
            Departure = new DateTime(2026, 7, 4)
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}