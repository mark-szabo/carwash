using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using MSHU.CarWash.ClassLibrary.Services;
using MSHU.CarWash.PWA.Extensions;
using MSHU.CarWash.PWA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MSHU.CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing reservations
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly ICalendarService _calendarService;
        private readonly IPushService _pushService;

        /// <summary>
        /// Wash time unit in minutes
        /// </summary>
        private const int TimeUnit = 12;

        /// <summary>
        /// Number of concurrent active reservations permitted
        /// </summary>
        private const int UserConcurrentReservationLimit = 2;

        /// <summary>
        /// Number of minutes to allow reserving in past or in a slot after that slot has already started
        /// </summary>
        private const int MinutesToAllowReserveInPast = 120;

        /// <summary>
        /// Time of day in hours after reservations for the same day must not be validated against company limit
        /// </summary>
        private const int HoursAfterCompanyLimitIsNotChecked = 11;

        /// <summary>
        /// Daily limits per company
        /// </summary>
        private static readonly List<Company> CompanyLimit = new List<Company>
        {
            new Company(Company.Carwash, 0),
            new Company(Company.Microsoft, 14),
            new Company(Company.Sap, 16),
            new Company(Company.Graphisoft, 5)
        };

        /// <summary>
        /// Bookable slots and their capacity (in washes and not in minutes!)
        /// </summary>
        private static readonly List<Slot> Slots = new List<Slot>
        {
            new Slot {StartTime = 8, EndTime = 11, Capacity = 12},
            new Slot {StartTime = 11, EndTime = 14, Capacity = 12},
            new Slot {StartTime = 14, EndTime = 17, Capacity = 11}
        };

        /// <inheritdoc />
        public ReservationsController(ApplicationDbContext context, UsersController usersController, ICalendarService calendarService, IPushService pushService)
        {
            _context = context;
            _user = usersController.GetCurrentUser();
            _calendarService = calendarService;
            _pushService = pushService;
        }

        // GET: api/reservations
        /// <summary>
        /// Get my reservations
        /// </summary>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(IEnumerable<ReservationViewModel>), 200)]
        [HttpGet]
        public IEnumerable<object> GetReservation()
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
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to get another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [ProducesResponseType(typeof(ReservationViewModel), 200)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReservation([FromRoute] string id)
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
        [ProducesResponseType(typeof(ReservationViewModel), 200)]
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation([FromRoute] string id, [FromBody] Reservation reservation, [FromQuery] bool dropoffConfirmed = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id != reservation.Id) return BadRequest();

            var dbReservation = await _context.Reservation.FindAsync(id);

            if (dbReservation == null) return NotFound();

            dbReservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty);
            dbReservation.Location = reservation.Location;
            dbReservation.Services = reservation.Services;
            dbReservation.Private = reservation.Private;
            dbReservation.StartDate = reservation.StartDate.ToLocalTime();
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
            if (dbReservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();
            if ((reservation.UserId != _user.Id || reservation.UserId != null) &&
                !(_user.IsAdmin || _user.IsCarwashAdmin))
                return BadRequest("Cannot modify user of registration. Yo need to re-create it.");
            if (reservation.Services == null) return BadRequest("No service chosen.");
            #endregion

            // Time requirement calculation
            dbReservation.TimeRequirement = dbReservation.Services.Contains(ServiceType.Carpet) ? 2 * TimeUnit : TimeUnit;

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

            // Check if there is enough time on that day
            if (!IsEnoughTimeOnDate(dbReservation.StartDate, dbReservation.TimeRequirement))
                return BadRequest("Company limit has been met for this day or there is not enough time at all.");

            // Check if there is enough time in that slot
            if (!IsEnoughTimeInSlot(dbReservation.StartDate, dbReservation.TimeRequirement))
                return BadRequest("There is not enough time in that slot.");
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
        [ProducesResponseType(typeof(ReservationViewModel), 201)]
        [HttpPost]
        public async Task<IActionResult> PostReservation([FromBody] Reservation reservation, [FromQuery] bool dropoffConfirmed = false)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            #region Defaults
            if (reservation.UserId == null) reservation.UserId = _user.Id;
            reservation.State = State.SubmittedNotActual;
            reservation.Mpv = false;
            reservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty);
            reservation.CarwashComment = null;
            reservation.CreatedById = _user.Id;
            reservation.CreatedOn = DateTime.Now;
            reservation.StartDate = reservation.StartDate.ToLocalTime();

            try
            {
                reservation.EndDate = CalculateEndTime(reservation.StartDate, reservation.EndDate);
            }
            catch (ArgumentOutOfRangeException)
            {
                return BadRequest("Reservation can be made to slots only.");
            }

            if (dropoffConfirmed)
            {
                if (reservation.Location == null) return BadRequest("Location must be set if drop-off pre-confirmed.");
                reservation.State = State.DropoffAndLocationConfirmed;
            }
            #endregion

            #region Input validation
            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();
            if (reservation.Services == null) return BadRequest("No service chosen.");
            #endregion

            // Time requirement calculation
            reservation.TimeRequirement = reservation.Services.Contains(ServiceType.Carpet) ? 2 * TimeUnit : TimeUnit;

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
                return BadRequest($"Cannot have more than {UserConcurrentReservationLimit} concurrent active reservations.");

            // Check if there is enough time on that day
            if (!IsEnoughTimeOnDate(reservation.StartDate, reservation.TimeRequirement))
                return BadRequest("Company limit has been met for this day or there is not enough time at all.");

            // Check if there is enough time in that slot
            if (!IsEnoughTimeInSlot(reservation.StartDate, reservation.TimeRequirement))
                return BadRequest("There is not enough time in that slot.");
            #endregion

            // Check if MPV
            reservation.Mpv = await IsMpvAsync(reservation.VehiclePlateNumber);

            // Add calendar event using Microsoft Graph
            if (reservation.UserId == _user.Id && _user.CalendarIntegration)
            {
                reservation.User = _user;
                reservation.OutlookEventId = await _calendarService.CreateEventAsync(reservation);
            }

            _context.Reservation.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReservation", new { id = reservation.Id }, new ReservationViewModel(reservation));
        }

        // DELETE: api/Reservations/{id}
        /// <summary>
        /// Delete an existing reservation
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns>The deleted <see cref="Reservation"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to delete another user's reservation.</response>
        /// <response code="404">NotFound if reservation not found.</response>
        [ProducesResponseType(typeof(ReservationViewModel), 200)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReservation([FromRoute] string id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var reservation = await _context.Reservation.FindAsync(id);
            if (reservation == null) return NotFound();

            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();

            _context.Reservation.Remove(reservation);
            await _context.SaveChangesAsync();

            // Delete calendar event using Microsoft Graph
            await _calendarService.DeleteEventAsync(reservation.OutlookEventId);

            return Ok(new ReservationViewModel(reservation));
        }

        // GET: api/reservations/company
        /// <summary>
        /// Get reservations in my company
        /// </summary>
        /// <returns>List of <see cref="AdminReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin.</response>
        [ProducesResponseType(typeof(IEnumerable<AdminReservationViewModel>), 200)]
        [HttpGet, Route("company")]
        public async Task<IActionResult> GetCompanyReservations()
        {
            if (!_user.IsAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.User.Company == _user.Company && r.UserId != _user.Id)
                .OrderByDescending(r => r.StartDate)
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
        [ProducesResponseType(typeof(IEnumerable<AdminReservationViewModel>), 200)]
        [HttpGet, Route("backlog")]
        public async Task<IActionResult> GetBacklog()
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.StartDate.Date >= DateTime.Today || r.State != State.Done)
                .OrderByDescending(r => r.StartDate)
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
                    await email.Send();
                    break;
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}",
                        Tag = NotificationTag.Done
                    };
                    await _pushService.Send(reservation.UserId, notification);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    break;
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = "CarWash has left a comment on your reservation.",
                        Body = reservation.CarwashComment,
                        Tag = NotificationTag.Comment
                    };
                    await _pushService.Send(reservation.UserId, notification);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
        [HttpPost("{id}/services")]
        public async Task<IActionResult> UpdateServices([FromRoute] string id, [FromBody] List<ServiceType> services)
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
        [ProducesResponseType(typeof(NoContentResult), 204)]
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
        [ProducesResponseType(typeof(IEnumerable<ObfuscatedReservationViewModel>), 200)]
        [HttpGet, Route("obfuscated")]
        public IEnumerable<object> GetObfuscatedReservations(int daysAhead = 365)
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
        [ProducesResponseType(typeof(NotAvailableDatesAndTimesViewModel), 200)]
        [HttpGet, Route("notavailabledates")]
        public async Task<object> GetNotAvailableDatesAndTimes(int daysAhead = 365)
        {
            if (_user.IsCarwashAdmin) return new NotAvailableDatesAndTimesViewModel { Dates = new List<DateTime>(), Times = new List<DateTime>() };

            #region Get not available dates
            var userCompanyLimit = CompanyLimit.Find(c => c.Name == _user.Company).DailyLimit;

            // Must be separated to force client evaluation because of this EF issue:
            // https://github.com/aspnet/EntityFrameworkCore/issues/11453
            // Current milestone to be fixed is EF 3.0.0
            var queryResult = await _context.Reservation
                .Where(r => r.EndDate >= DateTime.Now && r.StartDate <= DateTime.Now.AddDays(daysAhead))
                .Include(r => r.User)
                .Where(r => r.User.Company == _user.Company)
                .GroupBy(r => r.StartDate.Date)
                .Select(g => new
                {
                    g.Key.Date,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .Where(d => d.TimeSum >= userCompanyLimit * TimeUnit)
                .ToListAsync();

            var notAvailableDates = queryResult.Select(d => d.Date).ToList();

            if (!notAvailableDates.Contains(DateTime.Today))
            {
                // Cannot use SumAsync because of this EF issue:
                // https://github.com/aspnet/EntityFrameworkCore/issues/12314
                // Current milestone to be fixed is EF 2.1.3
                var toBeDoneTodayTime = _context.Reservation
                    .Where(r => r.StartDate >= DateTime.Now && r.StartDate.Date == DateTime.Today)
                    .Sum(r => r.TimeRequirement);
                if (toBeDoneTodayTime >= GetRemainingSlotCapacityToday() * TimeUnit) notAvailableDates.Add(DateTime.Today);
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
                .Where(d => d.TimeSum >= Slots.Find(s => s.StartTime == d.DateTime.Hour)?.Capacity * TimeUnit)
                .Select(d => d.DateTime)
                .ToList();

            // Check if a slot has already started today
            foreach (var slot in Slots)
            {
                var slotStartTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, slot.StartTime, 0, 0);
                if (!notAvailableTimes.Contains(slotStartTime) && slotStartTime.AddMinutes(MinutesToAllowReserveInPast) < DateTime.Now)
                {
                    notAvailableTimes.Add(slotStartTime);
                }
            }
            #endregion

            return new NotAvailableDatesAndTimesViewModel { Dates = notAvailableDates, Times = notAvailableTimes };
        }

        // GET: api/reservations/lastsettings
        /// <summary>
        /// Get some settings from the last reservation made by the user to be used as defaults for a new reservation
        /// </summary>
        /// <returns>an object containing the plate number and location last used</returns>
        /// <response code="200">OK</response>
        /// <response code="204">NoContent if user has no reservation yet.</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(LastSettingsViewModel), 200)]
        [HttpGet, Route("lastsettings")]
        public async Task<IActionResult> GetLastSettings()
        {
            var lastReservation = await _context.Reservation
                .Where(r => r.UserId == _user.Id)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefaultAsync();

            if (lastReservation == null) return NoContent();

            return Ok(new LastSettingsViewModel
            {
                VehiclePlateNumber = lastReservation.VehiclePlateNumber,
                Location = lastReservation.Location
            });
        }

        // GET: api/reservations/reservationpercentage
        /// <summary>
        /// Gets a list of slots and their reservation percentage on a given date
        /// </summary>
        /// <param name="date">the date to filter on</param>
        /// <returns>List of <see cref="ReservationPercentageViewModel"/></returns>
        [ProducesResponseType(typeof(List<ReservationPercentageViewModel>), 200)]
        [HttpGet, Route("reservationpercentage")]
        public async Task<IActionResult> GetReservationPercentage(DateTime date)
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
                var slotCapacity = Slots.Find(s => s.StartTime == a.DateTime.Hour)?.Capacity;
                if (slotCapacity == null) continue;
                slotReservationPercentage.Add(new ReservationPercentageViewModel
                {
                    StartTime = a.DateTime,
                    Percentage = a.TimeSum == 0 ? 0 : Math.Round(a.TimeSum / (double)(slotCapacity * TimeUnit), 2)
                });
            }

            return Ok(slotReservationPercentage);
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
        private static int GetRemainingSlotCapacityToday()
        {
            var capacity = 0;

            foreach (var slot in Slots)
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

            var slot = Slots.Find(s => s.StartTime == startTime.Hour);
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
        private static bool IsStartAndEndTimeOnSameDay(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            return startTime.Date == ((DateTime)endTime).Date;
        }

        /// <summary>
        /// Checks whether end time is later than start time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if end time is later than start time</returns>
        private static bool IsEndTimeLaterThanStartTime(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            return startTime < endTime;
        }

        /// <summary>
        /// Checks whether start or end times are before the earliest allowed time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <returns>true if start and end times are both after the earliest allowed time</returns>
        private bool IsInPast(DateTime startTime, DateTime? endTime)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));
            var earliestTimeAllowed = DateTime.Now.AddMinutes(MinutesToAllowReserveInPast * -1);

            return startTime < earliestTimeAllowed || endTime < earliestTimeAllowed;
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

            return Slots.Any(s => s.StartTime == startTime.Hour && s.EndTime == ((DateTime)endTime).Hour);
        }

        /// <summary>
        /// Checks whether user has met the active concurrent reservation limit: <see cref="UserConcurrentReservationLimit"/>
        /// </summary>
        /// <returns>true if user has met the limit and is not admin</returns>
        private async Task<bool> IsUserConcurrentReservationLimitMetAsync()
        {
            if (_user.IsAdmin || _user.IsCarwashAdmin) return false;

            var activeReservationCount = await _context.Reservation.Where(r => r.UserId == _user.Id && r.State != State.Done).CountAsync();

            return activeReservationCount >= UserConcurrentReservationLimit;
        }

        /// <summary>
        /// Checks if there is enough time on that day
        /// </summary>
        /// <param name="date">Date of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private bool IsEnoughTimeOnDate(DateTime date, int timeRequirement)
        {
            if (_user.IsCarwashAdmin) return true;

            // Do not validate against company limit after {HoursAfterCompanyLimitIsNotChecked} for today
            if (date.Date == DateTime.Today && DateTime.Now.Hour >= HoursAfterCompanyLimitIsNotChecked)
            {
                var allCompanyLimit = CompanyLimit.Sum(c => c.DailyLimit);

                // Cannot use SumAsync because of this EF issue:
                // https://github.com/aspnet/EntityFrameworkCore/issues/12314
                // Current milestone to be fixed is EF 2.1.3
                var reservedTimeOnDate = _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date)
                    .Sum(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > allCompanyLimit * TimeUnit) return false;
            }
            else
            {
                var userCompanyLimit = CompanyLimit.Find(c => c.Name == _user.Company).DailyLimit;

                // Cannot use SumAsync because of this EF issue:
                // https://github.com/aspnet/EntityFrameworkCore/issues/12314
                // Current milestone to be fixed is EF 2.1.3
                var reservedTimeOnDate = _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date && r.User.Company == _user.Company)
                    .Sum(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > userCompanyLimit * TimeUnit) return false;
            }

            if (date.Date == DateTime.Today)
            {
                // Cannot use SumAsync because of this EF issue:
                // https://github.com/aspnet/EntityFrameworkCore/issues/12314
                // Current milestone to be fixed is EF 2.1.3
                var toBeDoneTodayTime = _context.Reservation
                    .Where(r => r.StartDate >= DateTime.Now && r.StartDate.Date == DateTime.Today)
                    .Sum(r => r.TimeRequirement);
                if (toBeDoneTodayTime + timeRequirement > GetRemainingSlotCapacityToday() * TimeUnit) return false;
            }

            return true;
        }

        /// <summary>
        /// Check if there is enough time in that slot
        /// </summary>
        /// <param name="dateTime">Date and time of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private bool IsEnoughTimeInSlot(DateTime dateTime, int timeRequirement)
        {
            if (_user.IsCarwashAdmin) return true;

            // Cannot use SumAsync because of this EF issue:
            // https://github.com/aspnet/EntityFrameworkCore/issues/12314
            // Current milestone to be fixed is EF 2.1.3
            var reservedTimeInSlot = _context.Reservation
                .Where(r => r.StartDate == dateTime)
                .Sum(r => r.TimeRequirement);

            return reservedTimeInSlot + timeRequirement <=
                   Slots.Find(s => s.StartTime == dateTime.Hour)?.Capacity * TimeUnit;
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

    internal class ReservationViewModel
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
        public List<ServiceType> Services { get; set; }
        public bool Private { get; set; }
        public bool Mpv { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; }
        public string CarwashComment { get; set; }
    }

    internal class AdminReservationViewModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public UserViewModel User { get; set; }
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
        public State State { get; set; }
        public List<ServiceType> Services { get; set; }
        public bool? Private { get; set; }
        public bool? Mpv { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Comment { get; set; }
        public string CarwashComment { get; set; }
    }

    internal class ObfuscatedReservationViewModel
    {
        public string Company { get; set; }
        public List<ServiceType> Services { get; set; }
        public int? TimeRequirement { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    internal class NotAvailableDatesAndTimesViewModel
    {
        public IEnumerable<DateTime> Dates { get; set; }
        public IEnumerable<DateTime> Times { get; set; }
    }

    internal class LastSettingsViewModel
    {
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
    }

    internal class ReservationPercentageViewModel
    {
        public DateTime StartTime { get; set; }
        public double Percentage { get; set; }
    }

    internal class Slot
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int Capacity { get; set; }
    }
}