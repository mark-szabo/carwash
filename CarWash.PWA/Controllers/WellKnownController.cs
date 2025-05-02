using Microsoft.AspNetCore.Mvc;
using CarWash.ClassLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using CarWash.ClassLibrary.Services;
using Microsoft.AspNetCore.Authorization;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// .well-known API
    /// </summary>
    /// <inheritdoc />
    [Produces("application/json")]
    [Route("api/.well-known")]
    [ApiController]
    [AllowAnonymous]
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
