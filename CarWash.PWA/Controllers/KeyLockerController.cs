using System;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Key Locker API
    /// </summary>
    [Produces("application/json")]
    [Route("api/keylocker")]
    [ApiController]
    public class KeyLockerController(
        IOptionsMonitor<CarWashConfiguration> configuration,
        ApplicationDbContext context, 
        IUserService userService,
        IKeyLockerService keyLockerService,
        TelemetryClient telemetryClient) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser;

        // GET: api/keylocker/generate
        /// <summary>
        /// Generate boxes for a locker.
        /// </summary>
        /// <param name="request"> The request containing the details for box generation.</param>
        /// <returns>200 OK if boxes were generated successfully, 403 Forbidden if the user is not a carwash admin.</returns>
        /// <response code="200">OK</response>
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

            return Ok(); 
        }

        // POST: api/keylocker/box/{id}/open
        /// <summary>
        /// Open a box by its unique ID.
        /// </summary>
        /// <param name="id"> The unique ID of the box to open.</param>
        /// <returns>200 OK if the box was opened successfully, 400 Bad Request if the ID is null or empty, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <response code="200">OK</response>
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
                await keyLockerService.OpenBoxByIdAsync(id, _user.Id);
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

            return Ok();
        }

        // POST: api/keylocker/{lockerId}/{boxSerial}/open
        /// <summary>
        /// Open a box by locker ID and box serial.
        /// </summary>
        /// <param name="lockerId">The ID of the locker containing the box.</param>
        /// <param name="boxSerial">The serial number of the box to open.</param>
        /// <returns>200 OK if the box was opened successfully, 400 Bad Request if the locker ID or box serial is invalid, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <reponse code="200">OK</reponse>
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
                await keyLockerService.OpenBoxBySerialAsync(lockerId, boxSerial, _user.Id);
            }
            catch(InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 Internal Server Error
                telemetryClient.TrackException(ex);
                return StatusCode(500, "An error occurred while trying to open the box.");
            }

            return Ok();
        }

        // POST: api/keylocker/{lockerId}/available/open
        /// <summary>
        /// Open a random available box in a locker.
        /// </summary>
        /// <param name="lockerId"> The ID of the locker containing the boxes.</param>
        /// <returns>200 OK with the ID of the opened box if successful, 400 Bad Request if the locker ID is invalid, 403 Forbidden if the user is not a carwash admin, or 500 Internal Server Error if an unexpected error occurs.</returns>
        /// <response code="200">OK with the ID of the opened box.</response>
        /// <response code="400">BadRequest if the locker ID is invalid.</response>
        [HttpPost("{lockerId}/available/open")]
        public async Task<ActionResult<string>> OpenRandomAvailableBox([FromRoute] string lockerId)
        {
            if (string.IsNullOrEmpty(lockerId))
            {
                return BadRequest("Invalid locker ID.");
            }            

            try
            {
                var boxId = await keyLockerService.OpenRandomAvailableBoxAsync(lockerId, _user.Id);
                return Ok(boxId);
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
        }

        // POST api/keylocker/open/by-reservation
        /// <summary>
        /// Open a box by reservation ID.
        /// </summary>
        /// <param name="reservationId"> The ID of the reservation.</param>
        /// <returns>200 OK if the box was opened successfully, 404 Not Found if the reservation does not exist, 400 Bad Request if the reservation does not have an associated key locker box, or 403 Forbidden if the user is not authorized to open the box.</returns>
        /// <response code="200">OK</response>
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

            await keyLockerService.OpenBoxByIdAsync(reservation.KeyLockerBoxId, _user.Id);

            return Ok();
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
}
