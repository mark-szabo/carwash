using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSHU.CarWash.ClassLibrary;

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

        /// <summary>
        /// Wash time unit in minutes
        /// </summary>
        private const int TimeUnit = 12;
        
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
            new Slot {StartTime = 8, Capacity = 12},
            new Slot {StartTime = 11, Capacity = 12},
            new Slot {StartTime = 14, Capacity = 11}
        };

        /// <inheritdoc />
        public ReservationsController(ApplicationDbContext context, UsersController usersController)
        {
            _context = context;
            _user = usersController.GetCurrentUser();
        }

        // GET: api/reservations
        /// <summary>
        /// Get my reservations
        /// </summary>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unathorized</response>
        [ProducesResponseType(typeof(IEnumerable<ReservationViewModel>), 200)]
        [HttpGet]
        public IEnumerable<object> GetReservation()
        {
            var query = _user.IsAdmin || _user.IsCarwashAdmin ?
                _context.Reservation.Where(r => r.UserId == _user.Id || r.CreatedById == _user.Id) :
                _context.Reservation.Where(r => r.UserId == _user.Id);

            return query.OrderByDescending(r => r.DateFrom).Select(reservation => new ReservationViewModel(reservation));
        }

        // GET: api/reservations/{id}
        /// <summary>
        /// Get a specific reservation by id
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns><see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unathorized</response>
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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReservation([FromRoute] string id, [FromBody] Reservation reservation)
        {
            throw new NotImplementedException();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (id != reservation.Id) return BadRequest();

            _context.Entry(reservation).State = EntityState.Modified;

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

        // POST: api/Reservations
        /// <summary>
        /// Add a new reservation
        /// </summary>
        /// <param name="reservation"><see cref="Reservation"/></param>
        /// <returns>The newly created <see cref="Reservation"/></returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if no service choosen / DateFrom and DateTo isn't on the same day / a Date is in the past.</response>
        /// <response code="401">Unathorized</response>
        /// <response code="403">Forbidden if user is not admin but tries to reserve for another user.</response>
        [ProducesResponseType(typeof(ReservationViewModel), 200)]
        [HttpPost]
        public async Task<IActionResult> PostReservation([FromBody] Reservation reservation)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Defaults
            if (reservation.UserId == null) reservation.UserId = _user.Id;
            if (reservation.Private == null) reservation.Private = false;
            reservation.State = State.SubmittedNotActual;
            reservation.Mpv = false;
            reservation.CarwashComment = null;
            reservation.CreatedById = _user.Id;
            reservation.CreatedOn = DateTime.Now;

            // Validation
            if (reservation.UserId != _user.Id && !(_user.IsAdmin || _user.IsCarwashAdmin)) return Forbid();
            if (reservation.Services == null) return BadRequest("No service choosen.");
            if (reservation.DateFrom.Date != reservation.DateTo.Date)
                return BadRequest("Reservation data range should be located entirely on the same day.");
            if (reservation.DateFrom < DateTime.Now || reservation.DateTo < DateTime.Now)
                return BadRequest("Cannot reserve in the past.");

            // Time requirement calculation
            reservation.TimeRequirement = reservation.Services.Contains(ServiceType.Carpet) ? 2 * TimeUnit : TimeUnit;

            // TODO Check if there is enough time on that day

            // TODO Check if there is enough time in that slot

            _context.Reservation.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReservation", new { id = reservation.Id }, new ReservationViewModel(reservation));
        }

        // DELETE: api/Reservations/5
        /// <summary>
        /// Delete an existing reservation
        /// </summary>
        /// <param name="id">reservation id</param>
        /// <returns>The deleted <see cref="Reservation"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formated.</response>
        /// <response code="401">Unathorized</response>
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

            return Ok(new ReservationViewModel(reservation));
        }

        // GET: api/reservations/obfuscated
        /// <summary>
        /// Get all future reservation data for the next <paramref name="daysAhead"/> days
        /// </summary>
        /// <param name="daysAhead">Days ahead to return reservation data</param>
        /// <returns>List of <see cref="ReservationViewModel"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unathorized</response>
        [ProducesResponseType(typeof(IEnumerable<ObfuscatedReservationViewModel>), 200)]
        [HttpGet, Route("obfuscated")]
        public IEnumerable<object> GetObfuscatedReservations(int daysAhead = 365)
        {
            return _context.Reservation
                .Where(r => r.DateTo >= DateTime.Now && r.DateFrom <= DateTime.Now.AddDays(daysAhead))
                .Include(r => r.User)
                .OrderBy(r => r.DateFrom)
                .Select(reservation => new ObfuscatedReservationViewModel
                {
                    Company = reservation.User.Company,
                    Services = reservation.Services,
                    TimeRequirement = reservation.TimeRequirement,
                    DateFrom = reservation.DateFrom,
                    DateTo = reservation.DateTo,
                });
        }

        // GET: api/reservations/notavailabledates
        /// <summary>
        /// Get the list of future dates that are not available
        /// </summary>
        /// <returns>List of <see cref="DateTime"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unathorized</response>
        [ProducesResponseType(typeof(IEnumerable<DateTime>), 200)]
        [HttpGet, Route("notavailabledates")]
        public async Task<IEnumerable<DateTime>> GetNotAvailableDates(int daysAhead = 365)
        {
            var userCompanyLimit = CompanyLimit.Find(c => c.Name == _user.Company).DailyLimit;

            // Must be separated to force client evaluation because of this EF issue:
            // https://github.com/aspnet/EntityFrameworkCore/issues/11453
            // Current milestone to be fixed is EF 3.0.0
            var queryResult = await _context.Reservation
                .Where(r => r.DateTo >= DateTime.Now && r.DateFrom <= DateTime.Now.AddDays(daysAhead))
                .Include(r => r.User)
                .Where(r => r.User.Company == _user.Company)
                .GroupBy(r => r.DateFrom.Date)
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
                    .Where(r => r.DateFrom >= DateTime.Now && r.DateFrom.Date == DateTime.Today)
                    .Sum(r => r.TimeRequirement);
                if (toBeDoneTodayTime >= GetRemainingSlotCapacityToday() * TimeUnit) notAvailableDates.Add(DateTime.Today);
            }

            return notAvailableDates;
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
            }

            return capacity;
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
            DateFrom = reservation.DateFrom;
            DateTo = reservation.DateTo;
            Comment = reservation.Comment;
            CarwashComment = reservation.CarwashComment;
        }

        public string Id { get; set; }
        public string UserId { get; set; }
        public string VehiclePlateNumber { get; set; }
        public string Location { get; set; }
        public State State { get; set; }
        public List<ServiceType> Services { get; set; }
        public bool? Private { get; set; }
        public bool? Mpv { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string Comment { get; set; }
        public string CarwashComment { get; set; }
    }

    internal class ObfuscatedReservationViewModel
    {
        public ObfuscatedReservationViewModel() { }

        public ObfuscatedReservationViewModel(Reservation reservation)
        {
            Company = reservation.User.Company;
            Services = reservation.Services;
            TimeRequirement = reservation.TimeRequirement;
            DateFrom = reservation.DateFrom;
            DateTo = reservation.DateTo;
        }

        public string Company { get; set; }
        public List<ServiceType> Services { get; set; }
        public int? TimeRequirement { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
    }

    internal class Slot
    {
        public int StartTime { get; set; }
        public int Capacity { get; set; }
    }
}