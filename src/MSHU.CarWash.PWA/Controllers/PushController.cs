using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MSHU.CarWash.ClassLibrary;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebPush;
using PushSubscription = MSHU.CarWash.ClassLibrary.PushSubscription;

namespace MSHU.CarWash.PWA.Controllers
{
    /// <summary>
    /// VAPID Push Notification API
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly User _user;
        private readonly string _vapidSubject;
        private readonly string _vapidPublicKey;
        private readonly string _vapidPrivateKey;
        private readonly VapidDetails _vapidDetails;

        /// <inheritdoc />
        public PushController(ApplicationDbContext context, UsersController usersController, IConfiguration configuration)
        {
            _context = context;
            _user = usersController.GetCurrentUser();

            _vapidSubject = configuration.GetValue<string>("Vapid:Subject");
            _vapidPublicKey = configuration.GetValue<string>("Vapid:PublicKey");
            _vapidPrivateKey = configuration.GetValue<string>("Vapid:PrivateKey");

            if (string.IsNullOrEmpty(_vapidPublicKey) || string.IsNullOrEmpty(_vapidPrivateKey))
            {
                Debug.WriteLine("You must set the Vapid:Subject, Vapid:PublicKey and Vapid:PrivateKey application settings. You can use the following ones:");

                var vapidKeys = VapidHelper.GenerateVapidKeys();

                // Prints 2 URL Safe Base64 Encoded Strings
                Debug.WriteLine($"Public {vapidKeys.PublicKey}");
                Debug.WriteLine($"Private {vapidKeys.PrivateKey}");

                return;
            }

            _vapidDetails = new VapidDetails(_vapidSubject, _vapidPublicKey, _vapidPrivateKey);
        }

        // GET: api/push/vapidpublickey
        /// <summary>
        /// Get VAPID Public Key
        /// </summary>
        /// <returns>VAPID Public Key</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(string), 200)]
        [HttpGet, Route("vapidpublickey")]
        public IActionResult GetVapidPublicKey()
        {
            return Ok(_vapidPublicKey);
        }

        // POST: api/push/register
        /// <summary>
        /// Register for push notifications
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if subscription is null or invalid.</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(NoContentResult), 204)]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PushSubscriptionViewModel subscription)
        {
            var dbSubscription = new PushSubscription
            {
                UserId = _user.Id,
                Endpoint = subscription.Subscription.Endpoint,
                ExpirationTime = subscription.Subscription.ExpirationTime,
                Auth = subscription.Subscription.Keys.Auth,
                P256Dh = subscription.Subscription.Keys.P256Dh
            };

            if (!await _context.PushSubscription.AnyAsync(s =>
                s.UserId == dbSubscription.UserId &&
                s.Endpoint == dbSubscription.Endpoint &&
                s.Auth == dbSubscription.Auth &&
                s.P256Dh == dbSubscription.P256Dh))
            {
                await _context.PushSubscription.AddAsync(dbSubscription);
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction("Register", null);
        }

        // POST: api/push/send
        /// <summary>
        /// Send a push notifications
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="201">Created</response>
        /// <response code="400">BadRequest if subscription is null or invalid.</response>
        /// <response code="401">Unauthorized</response>
        [ProducesResponseType(typeof(NoContentResult), 204)]
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] string userId)
        {
            var client = new WebPushClient();

            var subscriptions = await _context.PushSubscription.Where(s => s.UserId == userId).ToListAsync();

            try
            {
                foreach (var subscription in subscriptions)
                {
                    client.SendNotification(subscription.ToWebPushSubscription(), "payload", _vapidDetails);
                }
            }
            catch (WebPushException e)
            {
                Debug.WriteLine("Error during push notification sending: " + e.Message);
            }

            return CreatedAtAction("Send", null);
        }
    }

    public class PushSubscriptionViewModel
    {
        public Subscription Subscription { get; set; }
    }

    public class Subscription
    {
        public string Endpoint { get; set; }
        public double? ExpirationTime { get; set; }
        public Keys Keys { get; set; }

        public WebPush.PushSubscription ToWebPushSubscription() => new WebPush.PushSubscription(Endpoint, Keys.P256Dh, Keys.Auth);
    }

    public class Keys
    {
        public string P256Dh { get; set; }
        public string Auth { get; set; }
    }
}