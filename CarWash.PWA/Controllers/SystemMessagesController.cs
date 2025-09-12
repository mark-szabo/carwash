using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Controller for managing system messages.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SystemMessagesController"/> class.
    /// </remarks>
    /// <param name="context">The database context.</param>
    /// <param name="userService">User provider service.</param>
    [Produces("application/json")]
    [Route("api/systemmessages")]
    [ApiController]
    public class SystemMessagesController(ApplicationDbContext context, IUserService userService, ICloudflareService cloudflareService) : ControllerBase
    {
        private readonly User _user = userService.CurrentUser ?? throw new Exception("User is not authenticated.");

        /// <summary>
        /// Gets all system messages.
        /// </summary>
        /// <returns>A list of system messages.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemMessage>>> GetSystemMessages()
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            return await context.SystemMessage.ToListAsync();
        }

        /// <summary>
        /// Creates a new system message.
        /// </summary>
        /// <param name="systemMessage">The system message to create.</param>
        /// <returns>The created system message.</returns>
        [HttpPost]
        public async Task<ActionResult<SystemMessage>> CreateSystemMessage(SystemMessage systemMessage)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            context.SystemMessage.Add(systemMessage);
            await context.SaveChangesAsync();

            // Purge Cloudflare cache after system message creation
            await cloudflareService.PurgeConfigurationCacheAsync();

            return CreatedAtAction(nameof(GetSystemMessages), new { id = systemMessage.Id }, systemMessage);
        }

        /// <summary>
        /// Updates an existing system message.
        /// </summary>
        /// <param name="id">The ID of the system message to update.</param>
        /// <param name="systemMessage">The updated system message.</param>
        /// <returns>No content if successful.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSystemMessage(string id, SystemMessage systemMessage)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            if (id != systemMessage.Id)
            {
                return BadRequest();
            }

            context.Entry(systemMessage).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                
                // Purge Cloudflare cache after system message update
                await cloudflareService.PurgeConfigurationCacheAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SystemMessageExists(id))
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

        /// <summary>
        /// Deletes a system message.
        /// </summary>
        /// <param name="id">The ID of the system message to delete.</param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSystemMessage(string id)
        {
            if (!_user.IsCarwashAdmin) return Forbid();

            var systemMessage = await context.SystemMessage.FindAsync(id);
            if (systemMessage == null)
            {
                return NotFound();
            }

            context.SystemMessage.Remove(systemMessage);
            await context.SaveChangesAsync();

            // Purge Cloudflare cache after system message deletion
            await cloudflareService.PurgeConfigurationCacheAsync();

            return NoContent();
        }

        private bool SystemMessageExists(string id)
        {
            return context.SystemMessage.Any(e => e.Id == id);
        }
    }
}
