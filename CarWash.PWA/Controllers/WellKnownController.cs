using System;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// .well-known API
    /// </summary>
    /// <inheritdoc />
    [Produces("application/json")]
    [Route("api/.well-known")]
    [ApiController]
    public class WellKnownController(IOptionsMonitor<CarWashConfiguration> configuration, ApplicationDbContext context, IPushService pushService) : ControllerBase
    {
        // GET: api/.well-known/configuration
        /// <summary>
        /// Get CarWash Configuration
        /// </summary>
        /// <returns>CarWash Configuration</returns>
        /// <response code="200">OK</response>
        [HttpGet, Route("configuration")]
        public async Task<ActionResult<WellKnown>> GetConfigurationAsync()
        {
            var wellKnown = new WellKnown
            {
                Slots = configuration.CurrentValue.Slots,
                Companies = await context.Company.ToListAsync(),
                Garages = configuration.CurrentValue.Garages,
                Services = configuration.CurrentValue.Services,
                ReservationSettings = configuration.CurrentValue.Reservation,
                ActiveSystemMessages = await context.SystemMessage
                    .Where(m => m.StartDateTime <= DateTime.UtcNow && m.EndDateTime >= DateTime.UtcNow)
                    .ToListAsync(),
                BuildNumber = configuration.CurrentValue.BuildNumber,
                Version = configuration.CurrentValue.Version
            };

            return Ok(wellKnown);
        }

        // GET: api/.well-known/vapidpublickey
        /// <summary>
        /// Get VAPID Public Key
        /// </summary>
        /// <returns>VAPID Public Key</returns>
        /// <response code="200">OK</response>
        [HttpGet, Route("vapidpublickey")]
        public ActionResult<string> GetVapidPublicKey()
        {
            return Ok(pushService.GetVapidPublicKey());
        }
    }
}
