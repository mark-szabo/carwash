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
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;

        public ReservationsController(ApplicationDbContext context, UsersController usersController)
        {
            _context = context;
            _user = usersController.GetCurrentUser();
        }

        // GET: api/reservations
        [HttpGet]
        public IEnumerable<object> GetReservation()
        {
            var query = _user.IsAdmin || _user.IsCarwashAdmin ?
                _context.Reservation.Where(r => r.UserId == _user.Id || r.CreatedById == _user.Id) :
                _context.Reservation.Where(r => r.UserId == _user.Id);

            return query.Select(reservation => new ReservationViewModel(reservation));
        }

        // GET: api/reservations/{id}
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
            reservation.State = State.NotStarted;
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

            // TODO: Calculate time requirement

            _context.Reservation.Add(reservation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReservation", new { id = reservation.Id }, new ReservationViewModel(reservation));
        }

        // DELETE: api/Reservations/5
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
    }

    internal class ReservationViewModel
    {
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
}