using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.Exceptions;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Attributes;
using CarWash.PWA.Hubs;
using CarWash.PWA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
        IUserService userService,
        IReservationService reservationService,
        IHubContext<BacklogHub> backlogHub) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser;

        // GET: api/reservations
        /// <summary>
        /// Get my reservations
        /// </summary>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet]
        public async Task<IEnumerable<ReservationViewModel>> GetReservationsAsync()
        {
            return (await reservationService
                .GetReservationsOfUserAsync(_user))
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

            try
            {
                var reservation = await reservationService.GetReservationByIdAsync(id, _user);

                if (reservation == null) return NotFound();

                return Ok(new ReservationViewModel(reservation));
            }
            catch (UnauthorizedException ex)
            {
                return Forbid(ex.Message);
            }
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

            try
            {
                // Update the reservation
                var updatedReservation = await reservationService.UpdateReservationAsync(reservation, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, updatedReservation.Id);

                return Ok(new ReservationViewModel(updatedReservation));
            }
            catch (ReservationValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
            {
                return NotFound();
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
                // Create the reservation
                var createdReservation = await reservationService.CreateReservationAsync(reservation, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationCreated, createdReservation.Id);

                return CreatedAtAction(nameof(GetReservation), new { id = createdReservation.Id }, new ReservationViewModel(createdReservation));
            }
            catch (ReservationValidationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
        }

        // DELETE: api/reservations/{id}
        /// <summary>
        /// Delete an existing reservation
        /// </summary>
        /// <param name="id">Reservation id</param>
        /// <returns>The deleted <see cref="Reservation"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formatted.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to delete another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [UserAction]
        [HttpDelete("{id}")]
        public async Task<ActionResult<ReservationViewModel>> DeleteReservation([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var deletedReservation = await reservationService.DeleteReservationAsync(id, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationDeleted, id);

                return Ok(new ReservationViewModel(deletedReservation));
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
            {
                return NotFound();
            }
        }

        // GET: api/reservations/company
        /// <summary>
        /// Get reservations for the company (admin only)
        /// </summary>
        /// <returns>List of latest 100 <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [UserAction]
        [HttpGet, Route("company")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetCompanyReservations()
        {
            try
            {
                var reservations = await reservationService.GetReservationsOfCompanyAsync(_user);

                return Ok(reservations.Select(r => new AdminReservationViewModel(r)));
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
        }

        // GET: api/reservations/backlog
        /// <summary>
        /// Get reservations on backlog (carwash admin only)
        /// </summary>
        /// <returns>List of <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        [UserAction]
        [HttpGet, Route("backlog")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetBacklog()
        {
            try
            {
                var reservations = await reservationService.GetReservationsOnBacklog(_user);

                return Ok(reservations.Select(r => new AdminReservationViewModel(r)));
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
        }

        // POST: api/reservations/{id}/confirmdropoff
        /// <summary>
        /// Confirm car key dropoff and location
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
        [HttpPost("{id}/confirmdropoff")]
        public async Task<IActionResult> ConfirmDropoff([FromRoute] string id, [FromBody] string location)
        {
            try
            {
                await reservationService.ConfirmDropoffAsync(id, location, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationDropoffConfirmed, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
            {
                return NotFound();
            }
        }

        // POST: api/reservations/next/confirmdropoff
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
            try
            {
                var reservation = await reservationService.ConfirmDropoffByEmail(model.Email, model.Location, model.VehiclePlateNumber);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationDropoffConfirmed, reservation.Id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ReservationConflictException ex)
            {
                return Conflict(ex.Message);
            }
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
            try
            {
                await reservationService.StartWashAsync(id, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.CompleteWashAsync(id, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.ConfirmPaymentAsync(id, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.SetReservationStateAsync(id, state, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.AddCommentAsync(id, comment, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.SetMpvAsync(id, mpv, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.UpdateServicesAsync(id, services, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
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
            try
            {
                await reservationService.UpdateLocationAsync(id, location, _user);

                // Broadcast backlog update to all connected clients on SignalR
                await backlogHub.Clients.All.SendAsync(Constants.BacklogHubMethods.ReservationUpdated, id);

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
            catch (ReservationNotFoundException)
            {
                return NotFound();
            }
        }

        // GET: api/reservations/not-available-dates-and-times
        /// <summary>
        /// Get the list of future dates that are not available
        /// </summary>
        /// <param name="daysAhead">Number of days ahead to check</param>
        /// <returns>Not available dates and times</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("notavailabledates")]
        public async Task<NotAvailableDatesAndTimes> GetNotAvailableDatesAndTimes(int daysAhead = 365)
        {
            return await reservationService.GetNotAvailableDatesAndTimesAsync(_user, daysAhead);
        }

        // GET: api/reservations/lastsettings
        /// <summary>
        /// Get some settings from the last reservation made by the user to be used as defaults for a new reservation
        /// </summary>
        /// <returns>Last settings</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("lastsettings")]
        public async Task<ActionResult<LastSettings>> GetLastSettings()
        {
            var lastSettings = await reservationService.GetLastSettingsAsync(_user);

            return lastSettings != null ? Ok(lastSettings) : NoContent();
        }

        // GET: api/reservations/reservationcapacity
        /// <summary>
        /// Gets a list of slots and their free reservation capacity on a given date
        /// <param name="date">Date to check</param>
        /// <returns>Reservation capacity</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("reservationcapacity")]
        public async Task<ActionResult<IEnumerable<ReservationCapacity>>> GetReservationCapacity(DateTime date)
        {
            var capacity = await reservationService.GetReservationCapacityAsync(date);
            return Ok(capacity);
        }

        // GET: api/reservations/export
        /// <summary>
        /// Export a list of reservations to Excel for a given timespan
        /// </summary>
        /// <param name="startDate">start date (default: a month before today)</param>
        /// <param name="endDate">end date (default: today)</param>
        /// <returns>An Excel file of the list of reservations</returns>
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

                var excelData = await reservationService.ExportReservationsAsync(_user, startDateNonNull, endDateNonNull);
                var stream = new MemoryStream(excelData);

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           $"carwash-export-{startDateNonNull.Year}-{startDateNonNull.Month}-{startDateNonNull.Day}.xlsx");
            }
            catch (UnauthorizedException)
            {
                return Forbid();
            }
        }
    }
}