using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CarWash.ClassLibrary.Models;
using CarWash.PWA.Extensions;
using CarWash.PWA.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarWash.PWA.Attributes;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing calendar blockers
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [UserAction]
    [Route("api/blockers")]
    [ApiController]
    public class BlockersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly ICalendarService _calendarService;
        private readonly TelemetryClient _telemetryClient;

        /// <inheritdoc />
        public BlockersController(ApplicationDbContext context, IUsersController usersController, ICalendarService calendarService)
        {
            _context = context;
            _user = usersController.GetCurrentUser();
            _calendarService = calendarService;
            _telemetryClient = new TelemetryClient();
        }

        // GET: api/blockers
        /// <summary>
        /// Get blockers
        /// </summary>
        /// <returns>List of <see cref="Blocker"/></returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin or carwash admin.</response>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Blocker>>> GetBlockers()
        {
            if (!_user.IsAdmin && !_user.IsCarwashAdmin) return Forbid();

            return await _context.Blocker.OrderByDescending(b => b.StartDate).ToListAsync();
        }

        // GET: api/blockers/{id}
        /// <summary>
        /// Get a specific blocker by id
        /// </summary>
        /// <param name="id">blocker id</param>
        /// <returns><see cref="Blocker"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formatted.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not admin or carwash admin.</response>
        /// <response code="404">NotFound if blocker not found.</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Blocker>> GetBlocker([FromRoute] string id)
        {
            if (!_user.IsAdmin && !_user.IsCarwashAdmin) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var blocker = await _context.Blocker.FindAsync(id);

            if (blocker == null) return NotFound();

            return Ok(blocker);
        }

        // POST: api/blockers
        /// <summary>
        /// Add a new blocker
        /// </summary>
        /// <param name="blocker"><see cref="Blocker"/></param>
        /// <returns>The newly created <see cref="Blocker"/></returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if model is not valid / EndDate is after StartDate / this blocker is overlapping with any other blocker.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is not a carwash admin.</response>
        [HttpPost]
        public async Task<ActionResult<Blocker>> PostBlocker([FromBody] Blocker blocker)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            blocker.CreatedById = _user.Id;
            blocker.CreatedOn = DateTime.Now;
            if (blocker.EndDate == null) blocker.EndDate = new DateTime(blocker.StartDate.Year, blocker.StartDate.Month, blocker.StartDate.Day, 23, 59, 59);

            if (blocker.EndDate.Value.Subtract(blocker.StartDate).TotalDays > 31) return BadRequest("Blocker cannot be longer than one month.");
            if (blocker.EndDate <= blocker.StartDate) return BadRequest("Blocker end time should be after the start time.");

            // Check for overlapping blocker
            var overLappingBlockerCount = await _context.Blocker
                .Where(b =>
                    b.StartDate < blocker.StartDate && b.EndDate > blocker.StartDate || //an existing blocker is overlapping with the beginning of the new blocker
                    b.StartDate < blocker.EndDate && b.EndDate > blocker.EndDate || //an existing blocker is overlapping with the end of the new blocker
                    b.StartDate > blocker.StartDate && b.EndDate < blocker.EndDate || //an existing blocker is a subset of the new blocker
                    b.StartDate < blocker.StartDate && b.EndDate > blocker.EndDate || //an existing blocker is a superset of the new blocker
                    b.StartDate == blocker.StartDate && b.EndDate == blocker.EndDate //an existing blocker is the same as the new
                )
                .CountAsync();

            if (overLappingBlockerCount > 0) return BadRequest("Two blocker cannot overlap each other.");

            // Delete existing reservations in blocker
            var existingReservationsInBlocker = await _context.Reservation
                .Include(r => r.User)
                .Where(r => r.StartDate > blocker.StartDate && r.EndDate < blocker.EndDate)
                .ToListAsync();
            foreach (var reservation in existingReservationsInBlocker)
            {
                var email = new Email
                {
                    To = reservation.User.Email,
                    Subject = "CarWash reservation deleted",
                    Body = $@"Hi {reservation.User.FirstName}, 
we just wanted to let you know, that your reservation for {reservation.StartDate:MMMM d, h:mm tt} for your car ({reservation.VehiclePlateNumber}) was deleted because one of the following reasons: 
    - we won't work on that day (holidays, etc.)
    - the CarWash will be closed because of some technical issues
If you have any questions, please contact us at +36 70 450 6612, +36 30 359 4870 or mimosonk@gmail.com!
Sorry for the inconvenience!"
                };

                _context.Reservation.Remove(reservation);
                await _context.SaveChangesAsync();

                try
                {
                    await email.Send();

                    // Delete calendar event using Microsoft Graph
                    await _calendarService.DeleteEventAsync(reservation);
                }
                catch (Exception e)
                {
                    _telemetryClient.TrackException(e);
                }
            }

            _context.Blocker.Add(blocker);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBlocker", new { id = blocker.Id }, blocker);
        }

        // DELETE: api/blockers/{id}
        /// <summary>
        /// Delete an existing blocker
        /// </summary>
        /// <param name="id">blocker id</param>
        /// <returns>The deleted <see cref="Blocker"/></returns>
        /// <response code="200">OK</response>
        /// <response code="400">BadRequest if <paramref name="id"/> is missing or not well-formatted.</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="403">Forbidden if user is carwash admin.</response>
        /// <response code="404">NotFound if blocker not found.</response>
        [HttpDelete("{id}")]
        public async Task<ActionResult<Blocker>> DeleteBlocker([FromRoute] string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var blocker = await _context.Blocker.FindAsync(id);
            if (blocker == null) return NotFound();

            _context.Blocker.Remove(blocker);
            await _context.SaveChangesAsync();

            return Ok(blocker);
        }
    }
}