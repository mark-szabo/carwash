using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database representation of a push subscription.
    /// Push subscriptions are used for sending out push notifications to users through the browser's own system but with a standard api.
    /// <see href="https://notifications.spec.whatwg.org/#dictdef-notificationoptions">Notification API Standard</see>
    /// DB mapped entity.
    /// </summary>
    public class PushSubscription : ApplicationDbContext.IEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PushSubscription"/> class.
        /// </summary>
        public PushSubscription() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PushSubscription"/> class.
        /// </summary>
        /// <param name="userId">User id.</param>
        /// <param name="subscription">WebPush subscription.</param>
        public PushSubscription(string userId, WebPush.PushSubscription subscription)
        {
            UserId = userId;
            Endpoint = subscription.Endpoint;
            ExpirationTime = null;
            P256Dh = subscription.P256DH;
            Auth = subscription.Auth;
        }

        /// <inheritdoc />
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user id associated with the push subscription.
        /// </summary>
        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a virtual user object populated from the database by <see cref="UserId"/>.
        /// </summary>
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the device id for later use.
        /// </summary>
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the endpoint associated with the push subscription.
        /// </summary>
        [Required]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the subscription expiration time associated with the push subscription, if there is one, or null otherwise.
        /// </summary>
        public double? ExpirationTime { get; set; }

        /// <summary>
        /// Gets or sets an
        /// <see href="https://en.wikipedia.org/wiki/Elliptic_curve_Diffie%E2%80%93Hellman">Elliptic curve Diffie-Hellman</see>
        /// public key on the P-256 curve (that is, the NIST secp256r1 elliptic curve).
        /// The resulting key is an uncompressed point in ANSI X9.62 format.
        /// </summary>
        [Required]
        [Key]
        public string P256Dh { get; set; }

        /// <summary>
        /// Gets or sets an authentication secret, as described in
        /// <see href="https://tools.ietf.org/html/draft-ietf-webpush-encryption-08">Message Encryption for Web Push</see>.
        /// </summary>
        [Required]
        public string Auth { get; set; }

        /// <summary>
        /// Converts the push subscription to the format of the WebPush library.
        /// </summary>
        /// <returns>WebPush subscription.</returns>
        public WebPush.PushSubscription ToWebPushSubscription()
        {
            return new WebPush.PushSubscription(Endpoint, P256Dh, Auth);
        }
    }
}
