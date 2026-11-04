using Bookings.API.Models.Requests;
using Bookings.API.Services;
using Bookings.Data.Entities;
using Bookings.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bookings.API.Tests.Services;

public class TripServiceTests : DbTestBase
{
    // ======================= CreateAsync =======================

    [Fact]
    public async Task CreateAsync_SavesTripWithRequestValues()
    {
        var userId = AddUser(organiser: true);
        using var db = NewContext();
        var service = new TripService(db);

        var created = await service.CreateAsync(
            new CreateTripRequest { Name = "Alps", MaxParticipants = 8, Live = false }, userId);

        Assert.True(created.Id > 0);

        using var verify = NewContext();
        var saved = await verify.Set<Trip>().SingleAsync(t => t.Id == created.Id);
        Assert.Equal("Alps", saved.Name);
        Assert.Equal(8, saved.MaxParticipants);
        Assert.False(saved.Live);
    }

    [Fact]
    public async Task CreateAsync_MakesCreatorTheOrganiser()
    {
        var userId = AddUser(organiser: true);
        using var db = NewContext();
        var service = new TripService(db);

        var created = await service.CreateAsync(
            new CreateTripRequest { Name = "Alps", MaxParticipants = 8, Live = true }, userId);

        using var verify = NewContext();
        var link = await verify.Set<UserTrip>().SingleAsync(ut => ut.TripId == created.Id);
        Assert.Equal(userId, link.UserId);
        Assert.Equal(TripRole.Organiser, link.Role);
    }

    // ======================= GetTripAsync =======================

    [Fact]
    public async Task GetTripAsync_ReturnsTripWithDestinationsAndTripUsersLoaded()
    {
        // The controller reads trip.Destinations and trip.TripUsers, so they must be included.
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, TripRole.Organiser);
        AddDestination(tripId, "Zermatt");
        AddDestination(tripId, "Chamonix", "France");

        using var db = NewContext();
        var trip = await new TripService(db).GetTripAsync(tripId);

        Assert.NotNull(trip);
        Assert.Equal(2, trip.Destinations.Count);
        var tripUser = Assert.Single(trip.TripUsers);
        Assert.Equal(TripRole.Organiser, tripUser.Role);
    }

    [Fact]
    public async Task GetTripAsync_MissingTrip_ReturnsNull()
    {
        using var db = NewContext();

        var trip = await new TripService(db).GetTripAsync(999);

        Assert.Null(trip);
    }

    // ======================= GetLiveTripsAsync =======================

    [Fact]
    public async Task GetLiveTripsAsync_ReturnsOnlyLiveTrips()
    {
        var live1 = AddTrip("Live 1", live: true);
        var live2 = AddTrip("Live 2", live: true);
        AddTrip("Draft", live: false);

        using var db = NewContext();
        var trips = await new TripService(db).GetLiveTripsAsync();

        Assert.Equal(2, trips.Count);
        Assert.Contains(trips, t => t.Id == live1);
        Assert.Contains(trips, t => t.Id == live2);
    }

    [Fact]
    public async Task GetLiveTripsAsync_NoLiveTrips_ReturnsEmptyList()
    {
        AddTrip(live: false);

        using var db = NewContext();
        var trips = await new TripService(db).GetLiveTripsAsync();

        Assert.Empty(trips);
    }

    // ======================= GetUserTripsAsync =======================

    [Fact]
    public async Task GetUserTripsAsync_ReturnsOnlyThatUsersTrips_WithTripLoaded()
    {
        var me = AddUser();
        var other = AddUser();
        var myTrip = AddTrip("Mine");
        var otherTrip = AddTrip("Theirs");
        Link(myTrip, me, TripRole.Customer);
        Link(otherTrip, other, TripRole.Customer);

        using var db = NewContext();
        var result = await new TripService(db).GetUserTripsAsync(me);

        var userTrip = Assert.Single(result);
        Assert.Equal(TripRole.Customer, userTrip.Role);
        Assert.NotNull(userTrip.Trip);              // controller reads t.Trip.Name etc.
        Assert.Equal("Mine", userTrip.Trip.Name);
    }

    [Fact]
    public async Task GetUserTripsAsync_IncludesBothRoles()
    {
        var me = AddUser();
        Link(AddTrip("As customer"), me, TripRole.Customer);
        Link(AddTrip("As organiser"), me, TripRole.Organiser);

        using var db = NewContext();
        var result = await new TripService(db).GetUserTripsAsync(me);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, ut => ut.Role == TripRole.Customer);
        Assert.Contains(result, ut => ut.Role == TripRole.Organiser);
    }

    [Fact]
    public async Task GetUserTripsAsync_UserWithNoTrips_ReturnsEmptyList()
    {
        var me = AddUser();

        using var db = NewContext();
        var result = await new TripService(db).GetUserTripsAsync(me);

        Assert.Empty(result);
    }

    // ======================= UserOnTripAsync =======================

    [Fact]
    public async Task UserOnTripAsync_UserLinked_ReturnsTrue()
    {
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, TripRole.Customer);

        using var db = NewContext();

        Assert.True(await new TripService(db).UserOnTripAsync(tripId, userId));
    }

    [Fact]
    public async Task UserOnTripAsync_UserNotLinked_ReturnsFalse()
    {
        var tripId = AddTrip();
        var userId = AddUser();

        using var db = NewContext();

        Assert.False(await new TripService(db).UserOnTripAsync(tripId, userId));
    }

    [Fact]
    public async Task UserOnTripAsync_LinkedToDifferentTrip_ReturnsFalse()
    {
        var tripA = AddTrip("A");
        var tripB = AddTrip("B");
        var userId = AddUser();
        Link(tripA, userId, TripRole.Customer);

        using var db = NewContext();

        Assert.False(await new TripService(db).UserOnTripAsync(tripB, userId));
    }

    // ======================= GetUserRoleOnTripAsync =======================

    [Theory]
    [InlineData(TripRole.Customer)]
    [InlineData(TripRole.Organiser)]
    public async Task GetUserRoleOnTripAsync_ReturnsTheUsersRole(TripRole role)
    {
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, role);

        using var db = NewContext();

        Assert.Equal(role, await new TripService(db).GetUserRoleOnTripAsync(tripId, userId));
    }

    [Fact]
    public async Task GetUserRoleOnTripAsync_UserNotOnTrip_ReturnsNull()
    {
        var tripId = AddTrip();
        var userId = AddUser();

        using var db = NewContext();

        Assert.Null(await new TripService(db).GetUserRoleOnTripAsync(tripId, userId));
    }

    [Fact]
    public async Task GetUserRoleOnTripAsync_TripDoesNotExist_ReturnsNull()
    {
        var userId = AddUser();

        using var db = NewContext();

        Assert.Null(await new TripService(db).GetUserRoleOnTripAsync(999, userId));
    }

    [Fact]
    public async Task GetUserRoleOnTripAsync_ReturnsRoleForTheRequestedTripOnly()
    {
        var tripA = AddTrip("A");
        var tripB = AddTrip("B");
        var userId = AddUser();
        Link(tripA, userId, TripRole.Organiser);
        Link(tripB, userId, TripRole.Customer);

        using var db = NewContext();
        var service = new TripService(db);

        Assert.Equal(TripRole.Organiser, await service.GetUserRoleOnTripAsync(tripA, userId));
        Assert.Equal(TripRole.Customer, await service.GetUserRoleOnTripAsync(tripB, userId));
    }

    // ======================= AddUserAsync =======================

    [Theory]
    [InlineData(TripRole.Customer)]
    [InlineData(TripRole.Organiser)]
    public async Task AddUserAsync_PersistsLinkWithRole(TripRole role)
    {
        var tripId = AddTrip();
        var userId = AddUser();

        using (var db = NewContext())
            await new TripService(db).AddUserAsync(tripId, userId, role);

        using var verify = NewContext();
        var link = await verify.Set<UserTrip>().SingleAsync();
        Assert.Equal(tripId, link.TripId);
        Assert.Equal(userId, link.UserId);
        Assert.Equal(role, link.Role);
    }

    [Fact]
    public async Task AddUserAsync_DoesNotAffectOtherUsersOnTheTrip()
    {
        var tripId = AddTrip();
        var existing = AddUser();
        var newcomer = AddUser();
        Link(tripId, existing, TripRole.Organiser);

        using (var db = NewContext())
            await new TripService(db).AddUserAsync(tripId, newcomer, TripRole.Customer);

        using var verify = NewContext();
        Assert.Equal(2, await verify.Set<UserTrip>().CountAsync(ut => ut.TripId == tripId));
        Assert.Equal(TripRole.Organiser,
            (await verify.Set<UserTrip>().SingleAsync(ut => ut.UserId == existing)).Role);
    }

    // ======================= RemoveUserAsync =======================

    [Fact]
    public async Task RemoveUserAsync_RemovesOnlyThatUsersLink()
    {
        var tripId = AddTrip();
        var leaving = AddUser();
        var staying = AddUser();
        Link(tripId, leaving, TripRole.Customer);
        Link(tripId, staying, TripRole.Customer);

        using (var db = NewContext())
            await new TripService(db).RemoveUserAsync(tripId, leaving);

        using var verify = NewContext();
        var remaining = await verify.Set<UserTrip>().SingleAsync();
        Assert.Equal(staying, remaining.UserId);
    }

    [Fact]
    public async Task RemoveUserAsync_DoesNotDeleteTripOrUser()
    {
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, TripRole.Customer);

        using (var db = NewContext())
            await new TripService(db).RemoveUserAsync(tripId, userId);

        using var verify = NewContext();
        Assert.True(await verify.Set<Trip>().AnyAsync(t => t.Id == tripId));
        Assert.True(await verify.Set<User>().AnyAsync(u => u.Id == userId));
    }

    [Fact]
    public async Task RemoveUserAsync_OnlyAffectsTheGivenTrip()
    {
        var tripA = AddTrip("A");
        var tripB = AddTrip("B");
        var userId = AddUser();
        Link(tripA, userId, TripRole.Customer);
        Link(tripB, userId, TripRole.Customer);

        using (var db = NewContext())
            await new TripService(db).RemoveUserAsync(tripA, userId);

        using var verify = NewContext();
        var remaining = await verify.Set<UserTrip>().SingleAsync();
        Assert.Equal(tripB, remaining.TripId);
    }

    // ======================= UpdateTripAsync =======================

    [Fact]
    public async Task UpdateTripAsync_PersistsNewValues()
    {
        var tripId = AddTrip("Old", live: false, maxParticipants: 5);

        using (var db = NewContext())
        {
            var service = new TripService(db);
            var trip = await service.GetTripAsync(tripId);   // same context, as in a real request

            await service.UpdateTripAsync(trip, new UpdateTripRequest
            {
                Name = "New",
                MaxParticipants = 20,
                Live = true
            });
        }

        using var verify = NewContext();
        var saved = await verify.Set<Trip>().SingleAsync(t => t.Id == tripId);
        Assert.Equal("New", saved.Name);
        Assert.Equal(20, saved.MaxParticipants);
        Assert.True(saved.Live);
    }

    [Fact]
    public async Task UpdateTripAsync_DoesNotChangeOtherTrips()
    {
        var target = AddTrip("Target");
        var bystander = AddTrip("Bystander", maxParticipants: 7);

        using (var db = NewContext())
        {
            var service = new TripService(db);
            var trip = await service.GetTripAsync(target);
            await service.UpdateTripAsync(trip, new UpdateTripRequest
            {
                Name = "Changed",
                MaxParticipants = 99,
                Live = false
            });
        }

        using var verify = NewContext();
        var other = await verify.Set<Trip>().SingleAsync(t => t.Id == bystander);
        Assert.Equal("Bystander", other.Name);
        Assert.Equal(7, other.MaxParticipants);
    }

    // ======================= DeleteTripAsync =======================

    [Fact]
    public async Task DeleteTripAsync_RemovesTripAndItsLinksAndDestinations()
    {
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, TripRole.Organiser);
        AddDestination(tripId, "Zermatt");

        using (var db = NewContext())
        {
            var service = new TripService(db);
            var trip = await service.GetTripAsync(tripId);
            await service.DeleteTripAsync(trip);
        }

        using var verify = NewContext();
        Assert.False(await verify.Set<Trip>().AnyAsync(t => t.Id == tripId));
        Assert.False(await verify.Set<UserTrip>().AnyAsync(ut => ut.TripId == tripId));
        Assert.False(await verify.Set<Destination>().AnyAsync(d => d.TripId == tripId));
        Assert.True(await verify.Set<User>().AnyAsync(u => u.Id == userId));   // the user survives
    }

    [Fact]
    public async Task DeleteTripAsync_LeavesOtherTripsAlone()
    {
        var doomed = AddTrip("Doomed");
        var safe = AddTrip("Safe");

        using (var db = NewContext())
        {
            var service = new TripService(db);
            await service.DeleteTripAsync(await service.GetTripAsync(doomed));
        }

        using var verify = NewContext();
        Assert.True(await verify.Set<Trip>().AnyAsync(t => t.Id == safe));
    }

    [Fact]
    public async Task RemoveUserAsync_UserNotOnTrip_DoesNothingAndDoesNotThrow()
    {
        var tripId = AddTrip();
        var staying = AddUser();
        var stranger = AddUser();
        Link(tripId, staying, TripRole.Customer);

        using (var db = NewContext())
            await new TripService(db).RemoveUserAsync(tripId, stranger);

        using var verify = NewContext();
        Assert.Equal(1, await verify.Set<UserTrip>().CountAsync());
    }

    [Fact]
    public async Task AddUserAsync_UserAlreadyOnTrip_Throws()
    {
        var tripId = AddTrip();
        var userId = AddUser();
        Link(tripId, userId, TripRole.Customer);

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            using var db = NewContext();
            await new TripService(db).AddUserAsync(tripId, userId, TripRole.Organiser);
        });

        using var verify = NewContext();
        Assert.Equal(TripRole.Customer, (await verify.Set<UserTrip>().SingleAsync()).Role);
    }

    [Fact]
    public async Task AddUserAsync_UnknownTrip_Throws()
    {
        var userId = AddUser();

        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            using var db = NewContext();
            await new TripService(db).AddUserAsync(999, userId, TripRole.Customer);
        });
    }

    [Fact]
    public async Task GetLiveTripsAsync_IncludesDestinations()
    {
        var tripId = AddTrip(live: true);
        AddDestination(tripId, "Zermatt");

        using var db = NewContext();
        var trips = await new TripService(db).GetLiveTripsAsync();

        var trip = Assert.Single(trips);
        Assert.Single(trip.Destinations);
    }

    [Fact]
    public async Task CreateAsync_UnknownUser_ThrowsAndSavesNoTrip()
    {
        await Assert.ThrowsAsync<DbUpdateException>(async () =>
        {
            using var db = NewContext();
            await new TripService(db).CreateAsync(
                new CreateTripRequest { Name = "Alps", MaxParticipants = 8, Live = true }, userId: 999);
        });

        using var verify = NewContext();
        Assert.Empty(await verify.Set<Trip>().ToListAsync());
    }
}