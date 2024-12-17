using Microsoft.AspNetCore.Mvc;
using CarWash.ClassLibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// .well-known API
    /// </summary>
    /// <inheritdoc />
    [Produces("application/json")]
    [Route("api/.well-known")]
    [ApiController]
    public class WellKnownController(CarWashConfiguration configuration, ApplicationDbContext context) : ControllerBase
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
                Slots = configuration.Slots,
                Companies = await context.Company.ToListAsync(),
                Garages = configuration.Garages,
                Services = configuration.Services,
                ReservationSettings = configuration.Reservation,
                BuildNumber = configuration.BuildNumber,
                Version = configuration.Version
            };

            return Ok(wellKnown);
        }
    }
}
