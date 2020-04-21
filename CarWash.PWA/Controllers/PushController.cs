using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using System.Threading.Tasks;
using CarWash.PWA.Attributes;
using Microsoft.Extensions.Hosting;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// VAPID Push Notification API
    /// </summary>
    [Produces("application/json")]
    [Authorize]
    [Route("api/push")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly User _user;
        private readonly IWebHostEnvironment _env;
        private readonly IPushService _pushService;

        /// <inheritdoc />
        public PushController(IUsersController usersController, IWebHostEnvironment hostingEnvironment, IPushService pushService)
        {
            _user = usersController.GetCurrentUser();
            _env = hostingEnvironment;
            _pushService = pushService;
        }

        // GET: api/push/vapidpublickey
        /// <summary>
        /// Get VAPID Public Key
        /// </summary>
        /// <returns>VAPID Public Key</returns>
        /// <response code="200">OK</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet, Route("vapidpublickey")]
        public ActionResult<string> GetVapidPublicKey()
        {
            return Ok(_pushService.GetVapidPublicKey());
        }

        // POST: api/push/register
        /// <summary>
        /// Register for push notifications
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="204">NoContent</response>
        /// <response code="400">BadRequest if subscription is null or invalid.</response>
        /// <response code="401">Unauthorized</response>
        [UserAction]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PushSubscriptionViewModel subscription)
        {
            var dbSubscription = new PushSubscription
            {
                Id = Guid.NewGuid().ToString(),
                UserId = _user.Id,
                Endpoint = subscription.Subscription.Endpoint,
                ExpirationTime = subscription.Subscription.ExpirationTime,
                Auth = subscription.Subscription.Keys.Auth,
                P256Dh = subscription.Subscription.Keys.P256Dh
            };

            await _pushService.Register(dbSubscription);

            return NoContent();
        }

        // POST: api/push/send
        /// <summary>
        /// Send a push notifications to a specific user's every device (for development only!)
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="202">Accepted</response>
        /// <response code="400">BadRequest if subscription is null or invalid.</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("send/{userId}")]
        public async Task<IActionResult> Send([FromRoute] string userId, [FromBody] Notification notification)
        {
            if (!_env.IsDevelopment()) return Forbid();

            await _pushService.Send(userId, notification);

            return Accepted();
        }
    }

    /// <summary>
    /// Request body model for Push registration
    /// </summary>
    public class PushSubscriptionViewModel
    {
        /// <inheritdoc cref="Subscription"/>
        public Subscription Subscription { get; set; }

        /// <summary>
        /// Device id for later use.
        /// </summary>
        public string DeviceId { get; set; }
    }

    /// <summary>
    /// Representation of the Web Standard Push API's <see href="https://developer.mozilla.org/en-US/docs/Web/API/PushSubscription">PushSubscription</see>
    /// </summary>
    public class Subscription
    {
        /// <summary>
        /// The endpoint associated with the push subscription.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// The subscription expiration time associated with the push subscription, if there is one, or null otherwise.
        /// </summary>
        public double? ExpirationTime { get; set; }

        /// <inheritdoc cref="Keys"/>
        public Keys Keys { get; set; }

        /// <summary>
        /// Converts the push subscription to the format of the library WebPush
        /// </summary>
        /// <returns>WebPush subscription</returns>
        public WebPush.PushSubscription ToWebPushSubscription() => new WebPush.PushSubscription(Endpoint, Keys.P256Dh, Keys.Auth);
    }

    /// <summary>
    /// Contains the client's public key and authentication secret to be used in encrypting push message data.
    /// </summary>
    public class Keys
    {
        /// <summary>
        /// An <see href="https://en.wikipedia.org/wiki/Elliptic_curve_Diffie%E2%80%93Hellman">Elliptic curve Diffie–Hellman</see> public key on the P-256 curve (that is, the NIST secp256r1 elliptic curve).
        /// The resulting key is an uncompressed point in ANSI X9.62 format.
        /// </summary>
        public string P256Dh { get; set; }

        /// <summary>
        /// An authentication secret, as described in <see href="https://tools.ietf.org/html/draft-ietf-webpush-encryption-08">Message Encryption for Web Push</see>.
        /// </summary>
        public string Auth { get; set; }
    }
}