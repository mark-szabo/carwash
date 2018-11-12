using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MSHU.CarWash.ClassLibrary.Models;

namespace MSHU.CarWash.PWA.Controllers
{
    /// <summary>
    /// Managing calendar blockers
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BlockersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly TelemetryClient _telemetryClient;

        /// <inheritdoc />
        public BlockersController(ApplicationDbContext context, UsersController usersController)
        {
            _context = context;
            _user = usersController.GetCurrentUser();
            _telemetryClient = new TelemetryClient();
        }

        // GET: api/blockers
        [HttpGet]
        public IEnumerable<Blocker> GetBlocker()
        {
            return _context.Blocker;
        }

        // GET: api/blockers/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBlocker([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var blocker = await _context.Blocker.FindAsync(id);

            if (blocker == null)
            {
                return NotFound();
            }

            return Ok(blocker);
        }

        // POST: api/blockers
        [HttpPost]
        public async Task<IActionResult> PostBlocker([FromBody] Blocker blocker)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Blocker.Add(blocker);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetBlocker", new { id = blocker.Id }, blocker);
        }

        // DELETE: api/blockers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBlocker([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var blocker = await _context.Blocker.FindAsync(id);
            if (blocker == null)
            {
                return NotFound();
            }

            _context.Blocker.Remove(blocker);
            await _context.SaveChangesAsync();

            return Ok(blocker);
        }
    }
}