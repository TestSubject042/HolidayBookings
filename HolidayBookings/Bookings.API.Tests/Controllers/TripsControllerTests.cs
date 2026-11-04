using System.Security.Claims;
using Bookings.API.Controllers;
using Bookings.API.Models.Requests;
using Bookings.API.Models.Responses;
using Bookings.API.Services.Interfaces;
using Bookings.Data.Entities;
using Bookings.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Bookings.API.Tests.Controllers;

public class TripsControllerTests
{
    private const int CallerId = 1;
    private const int TripId = 5;

    private readonly Mock<ITripService> _tripService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly TripsController _controller;

    public TripsControllerTests()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, CallerId.ToString()) },
            authenticationType: "test");

        _controller = new TripsController(_tripService.Object, _userService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    // ---------- Helpers: adjust these if your entities differ ----------

    private static Trip MakeTrip(int id = TripId, params UserTrip[] tripUsers) => new()
    {
        Id = id,
        Name = $"Trip {id}",
        MaxParticipants = 10,
        Live = true,
        Destinations = new List<Destination>(),
        TripUsers = tripUsers.ToList()
    };

    private static UserTrip MakeUserTrip(TripRole role, Trip? trip = null) => new()
    {
        Role = role,
        Trip = trip ?? MakeTrip()
    };

    private static User MakeUser(int id, bool organiser) => new()
    {
        Id = id,
        Organiser = organiser
    };

    private static UpdateTripRequest MakeUpdateRequest(int id = TripId) => new()
    {
        Name = "Updated name",
        MaxParticipants = 10,
        Live = true
    };

    private static CreateTripRequest MakeCreateRequest() => new()
    {
        Name = "Alps",
        MaxParticipants = 8,
        Live = false
    };

    /// Makes the caller pass RequireOrganizerAsync for the given trip.
    private void SetupCallerAsOrganizer(int tripId = TripId)
    {
        _userService.Setup(s => s.GetUserAsync(CallerId))
            .ReturnsAsync(MakeUser(CallerId, organiser: true));
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(tripId, CallerId))
            .ReturnsAsync(TripRole.Organiser);
    }

    // ======================= GetMine =======================

    [Fact]
    public async Task GetMine_SplitsTripsByRole()
    {
        var customerTrip = MakeTrip(1);
        var organisedTrip = MakeTrip(2);
        _tripService.Setup(s => s.GetUserTripsAsync(CallerId)).ReturnsAsync(new List<UserTrip>
        {
            MakeUserTrip(TripRole.Customer, customerTrip),
            MakeUserTrip(TripRole.Organiser, organisedTrip)
        });

        var result = await _controller.GetMine();

        var response = Assert.IsType<TripsResponse>(result.Value);
        var c = Assert.Single(response.CustomerTrips);
        var o = Assert.Single(response.OrganisedTrips);
        Assert.Equal(1, c.Id);
        Assert.Equal(2, o.Id);
    }

    [Fact]
    public async Task GetMine_NoTrips_ReturnsEmptyLists()
    {
        _tripService.Setup(s => s.GetUserTripsAsync(CallerId)).ReturnsAsync(new List<UserTrip>());

        var result = await _controller.GetMine();

        var response = Assert.IsType<TripsResponse>(result.Value);
        Assert.Empty(response.CustomerTrips);
        Assert.Empty(response.OrganisedTrips);
    }

    [Fact]
    public async Task GetMine_UsesIdFromClaims()
    {
        _tripService.Setup(s => s.GetUserTripsAsync(It.IsAny<int>())).ReturnsAsync(new List<UserTrip>());

        await _controller.GetMine();

        _tripService.Verify(s => s.GetUserTripsAsync(CallerId), Times.Once);
    }

    [Fact]
    public async Task GetMine_MapsTripFields()
    {
        var trip = MakeTrip(7);
        trip.Name = "Iceland";
        trip.MaxParticipants = 12;
        trip.Live = false;
        _tripService.Setup(s => s.GetUserTripsAsync(CallerId))
            .ReturnsAsync(new List<UserTrip> { MakeUserTrip(TripRole.Customer, trip) });

        var result = await _controller.GetMine();

        var mapped = Assert.Single(result.Value!.CustomerTrips);
        Assert.Equal(7, mapped.Id);
        Assert.Equal("Iceland", mapped.Name);
        Assert.Equal(12, mapped.MaxParticipants);
        Assert.False(mapped.Live);
    }

    // ======================= Get =======================

    [Fact]
    public async Task Get_TripExists_ReturnsMappedTrip()
    {
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(MakeTrip());

        var result = await _controller.Get(TripId);

        var response = Assert.IsType<TripResponse>(result.Value);
        Assert.Equal(TripId, response.Id);
        Assert.Equal($"Trip {TripId}", response.Name);
    }

    [Fact]
    public async Task Get_TripMissing_ReturnsNotFound()
    {
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync((Trip?)null);

        var result = await _controller.Get(TripId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ======================= GetDestinations =======================

    [Fact]
    public async Task GetDestinations_ReturnsMappedDestinations()
    {
        var trip = MakeTrip();
        trip.Destinations = new List<Destination>
        {
            new() { Id = 1, Name = "Zermatt", Country = "Switzerland",
                    Arrival = new DateTime(2026, 7, 1), Departure = new DateTime(2026, 7, 4) },
            new() { Id = 2, Name = "Chamonix", Country = "France",
                    Arrival = new DateTime(2026, 7, 4), Departure = new DateTime(2026, 7, 7) }
        };
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.GetDestinations(TripId);

        var list = Assert.IsType<List<DestinationResponse>>(result.Value);
        Assert.Equal(2, list.Count);
        Assert.Equal("Zermatt", list[0].Name);
        Assert.Equal("France", list[1].Country);
        Assert.Equal(new DateTime(2026, 7, 7), list[1].Departure);
    }

    [Fact]
    public async Task GetDestinations_NoDestinations_ReturnsEmptyList()
    {
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(MakeTrip());

        var result = await _controller.GetDestinations(TripId);

        Assert.Empty(Assert.IsType<List<DestinationResponse>>(result.Value));
    }

    [Fact]
    public async Task GetDestinations_TripMissing_ReturnsNotFound()
    {
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync((Trip?)null);

        var result = await _controller.GetDestinations(TripId);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ======================= Update =======================

    [Fact]
    public async Task Update_OrganizerAndTripExists_UpdatesAndReturnsNoContent()
    {
        SetupCallerAsOrganizer();
        var trip = MakeTrip();
        var request = MakeUpdateRequest();
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.Update(TripId, request);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.UpdateTripAsync(trip, request), Times.Once);
    }

    [Fact]
    public async Task Update_CallerNotFound_ReturnsNotFound()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync((User?)null);

        var result = await _controller.Update(TripId, MakeUpdateRequest());

        Assert.IsType<NotFoundResult>(result);
        _tripService.Verify(s => s.UpdateTripAsync(It.IsAny<Trip>(), It.IsAny<UpdateTripRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_UserIsNotAnOrganiser_ReturnsForbid()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));

        var result = await _controller.Update(TripId, MakeUpdateRequest());

        Assert.IsType<ForbidResult>(result);
        _tripService.Verify(s => s.UpdateTripAsync(It.IsAny<Trip>(), It.IsAny<UpdateTripRequest>()), Times.Never);
    }

    [Fact]
    public async Task Update_CallerNotOnTrip_ReturnsNotFound()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: true));
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, CallerId)).ReturnsAsync((TripRole?)null);

        var result = await _controller.Update(TripId, MakeUpdateRequest());

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_CallerIsCustomerOnTrip_ReturnsForbid()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: true));
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, CallerId)).ReturnsAsync(TripRole.Customer);

        var result = await _controller.Update(TripId, MakeUpdateRequest());

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Update_TripMissing_ReturnsNotFoundAndDoesNotUpdate()
    {
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync((Trip?)null);

        var result = await _controller.Update(TripId, MakeUpdateRequest());

        Assert.IsType<NotFoundResult>(result);
        _tripService.Verify(s => s.UpdateTripAsync(It.IsAny<Trip>(), It.IsAny<UpdateTripRequest>()), Times.Never);
    }

    // ======================= Delete (assumes fix 1) =======================

    [Fact]
    public async Task Delete_OrganizerAndTripExists_DeletesAndReturnsNoContent()
    {
        SetupCallerAsOrganizer();
        var trip = MakeTrip();
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.Delete(TripId);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.DeleteTripAsync(trip), Times.Once);
        _tripService.Verify(s => s.UpdateTripAsync(It.IsAny<Trip>(), It.IsAny<UpdateTripRequest>()), Times.Never);
    }

    [Fact]
    public async Task Delete_NotAnOrganiser_ReturnsForbidAndDoesNotDelete()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));

        var result = await _controller.Delete(TripId);

        Assert.IsType<ForbidResult>(result);
        _tripService.Verify(s => s.DeleteTripAsync(It.IsAny<Trip>()), Times.Never);
    }

    [Fact]
    public async Task Delete_TripMissing_ReturnsNotFound()
    {
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync((Trip?)null);

        var result = await _controller.Delete(TripId);

        Assert.IsType<NotFoundResult>(result);
        _tripService.Verify(s => s.DeleteTripAsync(It.IsAny<Trip>()), Times.Never);
    }

    // ======================= Create (assumes fix 3) =======================

    [Fact]
    public async Task Create_PassesRequestAndCallerIdToService_ReturnsCreated()
    {
        var request = MakeCreateRequest();
        var created = MakeTrip(42);
        created.Name = "Alps";
        created.MaxParticipants = 8;
        created.Live = false;
        _tripService.Setup(s => s.CreateAsync(request, CallerId)).ReturnsAsync(created);

        var result = await _controller.Create(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(TripsController.Get), createdResult.ActionName);
        Assert.Equal(42, createdResult.RouteValues!["tripId"]);

        var body = Assert.IsType<TripResponse>(createdResult.Value);
        Assert.Equal(42, body.Id);
        Assert.Equal("Alps", body.Name);
        Assert.Equal(8, body.MaxParticipants);
        Assert.False(body.Live);

        _tripService.Verify(s => s.CreateAsync(request, CallerId), Times.Once);
    }

    // ======================= AddTripUser (assumes fix 2) =======================

    [Fact]
    public async Task AddTripUser_SelfJoinAsCustomer_DoesNotRequireOrganizer()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(false);
        var request = new AddTripUserRequest { UserId = CallerId, Role = TripRole.Customer };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<CreatedAtActionResult>(result);
        _tripService.Verify(s => s.AddUserAsync(TripId, CallerId, TripRole.Customer), Times.Once);
        _tripService.Verify(s => s.GetUserRoleOnTripAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task AddTripUser_OrganizerAddsAnotherUser_AddsWithRequestedRole()
    {
        const int otherId = 2;
        SetupCallerAsOrganizer();
        _userService.Setup(s => s.GetUserAsync(otherId)).ReturnsAsync(MakeUser(otherId, organiser: true));
        _tripService.Setup(s => s.UserOnTripAsync(TripId, otherId)).ReturnsAsync(false);
        var request = new AddTripUserRequest { UserId = otherId, Role = TripRole.Organiser };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<CreatedAtActionResult>(result);
        _tripService.Verify(s => s.AddUserAsync(TripId, otherId, TripRole.Organiser), Times.Once);
    }

    [Fact]
    public async Task AddTripUser_NonOrganizerAddsAnotherUser_ReturnsForbid()
    {
        const int otherId = 2;
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));
        var request = new AddTripUserRequest { UserId = otherId, Role = TripRole.Customer };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<ForbidResult>(result);
        _tripService.Verify(s => s.AddUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TripRole>()), Times.Never);
    }

    [Fact]
    public async Task AddTripUser_NonOrganizerSelfPromotesToOrganiser_ReturnsForbid()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));
        var request = new AddTripUserRequest { UserId = CallerId, Role = TripRole.Organiser };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<ForbidResult>(result);
        _tripService.Verify(s => s.AddUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TripRole>()), Times.Never);
    }

    [Fact]
    public async Task AddTripUser_TargetUserMissing_ReturnsNotFound()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync((User?)null);
        var request = new AddTripUserRequest { UserId = CallerId, Role = TripRole.Customer };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<NotFoundObjectResult>(result);
        _tripService.Verify(s => s.AddUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TripRole>()), Times.Never);
    }

    [Fact]
    public async Task AddTripUser_AlreadyOnTrip_ReturnsConflict()
    {
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        var request = new AddTripUserRequest { UserId = CallerId, Role = TripRole.Customer };

        var result = await _controller.AddTripUser(TripId, request);

        Assert.IsType<ConflictObjectResult>(result);
        _tripService.Verify(s => s.AddUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<TripRole>()), Times.Never);
    }

    // ======================= RemoveParticipant =======================

    [Fact]
    public async Task RemoveParticipant_UserLeavesOwnTrip_DoesNotRequireOrganizer()
    {
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, CallerId)).ReturnsAsync(TripRole.Customer);
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(MakeTrip());

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(TripId, CallerId), Times.Once);
        _userService.Verify(s => s.GetUserAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveParticipant_OrganizerRemovesAnotherUser_Succeeds()
    {
        const int otherId = 2;
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.UserOnTripAsync(TripId, otherId)).ReturnsAsync(true);
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, otherId)).ReturnsAsync(TripRole.Customer);
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(MakeTrip());

        var result = await _controller.RemoveParticipant(TripId, otherId);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(TripId, otherId), Times.Once);
    }

    [Fact]
    public async Task RemoveParticipant_NonOrganizerRemovesAnotherUser_ReturnsForbid()
    {
        const int otherId = 2;
        _userService.Setup(s => s.GetUserAsync(CallerId)).ReturnsAsync(MakeUser(CallerId, organiser: false));

        var result = await _controller.RemoveParticipant(TripId, otherId);

        Assert.IsType<ForbidResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveParticipant_UserNotOnTrip_ReturnsConflict()
    {
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(false);

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<ConflictObjectResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveParticipant_OnTripButRoleIsNull_ReturnsConflict()
    {
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, CallerId)).ReturnsAsync((TripRole?)null);

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task RemoveParticipant_TripMissing_ReturnsNotFound()
    {
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, CallerId)).ReturnsAsync(TripRole.Customer);
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync((Trip?)null);

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task RemoveParticipant_LastOrganiserLeaving_ReturnsConflict()
    {
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        var trip = MakeTrip(TripId,
            MakeUserTrip(TripRole.Organiser),
            MakeUserTrip(TripRole.Customer));
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<ConflictObjectResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemoveParticipant_OrganiserLeavesWhenAnotherOrganiserExists_Succeeds()
    {
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.UserOnTripAsync(TripId, CallerId)).ReturnsAsync(true);
        var trip = MakeTrip(TripId,
            MakeUserTrip(TripRole.Organiser),
            MakeUserTrip(TripRole.Organiser));
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.RemoveParticipant(TripId, CallerId);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(TripId, CallerId), Times.Once);
    }

    [Fact]
    public async Task RemoveParticipant_RemovingCustomerWhenOnlyOneOrganiser_Succeeds()
    {
        const int otherId = 2;
        SetupCallerAsOrganizer();
        _tripService.Setup(s => s.UserOnTripAsync(TripId, otherId)).ReturnsAsync(true);
        _tripService.Setup(s => s.GetUserRoleOnTripAsync(TripId, otherId)).ReturnsAsync(TripRole.Customer);
        var trip = MakeTrip(TripId,
            MakeUserTrip(TripRole.Organiser),
            MakeUserTrip(TripRole.Customer));
        _tripService.Setup(s => s.GetTripAsync(TripId)).ReturnsAsync(trip);

        var result = await _controller.RemoveParticipant(TripId, otherId);

        Assert.IsType<NoContentResult>(result);
        _tripService.Verify(s => s.RemoveUserAsync(TripId, otherId), Times.Once);
    }
}