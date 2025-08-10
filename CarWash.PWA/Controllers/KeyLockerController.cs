#nullable enable
using CarWash.ClassLibrary;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Hubs;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Key Locker API
    /// </summary>
    [Produces("application/json")]
    [Route("api/keylocker")]
    [Authorize]
    [ApiController]
    public class KeyLockerController(
        IOptionsMonitor<CarWashConfiguration> configuration,
        ApplicationDbContext context,
        IUserService userService,
        IKeyLockerService keyLockerService,
        IHubContext<KeyLockerHub> keyLockerHub,
        TelemetryClient telemetryClient) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser ?? throw new Exception("User is not authenticated.");

        // GET: api/keylocker/generate
        /// <summary>
        /// Generate boxes for a locker.
        /// </summary>
        /// <param name="request"> The request containing the details for box generation.</param>
        /// <returns>200 OK if boxes were generated successfully, 403 Forbidden if the user is not a carwash admin.</returns>
        /// <response code="204">OK</response>
        /// <response code="400">BadRequest if the request parameters are invalid.</response>
        /// <response code="403">Forbidden if the user is not a carwash admin.</response>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBoxes([FromBody] GenerateBoxesRequest request)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (string.IsNullOrEmpty(request.NamePrefix) || request.NumberOfBoxes <= 0 || string.IsNullOrEmpty(request.Building) || string.IsNullOrEmpty(request.Floor))
            {
                return BadRequest("Invalid request parameters.");
            }

            await keyLockerService.GenerateBoxesToLocker(
                request.NamePrefix,
                request.NumberOfBoxes,
                request.Building,
                request.Floor,
                request.LockerId);

            return NoContent();
        }

        // POST: api/keylocker/box/{id}/open
        /// <summary>
        /// Open a box by its unique ID.
        /// </summary>
        /// <param name="id"> The unique ID of the box to open.</param>
        /// <returns>200 OK if the box was opened successfully, 400 Bad Request if the ID is null or empty, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <response code="204">OK</response>
        /// <response code="400">BadRequest if the box ID is null or empty.</response>
        /// <response code="403">Forbidden if the user is not a carwash admin.</response>
        [HttpPost("box/{id}/open")]
        public async Task<IActionResult> OpenBoxById([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Box ID cannot be null or empty.");
            }

            try
            {
                var box = await keyLockerService.OpenBoxByIdAsync(id, _user.Id);

                await keyLockerHub.Clients.All.SendAsync(Constants.KeyLockerHubMethods.KeyLockerBoxOpened, box.Id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 Internal Server Error
                telemetryClient.TrackException(ex);
                return StatusCode(500, "An error occurred while trying to open the box.");
            }

            return NoContent();
        }

        // POST: api/keylocker/{lockerId}/{boxSerial}/open
        /// <summary>
        /// Open a box by locker ID and box serial.
        /// </summary>
        /// <param name="lockerId">The ID of the locker containing the box.</param>
        /// <param name="boxSerial">The serial number of the box to open.</param>
        /// <returns>200 OK if the box was opened successfully, 400 Bad Request if the locker ID or box serial is invalid, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <reponse code="204">OK</reponse>
        /// <response code="400">BadRequest if the locker ID or box serial is invalid.</response>
        /// <response code="403">Forbidden if the user is not a carwash admin.</response>
        [HttpPost("{lockerId}/{boxSerial}/open")]
        public async Task<IActionResult> OpenBoxBySerial([FromRoute] string lockerId, [FromRoute] int boxSerial)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (string.IsNullOrEmpty(lockerId) || boxSerial <= 0)
            {
                return BadRequest("Invalid locker ID or box serial.");
            }

            try
            {
                var box = await keyLockerService.OpenBoxBySerialAsync(lockerId, boxSerial, _user.Id);

                await keyLockerHub.Clients.All.SendAsync(Constants.KeyLockerHubMethods.KeyLockerBoxOpened, box.Id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 Internal Server Error
                telemetryClient.TrackException(ex);
                return StatusCode(500, "An error occurred while trying to open the box.");
            }

            return NoContent();
        }

        /*// POST: api/keylocker/{lockerId}/available/open
        /// <summary>
        /// Open a random available box in a locker and optionally update a reservation with the opened box.
        /// </summary>
        /// <param name="lockerId"> The ID of the locker containing the boxes.</param>
        /// <param name="reservationId"> (Optional) The ID of the reservation to update with the opened box.</param>
        /// <returns>200 OK with the ID of the opened box if successful, 400 Bad Request if the locker ID is invalid, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <response code="200">OK with the the opened box.</response>
        /// <response code="400">BadRequest if the locker ID is invalid.</response>
        [HttpPost("{lockerId}/available/open")]
        public async Task<ActionResult<string>> OpenRandomAvailableBox([FromRoute] string lockerId, [FromQuery] string? reservationId = null)
        {
            if (string.IsNullOrEmpty(lockerId))
            {
                return BadRequest("Invalid locker ID.");
            }

            try
            {
                var box = await keyLockerService.OpenRandomAvailableBoxAsync(lockerId, _user.Id);

                if (!string.IsNullOrEmpty(reservationId))
                {
                    var reservation = await context.Reservation.FirstOrDefaultAsync(r => r.Id == reservationId);
                    if (reservation == null)
                    {
                        return NotFound($"Reservation with ID '{reservationId}' not found.");
                    }

                    reservation.KeyLockerBoxId = box.Id;
                    context.Reservation.Update(reservation);
                    await context.SaveChangesAsync();
                }

                return Ok(new BoxResponse(box.Id, box.BoxSerial, box.Building, box.Floor, box.Name));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 Internal Server Error
                telemetryClient.TrackException(ex);
                return StatusCode(500, "An error occurred while trying to open the box.");
            }
        }*/

        // POST: api/keylocker/open/available
        /// <summary>
        /// Open a random available box in a locker and update a reservation with the opened box.
        /// </summary>
        /// <param name="reservationId"> The ID of the reservation to update with the opened box.</param>
        /// <returns>200 OK with the ID of the opened box if successful, 400 Bad Request if the locker ID is invalid, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <response code="200">OK with the opened box.</response>
        /// <response code="400">BadRequest if the locker ID is invalid.</response>
        [HttpPost("open/available")]
        public async Task<ActionResult<string>> OpenRandomAvailableBox([FromQuery] string reservationId, [FromQuery] string location)
        {
            if (string.IsNullOrEmpty(reservationId)) return BadRequest("Invalid reservation ID.");
            if (string.IsNullOrEmpty(location)) return BadRequest("Invalid location.");

            var reservation = await context.Reservation.FirstOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null)
            {
                return NotFound($"Reservation with ID '{reservationId}' not found.");
            }

            reservation.Location = location;

            if (reservation.Building == null)
            {
                return BadRequest("Location is not set for reservation.");
            }

            try
            {
                var lockerId = configuration.CurrentValue.Garages.Find(g => g.Building == reservation.Building)?.KeyLockerId ?? throw new Exception($"No key locker id was found for building '{reservation.Building}'.");

                var box = await keyLockerService.OpenRandomAvailableBoxAsync(lockerId, _user.Id, async id =>
                {
                    // Callback when the box is closed
                    if (reservation != null)
                    {
                        await keyLockerHub.Clients.User(_user.Id).SendAsync(Constants.KeyLockerHubMethods.KeyLockerBoxClosed, id);
                    }

                    return;
                });

                await keyLockerHub.Clients.All.SendAsync(Constants.KeyLockerHubMethods.KeyLockerBoxOpened, box.Id);

                reservation.KeyLockerBoxId = box.Id;
                await context.SaveChangesAsync();

                return Ok(new BoxResponse(box.Id, box.BoxSerial, box.Building, box.Floor, box.Name));
            }
            catch (InvalidOperationException ex)
            {
                await context.SaveChangesAsync();

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                await context.SaveChangesAsync();

                // Log the exception and return a 500 Internal Server Error
                telemetryClient.TrackException(ex);

                return StatusCode(500, "An error occurred while trying to open the box.");
            }
        }

        // POST api/keylocker/open/by-reservation
        /// <summary>
        /// Open a box by reservation ID.
        /// </summary>
        /// <param name="reservationId"> The ID of the reservation.</param>
        /// <returns>200 OK if the box was opened successfully, 404 Not Found if the reservation does not exist, 400 Bad Request if the reservation does not have an associated key locker box, or 403 Forbidden if the user is not authorized to open the box.</returns>
        /// <response code="204">OK</response>
        /// <response code="400">BadRequest if the reservation does not have an associated key locker box.</response>
        /// <response code="403">Forbidden if the user is not authorized to open the box.</response>
        /// <response code="404">NotFound if the reservation with the specified ID does not exist.</response>
        [HttpPost("open/by-reservation")]
        public async Task<IActionResult> OpenBoxByReservationId([FromQuery] string reservationId)
        {
            // Find the reservation by ID
            var reservation = await context.Reservation
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return NotFound($"Reservation with ID '{reservationId}' not found.");
            }

            if (string.IsNullOrEmpty(reservation.KeyLockerBoxId))
            {
                return BadRequest("Reservation does not have an associated key locker box.");
            }

            // Only allow if current user is CarWash admin or the reservation's user
            if (reservation.UserId != _user.Id && !_user.IsCarwashAdmin)
            {
                return Forbid();
            }

            var box = await keyLockerService.OpenBoxByIdAsync(reservation.KeyLockerBoxId, _user.Id);

            await keyLockerHub.Clients.All.SendAsync(Constants.KeyLockerHubMethods.KeyLockerBoxOpened, box.Id);

            return NoContent();
        }

        // POST: api/keylocker/free/by-reservation
        /// <summary>
        /// Frees up a key locker box by reservation ID.
        /// </summary>
        /// <param name="reservationId">The ID of the reservation.</param>
        /// <returns>200 OK if the box was freed, 404 Not Found if the reservation does not exist, 400 Bad Request if the reservation does not have an associated key locker box, or 403 Forbidden if the user is not authorized.</returns>
        /// <response code="204">OK</response>
        /// <response code="400">BadRequest if the reservation does not have an associated key locker box.</response>
        /// <response code="403">Forbidden if the user is not authorized to free the box.</response>
        /// <response code="404">NotFound if the reservation with the specified ID does not exist.</response>
        [HttpPost("free/by-reservation")]
        public async Task<IActionResult> FreeUpBoxByReservationId([FromQuery] string reservationId)
        {
            // Find the reservation by ID
            var reservation = await context.Reservation
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
            {
                return NotFound($"Reservation with ID '{reservationId}' not found.");
            }

            if (string.IsNullOrEmpty(reservation.KeyLockerBoxId))
            {
                return BadRequest("Reservation does not have an associated key locker box.");
            }

            // Only allow if current user is CarWash admin or the reservation's user
            if (reservation.UserId != _user.Id && !_user.IsCarwashAdmin)
            {
                return Forbid();
            }

            try
            {
                await keyLockerService.FreeUpBoxAsync(reservation.KeyLockerBoxId, _user.Id);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                return StatusCode(500, "An error occurred while trying to free up the box.");
            }

            return NoContent();
        }
    }

    /// <summary>
    /// Request model for generating key locker boxes.
    /// </summary>
    public class GenerateBoxesRequest
    {
        /// <summary>
        /// Prefix for the friendly name of each box.
        /// </summary>
        public string NamePrefix { get; set; } = default!;

        /// <summary>
        /// Number of boxes to generate.
        /// </summary>
        public int NumberOfBoxes { get; set; }

        /// <summary>
        /// Name of the building where the locker is located.
        /// </summary>
        public string Building { get; set; } = default!;

        /// <summary>
        /// Name of the floor where the locker is located.
        /// </summary>
        public string Floor { get; set; } = default!;

        /// <summary>
        /// Optional ID of an existing locker to add boxes to.
        /// </summary>
        public string? LockerId { get; set; }
    }

    /// <summary>
    /// ViewModel for a key locker box response.
    /// </summary>
    /// <param name="BoxId">The unique ID of the opened box.</param>
    /// <param name="BoxSerial">Serial number of the key locker box, incrementing from 1 to N.</param>
    /// <param name="Building">Name of the building where the key locker is located.</param>
    /// <param name="Floor">Name of the floor where the key locker is located.</param>
    /// <param name="Name">Friendly name of the box, used to identify it.</param>
    public record BoxResponse(string BoxId, int BoxSerial, string Building, string Floor, string Name);
}
