using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using CarWash.PWA.Attributes;
using CarWash.ClassLibrary;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing reservations
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/reservations")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly CarWashConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly IEmailService _emailService;
        private readonly ICalendarService _calendarService;
        private readonly IPushService _pushService;
        private readonly IBotService _botService;
        private readonly TelemetryClient _telemetryClient;

        /// <inheritdoc />
        public ReservationsController(CarWashConfiguration configuration, ApplicationDbContext context, IUsersController usersController, IEmailService emailService, ICalendarService calendarService, IPushService pushService, IBotService botService, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _context = context;
            _user = usersController.GetCurrentUser();
            _emailService = emailService;
            _calendarService = calendarService;
            _pushService = pushService;
            _botService = botService;
            _telemetryClient = telemetryClient;
        }

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

            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();

            return Ok(new ReservationViewModel(reservation));
        }

        // PUT: api/reservations/{id}
        /// <summary>
        /// Update an existing reservation
        /// </summary>
        /// <param name="id">Reservation id</param>
        /// <param name="reservation"><see cref="Reservation"/></param>
        /// <param name="dropoffConfirmed">Indicates key drop-off and location confirmation (default false)</param>
        /// <returns>No content</returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if no service choosen / StartDate and EndDate isn't on the same day / a Date is in the past / StartDate and EndDate are not valid slot start/end times / user/company limit has been met / there is no more time in that slot.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to update another user's reservation.</response>
        [UserAction]
        [HttpPut("{id}")]
        public async Task<ActionResult<ReservationViewModel>> PutReservation([FromRoute] string id, [FromBody] Reservation reservation, [FromQuery] bool dropoffConfirmed = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id != reservation.Id) return BadRequest();

            var dbReservation = await _context.Reservation.FindAsync(id);

            if (dbReservation == null) return NotFound();

            dbReservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty);
            dbReservation.Location = reservation.Location;
            dbReservation.Services = reservation.Services;
            dbReservation.Private = reservation.Private;
            var oldStartDate = dbReservation.StartDate;
            var newStartDate = reservation.StartDate.ToLocalTime();
            dbReservation.StartDate = newStartDate;
            if (reservation.EndDate != null) dbReservation.EndDate = ((DateTime)reservation.EndDate).ToLocalTime();
            else dbReservation.EndDate = null;
            dbReservation.Comment = reservation.Comment;

            try
            {
                dbReservation.EndDate = CalculateEndTime(dbReservation.StartDate, dbReservation.EndDate);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest("Reservation can be made to slots only.");
            }

            if (dropoffConfirmed)
            {
                if (dbReservation.Location == null) return BadRequest("Location must be set if drop-off pre-confirmed.");
                dbReservation.State = State.DropoffAndLocationConfirmed;
            }

            #region Input validation
            if (dbReservation.UserId != _user.Id && !_user.IsAdmin && !_user.IsCarwashAdmin) return Forbid();
            if (reservation.UserId != _user.Id && reservation.UserId != null)
            {
                if (_user.IsAdmin || _user.IsCarwashAdmin) dbReservation.UserId = reservation.UserId;
                else return BadRequest("Cannot modify user of registration. You need to re-create it.");
            }
            if (reservation.Services == null || reservation.Services.Count == 0) return BadRequest("No service chosen.");
            #endregion

            // Time requirement calculation
            dbReservation.TimeRequirement = dbReservation.Services.Contains(Constants.ServiceType.Carpet) ?
                _configuration.Reservation.CarpetCleaningMultiplier * _configuration.Reservation.TimeUnit :
                _configuration.Reservation.TimeUnit;

            #region Business validation
            // Checks whether start and end times are on the same day
            if (!IsStartAndEndTimeOnSameDay(dbReservation.StartDate, dbReservation.EndDate))
                return BadRequest("Reservation time range should be located entirely on the same day.");

            // Checks whether end time is later than start time
            if (!IsEndTimeLaterThanStartTime(dbReservation.StartDate, dbReservation.EndDate))
                return BadRequest("Reservation end time should be later than the start time.");

            // Checks whether start or end time is before the earliest allowed time
            if (IsInPast(dbReservation.StartDate, dbReservation.EndDate))
                return BadRequest("Cannot reserve in the past.");

            // Checks whether start or end times fit into a slot
            if (!IsInSlot(dbReservation.StartDate, dbReservation.EndDate))
                return BadRequest("Reservation can be made to slots only.");

            // Checks if the date/time is blocked
            if (await IsBlocked(dbReservation.StartDate, dbReservation.EndDate))
                return BadRequest("This time is blocked.");

            // Don't check if date&slot haven't changed
            if (newStartDate != oldStartDate)
            {
                // Check if there is enough time on that day
                if (!await IsEnoughTimeOnDateAsync(dbReservation.StartDate, dbReservation.TimeRequirement))
                    return BadRequest("Company limit has been met for this day or there is not enough time at all.");

                // Check if there is enough time in that slot
                if (!await IsEnoughTimeInSlotAsync(dbReservation.StartDate, dbReservation.TimeRequirement))
                    return BadRequest("There is not enough time in that slot.");
            }
            #endregion

            // Check if MPV
            dbReservation.Mpv = dbReservation.Mpv || await IsMpvAsync(dbReservation.VehiclePlateNumber);

            // Update calendar event using Microsoft Graph
            if (dbReservation.UserId == _user.Id && _user.CalendarIntegration)
            {
                dbReservation.User = _user;
                dbReservation.OutlookEventId = await _calendarService.UpdateEventAsync(dbReservation);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Track event with AppInsight
            _telemetryClient.TrackEvent(
                "Reservation was updated.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", dbReservation.Id },
                    { "Reservation user ID", dbReservation.UserId },
                    { "Action user ID", _user.Id },
                });

            return Ok(new ReservationViewModel(dbReservation));
        }

        // POST: api/Reservations
        /// <summary>
        /// Add a new reservation
        /// </summary>
        /// <param name="reservation"><see cref="Reservation"/></param>
        /// <param name="dropoffConfirmed">Indicates key drop-off and location confirmation (default false)</param>
        /// <returns>The newly created <see cref="Reservation"/></returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if no service chosen / StartDate and EndDate isn't on the same day / a Date is in the past / StartDate and EndDate are not valid slot start/end times / user/company limit has been met / there is no more time in that slot.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to reserve for another user.</response>
        [UserAction]
        [HttpPost]
        public async Task<ActionResult<ReservationViewModel>> PostReservation([FromBody] Reservation reservation, [FromQuery] bool dropoffConfirmed = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            #region Defaults
            if (reservation.UserId == null) reservation.UserId = _user.Id;
            reservation.State = State.SubmittedNotActual;
            reservation.Mpv = false;
            reservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty);
            reservation.CarwashComment = null;
            reservation.CreatedById = _user.Id;
            reservation.CreatedOn = DateTime.Now;
            reservation.StartDate = reservation.StartDate.ToLocalTime();

            try
            {
                reservation.EndDate = CalculateEndTime(reservation.StartDate, reservation.EndDate);
            }
            catch (ArgumentOutOfRangeException e)
            {
                _telemetryClient.TrackException(e, new Dictionary<string, string>
                    {
                        { "CreatedById", reservation.CreatedById },
                        { "StartDate", reservation.StartDate.ToString() }
                    });

                return BadRequest("Reservation can be made to slots only.");
            }

            if (dropoffConfirmed)
            {
                if (reservation.Location == null)
                {
                    _telemetryClient.TrackTrace(
                        "BadRequest: Location must be set if drop-off pre-confirmed.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", reservation.UserId },
                            { "Location", reservation.Location }
                        });

                    return BadRequest("Location must be set if drop-off pre-confirmed.");
                }
                reservation.State = State.DropoffAndLocationConfirmed;
            }
            #endregion

            #region Input validation
            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin))
            {
                _telemetryClient.TrackTrace(
                    "Forbid: User cannot reserve in the name of others unless admin.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "IsAdmin", _user.IsAdmin.ToString() },
                        { "IsCarwashAdmin", _user.IsCarwashAdmin.ToString() },
                        { "CreatedById", reservation.CreatedById }
                    });

                return Forbid();
            }

            if (reservation.Services == null || reservation.Services.Count == 0)
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: No service chosen.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Services", reservation.ServicesJson }
                    });

                return BadRequest("No service chosen.");
            }
            #endregion

            // Time requirement calculation
            reservation.TimeRequirement = reservation.Services.Contains(Constants.ServiceType.Carpet) ?
                _configuration.Reservation.CarpetCleaningMultiplier * _configuration.Reservation.TimeUnit :
                _configuration.Reservation.TimeUnit;

            #region Business validation
            // Checks whether start and end times are on the same day
            if (!IsStartAndEndTimeOnSameDay(reservation.StartDate, reservation.EndDate))
                return BadRequest("Reservation time range should be located entirely on the same day.");

            // Checks whether end time is later than start time
            if (!IsEndTimeLaterThanStartTime(reservation.StartDate, reservation.EndDate))
                return BadRequest("Reservation end time should be later than the start time.");

            // Checks whether start or end time is before the earliest allowed time
            if (IsInPast(reservation.StartDate, reservation.EndDate))
                return BadRequest("Cannot reserve in the past.");

            // Checks whether start or end times fit into a slot
            if (!IsInSlot(reservation.StartDate, reservation.EndDate))
                return BadRequest("Reservation can be made to slots only.");

            // Checks whether user has met the active concurrent reservation limit
            if (await IsUserConcurrentReservationLimitMetAsync())
                return BadRequest($"Cannot have more than {_configuration.Reservation.UserConcurrentReservationLimit} concurrent active reservations.");

            // Checks if the date/time is blocked
            if (await IsBlocked(reservation.StartDate, reservation.EndDate))
                return BadRequest("This time is blocked.");

            // Check if there is enough time on that day
            if (!await IsEnoughTimeOnDateAsync(reservation.StartDate, reservation.TimeRequirement))
                return BadRequest("Company limit has been met for this day or there is not enough time at all.");

            // Check if there is enough time in that slot
            if (!await IsEnoughTimeInSlotAsync(reservation.StartDate, reservation.TimeRequirement))
                return BadRequest("There is not enough time in that slot.");
            #endregion

            // Check if MPV
            reservation.Mpv = await IsMpvAsync(reservation.VehiclePlateNumber);

            // Send meeting request
            if (reservation.UserId == _user.Id)
            {
                if (_user.CalendarIntegration)
                {
                    reservation.User = _user;
                    reservation.OutlookEventId = await _calendarService.CreateEventAsync(reservation);
                }
            }
            else
            {
                var user = await _context.Users.FindAsync(reservation.UserId);
                if (user.CalendarIntegration)
                {
                    var timer = DateTime.Now;
                    try
                    {
                        reservation.User = user;
                        reservation.OutlookEventId = await _calendarService.CreateEventAsync(reservation);
                        _telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.Now - timer, success: true);

                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.Now - timer, success: false);
                        _telemetryClient.TrackException(e);
                    }
                }
            }

            _context.Reservation.Add(reservation);
            await _context.SaveChangesAsync();

            // Track event with AppInsight
            _telemetryClient.TrackEvent(
                "New reservation was submitted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", _user.Id },
                });

            return CreatedAtAction("GetReservation", new { id = reservation.Id }, new ReservationViewModel(reservation));
        }

        // DELETE: api/Reservations/{id}
        /// <summary>
        /// Delete an existing reservation
        /// </summary>
        /// <param name="id">reservation id</param>
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

            var reservation = await _context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == id);
            if (reservation == null) return NotFound();

            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();

            _context.Reservation.Remove(reservation);
            await _context.SaveChangesAsync();

            // Delete calendar event using Microsoft Graph
            await _calendarService.DeleteEventAsync(reservation);

            // Track event with AppInsight
            _telemetryClient.TrackEvent(
                "Reservation was deleted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", _user.Id },
                });

            return Ok(new ReservationViewModel(reservation));
        }

        // GET: api/reservations/company
        /// <summary>
        /// Get reservations in my company
        /// </summary>
        /// <returns>List of latest 100 <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [UserAction]
        [HttpGet, Route("company")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetCompanyReservations()
        {
            if (!_user.IsAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.User.Company == _user.Company && r.UserId != _user.Id)
                .OrderByDescending(r => r.StartDate)
                .Take(100)
                .Select(reservation => new AdminReservationViewModel
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    VehiclePlateNumber = reservation.VehiclePlateNumber,
                    Location = reservation.Location,
                    State = reservation.State,
                    Services = reservation.Services,
                    Private = reservation.Private,
                    Mpv = reservation.Mpv,
                    StartDate = reservation.StartDate,
                    EndDate = (DateTime)reservation.EndDate,
                    Comment = reservation.Comment,
                    CarwashComment = reservation.CarwashComment,
                    User = new UserViewModel
                    {
                        Id = reservation.User.Id,
                        FirstName = reservation.User.FirstName,
                        LastName = reservation.User.LastName,
                        Company = reservation.User.Company,
                        IsAdmin = reservation.User.IsAdmin,
                        IsCarwashAdmin = reservation.User.IsCarwashAdmin
                    }
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // GET: api/reservations/backlog
        /// <summary>
        /// Get reservation backlog for carwash admins
        /// </summary>
        /// <returns>List of <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not carwash admin.</response>
        [UserAction]
        [HttpGet, Route("backlog")]
        public async Task<ActionResult<IEnumerable<AdminReservationViewModel>>> GetBacklog()
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.StartDate.Date >= DateTime.Today || r.State != State.Done)
                .OrderBy(r => r.StartDate)
                .Select(reservation => new AdminReservationViewModel
                {
                    Id = reservation.Id,
                    UserId = reservation.UserId,
                    VehiclePlateNumber = reservation.VehiclePlateNumber,
                    Location = reservation.Location,
                    State = reservation.State,
                    Services = reservation.Services,
                    Private = reservation.Private,
                    Mpv = reservation.Mpv,
                    StartDate = reservation.StartDate,
                    EndDate = (DateTime)reservation.EndDate,
                    Comment = reservation.Comment,
                    CarwashComment = reservation.CarwashComment,
                    User = new UserViewModel
                    {
                        Id = reservation.User.Id,
                        FirstName = reservation.User.FirstName,
                        LastName = reservation.User.LastName,
                        Company = reservation.User.Company,
                        IsAdmin = reservation.User.IsAdmin,
                        IsCarwashAdmin = reservation.User.IsCarwashAdmin
                    }
                })
                .ToListAsync();

            return Ok(reservations);
        }

        // POST: api/reservations/{id}/confirmdropoff
        /// <summary>
        /// Confirm car key dropoff and location
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <param name="location">car location</param>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if id or location param is null.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to update another user's reservation.</response>
        [UserAction]
        [HttpPost("{id}/confirmdropoff")]
        public async Task<IActionResult> ConfirmDropoff([FromRoute] string id, [FromBody] string location)
        {
            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (location == null) return BadRequest("Reservation location cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();

            reservation.State = State.DropoffAndLocationConfirmed;
            reservation.Location = location;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Track event with AppInsight
            _telemetryClient.TrackEvent(
                "Key dropoff was confirmed by user.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", _user.Id },
                });

            return NoContent();
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
            if (model?.Email == null) return BadRequest("Email cannot be null.");
            if (model.Location == null) return BadRequest("Reservation location cannot be null.");

            var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);
            if (user == null) return NotFound($"No user found with email address '{model.Email}'.");

            var reservations = await _context.Reservation
                .AsNoTracking()
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
                reservationResolution = "Only one reservation in 'waiting for key' state.";
            }
            // One where we are waiting for the key and is today - eg. there's another one in the past where the key was not dropped off and nobody deleted it
            else if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey && r.StartDate.Date == DateTime.Today) == 1)
            {
                reservation = reservations.Single(r => r.State == State.ReminderSentWaitingForKey && r.StartDate.Date == DateTime.Today);
                reservationResolution = "Only one reservation TODAY in 'waiting for key' state.";
            }
            // Only one active reservation today - eg. user has two reservations, one today, one in the future and on the morning drops off the keys before the reminder
            else if (reservations.Count(r => r.StartDate.Date == DateTime.Today) == 1)
            {
                reservation = reservations.Single(r => r.StartDate.Date == DateTime.Today);
                reservationResolution = "Only one reservation today.";
            }
            else if (model.VehiclePlateNumber == null)
            {
                return Conflict("More than one reservation found where the reservation state is submitted or waiting for key. Please specify vehicle plate number!");
            }
            else return Conflict("More than one reservation found where the reservation state is submitted or waiting for key.");

            reservation.State = State.DropoffAndLocationConfirmed;
            reservation.Location = model.Location;

            try
            {
                _context.Reservation.Update(reservation);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == reservation.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Track event with AppInsight
            _telemetryClient.TrackEvent(
                "Key dropoff was confirmed by 3rd party service.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Reservation resolution method", reservationResolution },
                    { "Number of active reservation of the user", reservations.Count.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "Service dropoff", 1 },
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
        [UserAction]
        [HttpPost("{id}/startwash")]
        public async Task<IActionResult> StartWash([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.State = State.WashInProgress;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Try to send message through bot
            await _botService.SendWashStartedMessageAsync(reservation);

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/completewash")]
        public async Task<IActionResult> CompleteWash([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");

            var reservation = await _context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            reservation.State = reservation.Private ? State.NotYetPaid : State.Done;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            switch (reservation.User.NotificationChannel)
            {
                case NotificationChannel.Disabled:
                    break;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email,
                        Subject = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}",
                    };
                    await _emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}",
                        Tag = NotificationTag.Done
                    };
                    try
                    {
                        await _pushService.Send(reservation.UserId, notification);
                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackException(e);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Try to send message through bot
            await _botService.SendWashCompletedMessageAsync(reservation);

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/confirmpayment")]
        public async Task<IActionResult> ConfirmPayment([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            if (reservation.State != State.NotYetPaid) return BadRequest("Reservation state is not 'Not yet paid'.");

            reservation.State = State.Done;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/state/{state}")]
        public async Task<IActionResult> SetState([FromRoute] string id, [FromRoute] State state)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.State = state;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/carwashcomment")]
        public async Task<IActionResult> AddCarwashComment([FromRoute] string id, [FromBody] string comment)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (comment == null) return BadRequest("Comment cannot be null.");

            var reservation = await _context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound();

            if (reservation.CarwashComment != null) reservation.CarwashComment += "\n";
            reservation.CarwashComment += comment;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            switch (reservation.User.NotificationChannel)
            {
                case NotificationChannel.Disabled:
                    break;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = "CarWash has left a comment on your reservation.",
                        Body = reservation.CarwashComment,
                        Tag = NotificationTag.Comment
                    };
                    try
                    {
                        await _pushService.Send(reservation.UserId, notification);
                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackException(e);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Try to send message through bot
            await _botService.SendCarWashCommentLeftMessageAsync(reservation);

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/mpv")]
        public async Task<IActionResult> SetMpv([FromRoute] string id, [FromBody] bool mpv)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.Mpv = mpv;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/services")]
        public async Task<IActionResult> UpdateServices([FromRoute] string id, [FromBody] List<int> services)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (services == null) return BadRequest("Services param cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.Services = services;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
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
        [UserAction]
        [HttpPost("{id}/location")]
        public async Task<IActionResult> UpdateLocation([FromRoute] string id, [FromBody] string location)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id == null) return BadRequest("Reservation id cannot be null.");
            if (location == null) return BadRequest("New location cannot be null.");

            var reservation = await _context.Reservation.FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.Location = location;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Reservation.Any(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // GET: api/reservations/obfuscated
        /// <summary>
        /// Get all future reservation data for the next <paramref name="daysAhead"/> days
        /// </summary>
        /// <param name="daysAhead">Days ahead to return reservation data</param>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet, Route("obfuscated")]
        [Obsolete("Use GetReservationCapacity instead.")]
        public IEnumerable<ObfuscatedReservationViewModel> GetObfuscatedReservations(int daysAhead = 365)
        {
            return _context.Reservation
                .Where(r => r.EndDate >= DateTime.Now && r.StartDate <= DateTime.Now.AddDays(daysAhead))
                .Include(r => r.User)
                .OrderBy(r => r.StartDate)
                .Select(reservation => new ObfuscatedReservationViewModel
                {
                    Company = reservation.User.Company,
                    Services = reservation.Services,
                    TimeRequirement = reservation.TimeRequirement,
                    StartDate = reservation.StartDate,
                    EndDate = (DateTime)reservation.EndDate,
                });
        }

        // GET: api/reservations/notavailabledates
        /// <summary>
        /// Get the list of future dates that are not available
        /// </summary>
        /// <returns>List of <see cref="DateTime"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("notavailabledates")]
        public async Task<NotAvailableDatesAndTimesViewModel> GetNotAvailableDatesAndTimes(int daysAhead = 365)
        {
            if (_user.IsCarwashAdmin) return new NotAvailableDatesAndTimesViewModel { Dates = [], Times = [] };

            #region Get not available dates
            var notAvailableDates = new List<DateTime>();
            var dailyCapacity = _configuration.Slots.Sum(s => s.Capacity);
            var userCompanyLimit = (await _context.Company.SingleAsync(c => c.Name == _user.Company)).DailyLimit;

            notAvailableDates.AddRange(await _context.Reservation
                .Where(r => r.EndDate >= DateTime.Now && r.StartDate <= DateTime.Now.AddDays(daysAhead))
                .GroupBy(r => r.StartDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .Where(d => d.TimeSum >= dailyCapacity * _configuration.Reservation.TimeUnit)
                .Select(d => d.Date)
                .ToListAsync());

            if (!notAvailableDates.Contains(DateTime.Today))
            {
                var toBeDoneTodayTime = await _context.Reservation
                    .Where(r => r.StartDate >= DateTime.Now && r.StartDate.Date == DateTime.Today)
                    .SumAsync(r => r.TimeRequirement);

                if (toBeDoneTodayTime >= GetRemainingSlotCapacityToday() * _configuration.Reservation.TimeUnit) notAvailableDates.Add(DateTime.Today.Date);
            }

            // If the company has set up limits.
            if (userCompanyLimit > 0)
            {
                notAvailableDates.AddRange(await _context.Reservation
                    .Where(r => r.EndDate >= DateTime.Now && r.StartDate <= DateTime.Now.AddDays(daysAhead))
                    .Where(r => r.User.Company == _user.Company)
                    .GroupBy(r => r.StartDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TimeSum = g.Sum(r => r.TimeRequirement)
                    })
                    .Where(d => d.TimeSum >= userCompanyLimit * _configuration.Reservation.TimeUnit)
                    .Select(d => d.Date)
                    .ToListAsync());
            }
            #endregion

            #region Get not available times
            var slotReservationAggregate = await _context.Reservation
                .Where(r => r.EndDate >= DateTime.Now && r.StartDate <= DateTime.Now.AddDays(daysAhead))
                .GroupBy(r => r.StartDate)
                .Select(g => new
                {
                    DateTime = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .ToListAsync();

            var notAvailableTimes = slotReservationAggregate
                .Where(d => d.TimeSum >= _configuration.Slots.Find(s => s.StartTime == d.DateTime.Hour)?.Capacity * _configuration.Reservation.TimeUnit)
                .Select(d => d.DateTime)
                .ToList();

            // Check if a slot has already started today
            foreach (var slot in _configuration.Slots)
            {
                var slotStartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, slot.StartTime, 0, 0);
                if (!notAvailableTimes.Contains(slotStartTime) && slotStartTime.AddMinutes(_configuration.Reservation.MinutesToAllowReserveInPast) < DateTime.Now)
                {
                    notAvailableTimes.Add(slotStartTime);
                }
            }
            #endregion

            #region Check blockers
            var blockers = await _context.Blocker
                .Where(b => b.EndDate >= DateTime.Now)
                .ToListAsync();

            foreach (var blocker in blockers)
            {
                Debug.Assert(blocker.EndDate != null, "blocker.EndDate != null");
                if (blocker.EndDate == null) continue;

                var dateIterator = blocker.StartDate.Date;
                while (dateIterator <= ((DateTime)blocker.EndDate).Date)
                {
                    // Don't bother with the past part of the blocker
                    if (dateIterator < DateTime.Today)
                    {
                        dateIterator = dateIterator.AddDays(1);
                        continue;
                    }

                    var dateBlocked = true;

                    foreach (var slot in _configuration.Slots)
                    {
                        var slotStart = new DateTime(dateIterator.Year, dateIterator.Month, dateIterator.Day, slot.StartTime, 0, 0);
                        var slotEnd = new DateTime(dateIterator.Year, dateIterator.Month, dateIterator.Day, slot.EndTime, 0, 0);
                        if (slotStart > blocker.StartDate && slotEnd < blocker.EndDate && !notAvailableTimes.Contains(slotStart))
                        {
                            notAvailableTimes.Add(slotStart);
                        }
                        else
                        {
                            dateBlocked = false;
                        }
                    }

                    if (dateBlocked && !notAvailableDates.Contains(dateIterator)) notAvailableDates.Add(dateIterator);

                    dateIterator = dateIterator.AddDays(1);
                }
            }
            #endregion

            return new NotAvailableDatesAndTimesViewModel { Dates = notAvailableDates.Distinct(), Times = notAvailableTimes };
        }

        // GET: api/reservations/lastsettings
        /// <summary>
        /// Get some settings from the last reservation made by the user to be used as defaults for a new reservation
        /// </summary>
        /// <returns>an object containing the plate number and location last used</returns>
        /// <response code="200">OK</response>
        /// <response code="204">NoContent if user has no reservation yet.</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpGet, Route("lastsettings")]
        public async Task<ActionResult<LastSettingsViewModel>> GetLastSettings()
        {
            var lastReservation = await _context.Reservation
                .Where(r => r.UserId == _user.Id)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefaultAsync();

            if (lastReservation == null) return NoContent();

            return Ok(new LastSettingsViewModel
            {
                VehiclePlateNumber = lastReservation.VehiclePlateNumber,
                Location = lastReservation.Location,
                Services = lastReservation.Services
            });
        }

        // GET: api/reservations/reservationpercentage
        /// <summary>
        /// Gets a list of slots and their reservation percentage on a given date
        /// </summary>
        /// <param name="date">the date to filter on</param>
        /// <returns>List of <see cref="ReservationPercentageViewModel"/></returns>
        [Obsolete("Use GetReservationCapacity instead.")]
        [HttpGet, Route("reservationpercentage")]
        public async Task<ActionResult<IEnumerable<ReservationPercentageViewModel>>> GetReservationPercentage(DateTime date)
        {
            var slotReservationAggregate = await _context.Reservation
                .Where(r => r.StartDate.Date == date.Date)
                .GroupBy(r => r.StartDate)
                .Select(g => new
                {
                    DateTime = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .ToListAsync();

            var slotReservationPercentage = new List<ReservationPercentageViewModel>();
            foreach (var a in slotReservationAggregate)
            {
                var slotCapacity = _configuration.Slots.Find(s => s.StartTime == a.DateTime.Hour)?.Capacity;
                if (slotCapacity == null) continue;
                slotReservationPercentage.Add(new ReservationPercentageViewModel
                {
                    StartTime = a.DateTime,
                    Percentage = a.TimeSum == 0 ? 0 : Math.Round(a.TimeSum / (double)(slotCapacity * _configuration.Reservation.TimeUnit), 2)
                });
            }

            return Ok(slotReservationPercentage);
        }

        // GET: api/reservations/reservationcapacity
        /// <summary>
        /// Gets a list of slots and their free reservation capacity on a given date
        /// </summary>
        /// <param name="date">the date to filter on</param>
        /// <returns>List of <see cref="ReservationCapacityViewModel"/></returns>
        [HttpGet, Route("reservationcapacity")]
        public async Task<ActionResult<IEnumerable<ReservationCapacityViewModel>>> GetReservationCapacity(DateTime date)
        {
            var slotReservationAggregate = await _context.Reservation
                .Where(r => r.StartDate.Date == date.Date)
                .GroupBy(r => r.StartDate)
                .Select(g => new
                {
                    DateTime = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .ToListAsync();

            var slotFreeCapacity = new List<ReservationCapacityViewModel>();
            foreach (var a in slotReservationAggregate)
            {
                var slotCapacity = _configuration.Slots.Find(s => s.StartTime == a.DateTime.Hour)?.Capacity;
                if (slotCapacity == null) continue;
                var reservedCapacity = (int)Math.Ceiling((double)a.TimeSum / _configuration.Reservation.TimeUnit);
                slotFreeCapacity.Add(new ReservationCapacityViewModel
                {
                    StartTime = a.DateTime,
                    FreeCapacity = (int)slotCapacity - reservedCapacity
                });
            }

            // Add slots with no reservations yet
            foreach (var slot in _configuration.Slots)
            {
                // ReSharper disable SimplifyLinqExpression
                if (!slotFreeCapacity.Any(s => s.StartTime.Hour == slot.StartTime))
                    slotFreeCapacity.Add(new ReservationCapacityViewModel
                    {
                        StartTime = new DateTime(date.Year, date.Month, date.Day, slot.StartTime, 0, 0),
                        FreeCapacity = slot.Capacity
                    });
                // ReSharper restore SimplifyLinqExpression
            }

            return Ok(slotFreeCapacity.OrderBy(s => s.StartTime));
        }

        // GET: api/reservations/export
        /// <summary>
        /// Export a list of reservations to Excel for a given timespan
        /// </summary>
        /// <param name="startDate">start date (default: a month before today)</param>
        /// <param name="endDate">end date (default: today)</param>
        /// <returns>An Excel file of the list of reservations</returns>
        [ProducesResponseType(typeof(FileStreamResult), 200)]
        [UserAction]
        [HttpGet, Route("export")]
        public async Task<IActionResult> Export(DateTime? startDate = null, DateTime? endDate = null)
        {
            var startDateNonNull = startDate ?? DateTime.Today.AddMonths(-1);
            var endDateNonNull = endDate ?? DateTime.Today;

            List<Reservation> reservations;

            if (_user.IsCarwashAdmin)
                reservations = await _context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.StartDate.Date >= startDateNonNull.Date && r.StartDate.Date <= endDateNonNull.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else if (_user.IsAdmin)
                reservations = await _context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.User.Company == _user.Company && r.StartDate.Date >= startDateNonNull.Date &&
                                r.StartDate.Date <= endDateNonNull.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else return Forbid();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the empty workbook
                var worksheet = package.Workbook.Worksheets.Add($"{startDateNonNull.Year}-{startDateNonNull.Month}");

                // Add the headers
                worksheet.Cells[1, 1].Value = "Date";
                worksheet.Cells[1, 2].Value = "Start time";
                worksheet.Cells[1, 3].Value = "End time";
                worksheet.Cells[1, 4].Value = "Company";
                worksheet.Cells[1, 5].Value = "Name";
                worksheet.Cells[1, 6].Value = "Vehicle plate number";
                worksheet.Cells[1, 7].Value = "MPV";
                worksheet.Cells[1, 8].Value = "Private";
                worksheet.Cells[1, 9].Value = "Services";
                worksheet.Cells[1, 10].Value = "Comment";
                worksheet.Cells[1, 11].Value = "Carwash comment";
                worksheet.Cells[1, 12].Value = "Price (computed)";

                // Add values
                var i = 2;
                foreach (var reservation in reservations)
                {
                    worksheet.Cells[i, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[i, 1].Value = reservation.StartDate.Date;

                    worksheet.Cells[i, 2].Style.Numberformat.Format = "hh:mm";
                    worksheet.Cells[i, 2].Value = reservation.StartDate.TimeOfDay;

                    worksheet.Cells[i, 3].Style.Numberformat.Format = "hh:mm";
                    worksheet.Cells[i, 3].Value = reservation.EndDate?.TimeOfDay;

                    worksheet.Cells[i, 4].Value = reservation.User.Company;

                    worksheet.Cells[i, 5].Value = reservation.User.FullName;

                    worksheet.Cells[i, 6].Value = reservation.VehiclePlateNumber;

                    worksheet.Cells[i, 7].Value = reservation.Mpv;

                    worksheet.Cells[i, 8].Value = reservation.Private;

                    worksheet.Cells[i, 9].Value = string.Join(", ", reservation.Services);

                    worksheet.Cells[i, 10].Value = reservation.Comment;

                    worksheet.Cells[i, 11].Value = reservation.CarwashComment;

                    worksheet.Cells[i, 12].Value = reservation.GetPrice(_configuration);

                    i++;
                }

                // Format as table
                var dataRange = worksheet.Cells[1, 1, i == 2 ? i : i - 1, 12]; //cannot create table with only one row
                var table = worksheet.Tables.Add(dataRange, $"reservations_{startDateNonNull.Year}_{startDateNonNull.Month}");
                table.ShowTotal = false;
                table.ShowHeader = true;
                table.ShowFilter = true;

                // Column auto-width
                worksheet.Column(1).AutoFit();
                worksheet.Column(2).AutoFit();
                worksheet.Column(3).AutoFit();
                worksheet.Column(4).AutoFit();
                worksheet.Column(5).AutoFit();
                worksheet.Column(6).AutoFit();
                worksheet.Column(7).AutoFit();
                worksheet.Column(8).AutoFit();
                worksheet.Column(9).AutoFit(); //don't do it for comment fields
                worksheet.Column(12).AutoFit();

                // Pivot table
                var pivotSheet = package.Workbook.Worksheets.Add($"{startDateNonNull.Year}-{startDateNonNull.Month} pivot");
                var pivot = pivotSheet.PivotTables.Add(pivotSheet.Cells[1, 1], dataRange, "Employee pivot");
                if (_user.IsCarwashAdmin) pivot.RowFields.Add(pivot.Fields["Company"]);
                pivot.RowFields.Add(pivot.Fields["Name"]);
                pivot.DataFields.Add(pivot.Fields["Price (computed)"]);
                pivot.DataOnRows = true;

                // Convert to stream
                var stream = new MemoryStream(package.GetAsByteArray());

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"carwash-export-{startDateNonNull.Year}-{startDateNonNull.Month}-{startDateNonNull.Day}.xlsx");
            }
        }

        /// <summary>
        /// Sums the capacity of all not started slots, what are left from the day
        /// </summary>
        /// <remarks>
        /// eg. It is 9:00 AM.
        /// The slot 8-11 has already started.
        /// The slot 11-14 is not yet started, so add the capacity (eg. 12) to the sum.
        /// The slot 14-17 is not yet started, so add the capacity (eg. 11) to the sum.
        /// Sum will be 23.
        /// </remarks>
        /// <returns>Capacity of slots (not time in minutes!)</returns>
        private int GetRemainingSlotCapacityToday()
        {
            var capacity = 0;

            foreach (var slot in _configuration.Slots)
            {
                if (DateTime.Now.Hour < slot.StartTime) capacity += slot.Capacity;

                if (DateTime.Now.Hour >= slot.StartTime && DateTime.Now.Hour < slot.EndTime)
                {
                    var endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, slot.EndTime, 0, 0);
                    var timeDifference = endTime - DateTime.Now;
                    var slotTimeSpan = (slot.EndTime - slot.StartTime) * 60;
                    var slotCapacity = timeDifference.TotalMinutes / slotTimeSpan * slot.Capacity;
                    capacity += (int)Math.Floor(slotCapacity);
                }
            }

            return capacity;
        }

        private DateTime CalculateEndTime(DateTime startTime, DateTime? endTime)
        {
            if (endTime != null) return ((DateTime)endTime).ToLocalTime();

            var slot = _configuration.Slots.Find(s => s.StartTime == startTime.Hour);
            if (slot == null) throw new ArgumentOutOfRangeException(nameof(startTime), "Start time does not fit into any slot.");

            return new DateTime(
                startTime.Year,
                startTime.Month,
                startTime.Day,
                slot.EndTime,
                0, 0);
        }

        /// <summary>
        /// Checks whether start and end times are on the same day
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if start and end times are on the same day</returns>
        private bool IsStartAndEndTimeOnSameDay(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (startTime.Date == ((DateTime)endTime).Date) return true;

            _telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should be located entirely on the same day.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", _user.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() }
                });

            return false;
        }

        /// <summary>
        /// Checks whether end time is later than start time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if end time is later than start time</returns>
        private bool IsEndTimeLaterThanStartTime(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (startTime < endTime) return true;

            _telemetryClient.TrackTrace(
                "BadRequest: Reservation end time should be later than the start time.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", _user.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() }
                });

            return false;
        }

        /// <summary>
        /// Checks whether start or end times are before the earliest allowed time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if start and end times are both after the earliest allowed time</returns>
        private bool IsInPast(DateTime startTime, DateTime? endTime)
        {
            if (_user.IsCarwashAdmin) return false;

            if (endTime == null) throw new ArgumentNullException(nameof(endTime));
            var earliestTimeAllowed = DateTime.Now.AddMinutes(_configuration.Reservation.MinutesToAllowReserveInPast * -1);

            if (startTime < earliestTimeAllowed || endTime < earliestTimeAllowed)
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: Reservation time range should be located entirely after the earliest allowed time.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", _user.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() },
                        { "EarliestTimeAllowed", earliestTimeAllowed.ToString() }
                    });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether start and end times fit into a slot
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if start and end times fit into a slot</returns>
        private bool IsInSlot(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (_configuration.Slots.Any(s => s.StartTime == startTime.Hour && s.EndTime == ((DateTime)endTime).Hour)) return true;

            _telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should fit into a slot.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", _user.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() }
                });

            return false;
        }

        /// <summary>
        /// Checks whether user has met the active concurrent reservation limit: <see cref="CarWashConfiguration.ReservationSettings.UserConcurrentReservationLimit"/>
        /// </summary>
        /// <returns>true if user has met the limit and is not admin</returns>
        private async Task<bool> IsUserConcurrentReservationLimitMetAsync()
        {
            if (_user.IsAdmin || _user.IsCarwashAdmin) return false;

            var activeReservationCount = await _context.Reservation.Where(r => r.UserId == _user.Id && r.State != State.Done).CountAsync();

            if (activeReservationCount >= _configuration.Reservation.UserConcurrentReservationLimit)
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: User has met the active concurrent reservation limit.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", _user.Id },
                        { "ActiveReservationCount", activeReservationCount.ToString() },
                        { "UserConcurrentReservationLimit", _configuration.Reservation.UserConcurrentReservationLimit.ToString() }
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if there is enough time on that day
        /// </summary>
        /// <param name="date">Date of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private async Task<bool> IsEnoughTimeOnDateAsync(DateTime date, int timeRequirement)
        {
            if (_user.IsCarwashAdmin) return true;

            var userCompanyLimit = (await _context.Company.SingleAsync(c => c.Name == _user.Company)).DailyLimit;

            // Do not validate against company limit after {HoursAfterCompanyLimitIsNotChecked} for today
            // or if company limit is 0 (meaning unlimited)
            if ((date.Date == DateTime.Today && DateTime.Now.Hour >= _configuration.Reservation.HoursAfterCompanyLimitIsNotChecked)
                || userCompanyLimit == 0)
            {
                var allSlotCapacity = _configuration.Slots.Sum(s => s.Capacity);

                var reservedTimeOnDate = await _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date)
                    .SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > allSlotCapacity * _configuration.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
                        "BadRequest: There is not enough time on this day at all.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", _user.Id },
                            { "StartDate", date.ToString() },
                            { "ReservedTimeOnDate", reservedTimeOnDate.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "AllSlotCapacity", allSlotCapacity.ToString() },
                        });

                    return false;
                }
            }
            else
            {
                var reservedTimeOnDate = await _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date && r.User.Company == _user.Company)
                    .SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > userCompanyLimit * _configuration.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
                        "BadRequest: Company limit has been reached.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", _user.Id },
                            { "StartDate", date.ToString() },
                            { "ReservedTimeOnDate", reservedTimeOnDate.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "UserCompanyLimit", userCompanyLimit.ToString() },
                        });

                    return false;
                }
            }

            if (date.Date == DateTime.Today)
            {
                var toBeDoneTodayTime = await _context.Reservation
                    .Where(r => r.StartDate >= DateTime.Now && r.StartDate.Date == DateTime.Today)
                    .SumAsync(r => r.TimeRequirement);

                var remainingSlotCapacityToday = GetRemainingSlotCapacityToday();
                if (toBeDoneTodayTime + timeRequirement > remainingSlotCapacityToday * _configuration.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
                        "BadRequest: Company limit has been reached.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", _user.Id },
                            { "StartDate", date.ToString() },
                            { "ToBeDoneTodayTime", toBeDoneTodayTime.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "RemainingSlotCapacityToday", remainingSlotCapacityToday.ToString() },
                        });

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Check if there is enough time in that slot
        /// </summary>
        /// <param name="dateTime">Date and time of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private async Task<bool> IsEnoughTimeInSlotAsync(DateTime dateTime, int timeRequirement)
        {
            if (_user.IsCarwashAdmin) return true;

            var reservedTimeInSlot = await _context.Reservation
                .Where(r => r.StartDate == dateTime)
                .SumAsync(r => r.TimeRequirement);

            var slotCapacity = _configuration.Slots.Find(s => s.StartTime == dateTime.Hour)?.Capacity;
            if (reservedTimeInSlot + timeRequirement <= slotCapacity * _configuration.Reservation.TimeUnit) return true;

            _telemetryClient.TrackTrace(
                "BadRequest: There is not enough time in that slot.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", _user.Id },
                    { "StartDate", dateTime.ToString() },
                    { "reservedTimeInSlot", reservedTimeInSlot.ToString() },
                    { "TimeRequirement", timeRequirement.ToString() },
                    { "slotCapacity", slotCapacity.ToString() },
                });

            return false;
        }

        /// <summary>
        /// Checks if the date/time is blocked
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if date/time is blocked and user is not carwash admin</returns>
        private async Task<bool> IsBlocked(DateTime startTime, DateTime? endTime)
        {
            if (_user.IsCarwashAdmin) return false;

            if (await _context.Blocker.AnyAsync(b => b.StartDate < startTime && b.EndDate > endTime))
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: Cannot reserve for blocked slots.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", _user.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() }
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if car is an MPV
        /// </summary>
        /// <param name="vehiclePlateNumber">vehicle plate number</param>
        /// <returns>true if the given vehicle plate number was categorized as an MPV at least once</returns>
        private async Task<bool> IsMpvAsync(string vehiclePlateNumber)
        {
            return await _context.Reservation
                .OrderByDescending(r => r.StartDate)
                .Where(r => r.VehiclePlateNumber == vehiclePlateNumber)
                .Select(r => r.Mpv)
                .FirstOrDefaultAsync();
        }
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class ReservationViewModel
    {
        public ReservationViewModel() { }

        public ReservationViewModel(Reservation reservation)
        {
            Id = reservation.Id;
            UserId = reservation.UserId;
            VehiclePlateNumber = reservation.VehiclePlateNumber;
            Location = reservation.Location;
            State = reservation.State;
            Services = reservation.Services;
            Private = reservation.Private;
            Mpv = reservation.Mpv;
            StartDate = reservation.StartDate;
            if (reservation.EndDate != null) EndDate = (DateTime)reservation.EndDate;
            Comment = reservation.Comment;
            CarwashComment = reservation.CarwashComment;
        }

        public string Id { get; set; }
        public string UserId { get; set; }
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
        public State State { get; set; }
        public List<int> Services { get; set; }
        public bool Private { get; set; }
        public bool Mpv { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; }
        public string CarwashComment { get; set; }
    }

    public class AdminReservationViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public UserViewModel User { get; set; }
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
        public State State { get; set; }
        public List<int> Services { get; set; }
        public bool? Private { get; set; }
        public bool? Mpv { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; }
        public string CarwashComment { get; set; }
    }

    public class ObfuscatedReservationViewModel
    {
        public string Company { get; set; }
        public List<int> Services { get; set; }
        public int? TimeRequirement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class NotAvailableDatesAndTimesViewModel
    {
        public IEnumerable<DateTime> Dates { get; set; }
        public IEnumerable<DateTime> Times { get; set; }
    }

    public class LastSettingsViewModel
    {
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
        public List<int> Services { get; set; }
    }

    public class ReservationPercentageViewModel
    {
        public DateTime StartTime { get; set; }
        public double Percentage { get; set; }
    }

    public class ReservationCapacityViewModel
    {
        public DateTime StartTime { get; set; }
        public int FreeCapacity { get; set; }
    }

    public class ConfirmDropoffByEmailViewModel
    {
        public string Email { get; set; }
        public string Location { get; set; }
        public string VehiclePlateNumber { get; set; }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}