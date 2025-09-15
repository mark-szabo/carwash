using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ViewModels;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Attributes;
using CarWash.PWA.Hubs;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing reservations
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController(
        IOptionsMonitor<CarWashConfiguration> configuration,
        ApplicationDbContext context,
        IUserService userService,
        IReservationService reservationService,
        IHubContext<BacklogHub> backlogHub,
        TelemetryClient telemetryClient) : ControllerBase
    {
        private readonly IOptionsMonitor<CarWashConfiguration> _configuration = configuration;
        private readonly ApplicationDbContext _context = context;
        private readonly User _user = userService.CurrentUser;
        private readonly IReservationService _reservationService = reservationService;
        private readonly IHubContext<BacklogHub> _backlogHub = backlogHub;
        private readonly TelemetryClient _telemetryClient = telemetryClient;

        // GET: api/reservations
        /// <summary>
        /// Get my reservations
        /// </summary>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet]
        public IEnumerable<ReservationViewModel> GetReservations()
        {
            return _context.Reservation
                .Include(r => r.KeyLockerBox)
                .Where(r => r.UserId == _user.Id)
                .OrderByDescending(r => r.StartDate)
                .Select(reservation => new ReservationViewModel(reservation));
        }

        // GET: api/reservations/{id}
        /// <summary>
        /// Get a specific reservation by id
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns><see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formatted.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to get another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpGet("{id}")]
        public async Task<ActionResult<ReservationViewModel>> GetReservation([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            if (reservation.UserId != _user.Id && !((_user.IsAdmin && reservation.User.Company == _user.Company) || _user.IsCarwashAdmin)) return Forbid();

            return Ok(new ReservationViewModel(reservation));
        }

        // PUT: api/reservations/{id}
        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="id">Reservation id</param>
        /// <param name="reservation"><see cref="Reservation"/></param>
        /// <returns>No content</returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if no service choosen / StartDate and EndDate isn't on the same day / a Date is in the past / StartDate and EndDate are not valid slot start/end times / user/company limit has been met / there is no more time in that slot.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to update another user's reservation.</response>
        [UserAction]
        [HttpPut("{id}")]
        public async Task<ActionResult<ReservationViewModel>> PutReservation([FromRoute] string id, [FromBody] Reservation reservation)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id != reservation.Id) return BadRequest();

            var dbReservation = await _context.Reservation.FindAsync(id);
            if (dbReservation == null) return NotFound();

            try
            {
                // Validate the reservation
                var validationResult = await _reservationService.ValidateReservationAsync(reservation, true, _user, id);
                if (!validationResult.IsValid)
                    return BadRequest(validationResult.ErrorMessage);

                // Update the reservation
                var updatedReservation = await _reservationService.UpdateReservationAsync(reservation, _user);

                // Broadcast SignalR update
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, updatedReservation.Id);

                return Ok(new ReservationViewModel(updatedReservation));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Reservation.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        // POST: api/reservations
        /// <summary>
        /// Create a new reservation
        /// </summary>
        /// <param name="reservation"><see cref="Reservation"/></param>
        /// <returns>The newly created <see cref="Reservation"/></returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if no service chosen / StartDate and EndDate isn't on the same day / a Date is in the past / StartDate and EndDate are not valid slot start/end times / user/company limit has been met / there is no more time in that slot.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to reserve for another user.</response>
        [UserAction]
        [HttpPost]
        public async Task<ActionResult<ReservationViewModel>> PostReservation([FromBody] Reservation reservation)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                // Set defaults before validation
                reservation.UserId ??= _user.Id;

                // Validate the reservation
                var validationResult = await _reservationService.ValidateReservationAsync(reservation, false, _user);
                if (!validationResult.IsValid)
                    return BadRequest(validationResult.ErrorMessage);

                // Create the reservation
                var createdReservation = await _reservationService.CreateReservationAsync(reservation, _user);

                // Broadcast SignalR update
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationCreated, createdReservation.Id);

                return CreatedAtAction(nameof(GetReservation), new { id = createdReservation.Id }, new ReservationViewModel(createdReservation));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid();
            }
        }

        // DELETE: api/reservations/{id}
        /// <summary>
        /// Delete an existing reservation
        /// </summary>
        /// <param name="id">Reservation id</param>
        /// <returns>No content</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to delete another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ReservationViewModel>> DeleteReservation([FromRoute] string id)
        {
            try
            {
                var deletedReservation = await _reservationService.DeleteReservationAsync(id, _user);

                // Broadcast SignalR update
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationDeleted, id);

                return Ok(new ReservationViewModel(deletedReservation));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // GET: api/reservations/company
        /// <summary>
        /// Get reservations for the company (admin only)
        /// </summary>
        /// <returns>List of <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [HttpGet, Route("company")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetCompanyReservations()
        {
            if (!_user.IsAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .Where(r => r.User.Company == _user.Company)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();

            return Ok(reservations.Select(r => new AdminReservationViewModel(r)));
        }

        // GET: api/reservations/backlog
        /// <summary>
        /// Get reservations in backlog (carwash admin only)
        /// </summary>
        /// <returns>List of <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        [HttpGet, Route("backlog")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetBacklog()
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .Where(r => r.State == State.DropoffAndLocationConfirmed ||
                           r.State == State.ReminderSentWaitingForKey ||
                           r.State == State.WashInProgress)
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            return Ok(reservations.Select(r => new AdminReservationViewModel(r)));
        }

        // POST: api/reservations/{id}/confirm-dropoff
        /// <summary>
        /// Confirm dropoff and location
        /// </summary>
        /// <param name="id">Reservation id</param>
        /// <param name="location">Car location</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or location param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to update another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/confirm-dropoff")]
        public async Task<IActionResult> ConfirmDropoff([FromRoute] string id, [FromBody] string location)
        {
            try
            {
                await _reservationService.ConfirmDropoffAsync(id, location, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/confirm-dropoff-by-email
        /// <summary>
        /// Confirm car key dropoff and location of user's next reservation (service endpoint)
        /// </summary>
        /// <param name="model">Model containing user email and car location.</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if email or location param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="404">NotFound if user does not exist or if there's no reservation where the key can be dropped off.</response>
        /// <response code="409">Conflict if user has more than one reservation where the key can be dropped off.</response>
        [ServiceAction]
        [HttpPost("next/confirmdropoff")]
        public async Task<IActionResult> ConfirmDropoffByEmail([FromBody] ConfirmDropoffByEmailViewModel model)
        {
            if (model?.Email == null) return BadRequest("Email cannot be null.");
            if (model.Location == null) return BadRequest("Reservation location cannot be null.");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) return NotFound($"No user found with email address '{model.Email}'.");

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.User.Email == user.Email && (r.State == State.SubmittedNotActual || r.State == State.ReminderSentWaitingForKey))
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            if (reservations == null || reservations.Count == 0) return NotFound("No reservations found.");

            Reservation reservation;
            string reservationResolution;
            // Only one active - straightforward
            if (reservations.Count == 1)
            {
                reservation = reservations[0];
                reservationResolution = "Only one active reservation.";
            }
            // Vehicle plate number was specified and only one reservation for that car - easy
            else if (model.VehiclePlateNumber?.ToUpper() != null && reservations.Count(r => r.VehiclePlateNumber == model.VehiclePlateNumber) == 1)
            {
                reservation = reservations.Single(r => r.VehiclePlateNumber == model.VehiclePlateNumber.ToUpper());
                reservationResolution = "Vehicle plate number was specified and only one reservation exists for that car.";
            }
            // Only one where we are waiting for the key - still pretty straightforward
            else if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey) == 1)
            {
                reservation = reservations.Single(r => r.State == State.ReminderSentWaitingForKey);
                reservationResolution = "Only one reservation is waiting for the key.";
            }
            // Only one today - probably fine
            else if (reservations.Count(r => r.StartDate.Date == DateTime.UtcNow.Date) == 1)
            {
                reservation = reservations.Single(r => r.StartDate.Date == DateTime.UtcNow.Date);
                reservationResolution = "Only one reservation today.";
            }
            else if (model.VehiclePlateNumber == null)
            {
                return Conflict("More than one reservation found where the reservation state is submitted or waiting for key. Please specify vehicle plate number!");
            }
            else
            {
                return Conflict("More than one reservation found where the reservation state is submitted or waiting for key.");
            }

            await _reservationService.ConfirmDropoffAsync(reservation.Id, model.Location, reservation.User);
            await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, reservation.Id);

            _telemetryClient.TrackEvent("ConfirmDropoffByEmail", new Dictionary<string, string>
            {
                { "Email", model.Email },
                { "Resolution", reservationResolution }
            });

            return NoContent();
        }

        // POST: api/reservations/{id}/startwash
        /// <summary>
        /// Log the start of the carwash
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/startwash")]
        public async Task<IActionResult> StartWash([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");

            try
            {
                await _reservationService.StartWashAsync(id, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/completewash
        /// <summary>
        /// Log the completion of the carwash
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/completewash")]
        public async Task<IActionResult> CompleteWash([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");

            try
            {
                await _reservationService.CompleteWashAsync(id, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/confirmpayment
        /// <summary>
        /// Confirm payment
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/confirmpayment")]
        public async Task<IActionResult> ConfirmPayment([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");

            try
            {
                await _reservationService.ConfirmPaymentAsync(id, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/state/{state}
        /// <summary>
        /// Set new state
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="state">state no.</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or location param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/state/{state}")]
        public async Task<IActionResult> SetState([FromRoute] string id, [FromRoute] State state)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");

            try
            {
                await _reservationService.SetReservationStateAsync(id, state, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/carwashcomment
        /// <summary>
        /// Add a carwash comment to a reservation
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="comment">comment to be added</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or comment is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/carwashcomment")]
        public async Task<IActionResult> AddCarwashComment([FromRoute] string id, [FromBody] string comment)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (comment == null) return BadRequest("Comment cannot be null.");

            try
            {
                await _reservationService.AddCommentAsync(id, comment, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/comment
        /// <summary>
        /// Add a comment to a reservation
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="comment">comment to be added</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or comment is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/comment")]
        public async Task<IActionResult> AddComment([FromRoute] string id, [FromBody] string comment)
        {
            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (comment == null) return BadRequest("Comment cannot be null.");

            try
            {
                await _reservationService.AddCommentAsync(id, comment, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/mpv
        /// <summary>
        /// Change wether a car is MPV or not
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="mpv">the new value (true/false)</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/mpv")]
        public async Task<IActionResult> SetMpv([FromRoute] string id, [FromBody] bool mpv)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");

            try
            {
                await _reservationService.SetMpvAsync(id, mpv, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/services
        /// <summary>
        /// Update selected services
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="services">new services</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or services param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/services")]
        public async Task<IActionResult> UpdateServices([FromRoute] string id, [FromBody] List<int> services)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (services == null) return BadRequest("Services param cannot be null.");

            try
            {
                await _reservationService.UpdateServicesAsync(id, services, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/{id}/location
        /// <summary>
        /// Update car location
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="location">new location</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or services param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpPost("{id}/location")]
        public async Task<IActionResult> UpdateLocation([FromRoute] string id, [FromBody] string location)
        {
            if (!_user.IsCarwashAdmin) return Forbid();
            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (location == null) return BadRequest("Location cannot be null.");

            try
            {
                await _reservationService.UpdateLocationAsync(id, location, _user);
                await _backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);
                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        // GET: api/reservations/not-available-dates-and-times
        /// <summary>
        /// Get not available dates and times
        /// </summary>
        /// <param name="daysAhead">Number of days ahead to check</param>
        /// <returns>Not available dates and times</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("not-available-dates-and-times")]
        public async Task<NotAvailableDatesAndTimesViewModel> GetNotAvailableDatesAndTimes(int daysAhead = 365)
        {
            return await _reservationService.GetNotAvailableDatesAndTimesAsync(_user, daysAhead);
        }

        // GET: api/reservations/last-settings
        /// <summary>
        /// Get last settings
        /// </summary>
        /// <returns>Last settings</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("last-settings")]
        public async Task<ActionResult<LastSettingsViewModel>> GetLastSettings()
        {
            var lastSettings = await _reservationService.GetLastSettingsAsync(_user);
            return lastSettings != null ? Ok(lastSettings) : NotFound();
        }

        // GET: api/reservations/capacity
        /// <summary>
        /// Get reservation capacity for a date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>Reservation capacity</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("capacity")]
        public async Task<ActionResult<IEnumerable<ReservationCapacityViewModel>>> GetReservationCapacity(DateTime date)
        {
            var capacity = await _reservationService.GetReservationCapacityAsync(date);
            return Ok(capacity);
        }

        // GET: api/reservations/export
        /// <summary>
        /// Export reservations to Excel
        /// </summary>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Excel file</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [UserAction]
        [HttpGet, Route("export")]
        public async Task<IActionResult> Export(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var startDateNonNull = startDate ?? DateTime.UtcNow.Date.AddMonths(-1);
                var endDateNonNull = endDate ?? DateTime.UtcNow.Date;
                
                var excelData = await _reservationService.ExportReservationsAsync(_user, startDate, endDate);
                var stream = new MemoryStream(excelData);
                
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                           $"carwash-export-{startDateNonNull.Year}-{startDateNonNull.Month}-{startDateNonNull.Day}.xlsx");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}