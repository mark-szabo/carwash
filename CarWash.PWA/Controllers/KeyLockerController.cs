using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Key Locker API
    /// </summary>
    [Produces("application/json")]
    [Route("api/keylocker")]
    [ApiController]
    public class KeyLockerController(
        IOptionsMonitor<CarWashConfiguration> configuration,
        ApplicationDbContext context,
        IKeyLockerService keyLockerService) : ControllerBase
    {
        /// <summary>
        /// Generate boxes for a locker.
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateBoxes([FromBody] GenerateBoxesRequest request)
        {
            await keyLockerService.GenerateBoxesToLocker(
                request.NamePrefix,
                request.NumberOfBoxes,
                request.Building,
                request.Floor,
                request.LockerId);

            return Ok();
        }

        /// <summary>
        /// Open a box by its unique ID.
        /// </summary>
        [HttpPost("open/by-id")]
        public async Task<IActionResult> OpenBoxById([FromBody] OpenBoxByIdRequest request)
        {
            await keyLockerService.OpenBoxByIdAsync(request.BoxId, request.UserId);
            return Ok();
        }

        /// <summary>
        /// Open a box by locker ID and box serial.
        /// </summary>
        [HttpPost("open/by-serial")]
        public async Task<IActionResult> OpenBoxBySerial([FromBody] OpenBoxBySerialRequest request)
        {
            await keyLockerService.OpenBoxBySerialAsync(request.LockerId, request.BoxSerial, request.UserId);
            return Ok();
        }

        /// <summary>
        /// Open a random available box in a locker.
        /// </summary>
        [HttpPost("open/random")]
        public async Task<ActionResult<string>> OpenRandomAvailableBox([FromBody] OpenRandomBoxRequest request)
        {
            var boxId = await keyLockerService.OpenRandomAvailableBoxAsync(request.LockerId, request.UserId);
            return Ok(boxId);
        }
    }

    // DTOs for requests
    public class GenerateBoxesRequest
    {
        public string NamePrefix { get; set; } = default!;
        public int NumberOfBoxes { get; set; }
        public string Building { get; set; } = default!;
        public string Floor { get; set; } = default!;
        public string? LockerId { get; set; }
    }

    public class OpenBoxByIdRequest
    {
        public string BoxId { get; set; } = default!;
        public string? UserId { get; set; }
    }

    public class OpenBoxBySerialRequest
    {
        public string LockerId { get; set; } = default!;
        public int BoxSerial { get; set; }
        public string? UserId { get; set; }
    }

    public class OpenRandomBoxRequest
    {
        public string LockerId { get; set; } = default!;
        public string? UserId { get; set; }
    }
}
