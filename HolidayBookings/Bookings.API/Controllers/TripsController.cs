using Azure.Core;
using Bookings.API.Models.Requests;
using Bookings.API.Models.Responses;
using Bookings.API.Services;
using Bookings.Data.Entities;
using Bookings.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;

namespace Bookings.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("trips")]
    public class TripsController : ControllerBase
    {
        private readonly TripService _tripService;
        private readonly UserService _userService;
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public TripsController(TripService tripService, UserService userService)
        {
            _tripService = tripService;
            _userService = userService;
        }

        // GET /trips/mine
        [HttpGet("mine")]
        public async Task<ActionResult<TripsResponse>> GetMine()
        {
            int userId = CurrentUserId;

            var trips = await _tripService.GetUserTripsAsync(userId);
            var tripsResponse = new TripsResponse
            {
                CustomerTrips = trips.Where(t => t.Role == TripRole.Customer)
                    .Select(t => new TripResponse {
                        Id = t.Trip.Id,
                        Name = t.Trip.Name,
                        MaxParticipants = t.Trip.MaxParticipants,
                        Live = t.Trip.Live
                    }).ToList(),
                OrganisedTrips = trips.Where(t => t.Role == TripRole.Organiser)
                    .Select(t => new TripResponse
                    {
                        Id = t.Trip.Id,
                        Name = t.Trip.Name,
                        MaxParticipants = t.Trip.MaxParticipants,
                        Live = t.Trip.Live
                    }).ToList()
            };

            return tripsResponse;
        }

        [HttpGet("{tripId:int}")]
        public async Task<ActionResult<TripResponse>> Get(int tripId)
        {
            var userId = CurrentUserId;

            var trip = await _tripService.GetTripAsync(tripId);

            if (trip is null) return NotFound();

            var tripResponse = new TripResponse
            {
                Id = trip.Id,
                Name = trip.Name,
                MaxParticipants = trip.MaxParticipants,
                Live = trip.Live
            };

            return tripResponse;
        }

        // GET /trips/5/destinations
        [HttpGet("{tripId:int}/destinations")]
        public async Task<ActionResult<List<DestinationResponse>>> GetDestinations(int tripId)
        {
            var trip = await _tripService.GetTripAsync(tripId);

            if (trip is null) return NotFound();

            var destinations = trip.Destinations.Select(d => new DestinationResponse
                {
                    Id = d.Id,
                    Name = d.Name,
                    Country = d.Country,
                    Arrival = d.Arrival,
                    Departure = d.Departure
                }).ToList();

            return destinations;
        }

        // PUT /trips/5
        [HttpPut("{tripId:int}")]
        public async Task<IActionResult> Update(int tripId, UpdateTripRequest request)
        {
            var denied = await RequireOrganizerAsync(tripId);
            if (denied is not null) return denied;

            var trip = await _tripService.GetTripAsync(tripId);
            if (trip is null) return NotFound();

            await _tripService.UpdateTripAsync(trip, request);

            return NoContent();
        }

        // DELETE /trips/5
        [HttpDelete("{tripId:int}")]
        public async Task<IActionResult> Delete(int tripId, UpdateTripRequest request)
        {
            var denied = await RequireOrganizerAsync(tripId);
            if (denied is not null) return denied;

            var trip = await _tripService.GetTripAsync(tripId);
            if (trip is null) return NotFound();

            await _tripService.UpdateTripAsync(trip, request);

            return NoContent();
        }

        // POST /trips
        [HttpPost]
        public async Task<ActionResult<TripResponse>> Create(CreateTripRequest request)
        {
            var trip = await _tripService.CreateAsync(request, CurrentUserId);

            return CreatedAtAction(nameof(Create), new { tripId = trip.Id },
                new TripResponse{
                    Id = trip.Id,
                    Name = trip.Name,
                    Live = trip.Live,
                    MaxParticipants = trip.MaxParticipants
                });
        }

        // POST /trips/5/tripUser
        [HttpPost("{tripId:int}/tripUser")]
        public async Task<IActionResult> AddTripUser(int tripId, AddTripUserRequest request)
        {
            // If the user being added to the trip is not the logged in user,
            // or the trip role is not a customer
            // then the user must be an organiser of the trip
            if (request.userId != CurrentUserId || request.Role != TripRole.Customer)
            {
                var denied = await RequireOrganizerAsync(tripId);
                if (denied is not null) return denied;
            }

            var user = await _userService.GetUserAsync(request.userId);

            if (user is null) return NotFound($"User with id {request.userId} not found");    //User not found

            var alreadyOnTrip = await _tripService.UserOnTripAsync(tripId, request.userId);
            if (alreadyOnTrip) return Conflict("That user is already associated with this trip.");

            
            return CreatedAtAction(nameof(AddTripUser), new { tripId });
        }

        // DELETE /trips/5/tripUser/10
        [HttpDelete("{tripId:int}/tripUser/{userId:int}")]
        public async Task<IActionResult> RemoveParticipant(int tripId, int userId)
        {
            // If the user being removed from the trip is not the logged in user,
            // then the user must be an organiser of the trip
            if (userId != CurrentUserId)
            {
                var denied = await RequireOrganizerAsync(tripId);
                if (denied is not null) return denied;
            }

            var onTrip = await _tripService.UserOnTripAsync(tripId, userId);
            var roleOnTrip = await _tripService.GetUserRoleOnTripAsync(tripId, userId);
            if (!onTrip || roleOnTrip is null) return Conflict("That user is not associated with this trip.");

            var trip = await _tripService.GetTripAsync(tripId);

            if (trip is null) return NotFound("The requsted trip could not be found");

            var isLastOrganiser = roleOnTrip == TripRole.Organiser 
                && trip.TripUsers.Count(tu => tu.Role == TripRole.Organiser) == 1;

            if (isLastOrganiser)
                return Conflict("A trip must keep at least one organiser. Add another organiser first.");

            await _tripService.RemoveUserAsync(tripId, userId);

            return NoContent();
        }

        private async Task<IActionResult?> RequireOrganizerAsync(int tripId)
        {
            var userId = CurrentUserId;

            var user = await _userService.GetUserAsync(userId);

            if (user is null) return NotFound();    //User not found
            if (!user.Organiser) return Forbid();   //User is not an Organiser

            var role = await _tripService.GetUserRoleOnTripAsync(tripId, userId);

            if (role is null) return NotFound();               // not on the trip or trip does not exist
            if (role != TripRole.Organiser) return Forbid();   // on the trip but not allowed to perform action
            return null;
        }
    }
}
