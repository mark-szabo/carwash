using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSHU.CarWash.ClassLibrary
{
    public class PushSubscription : ApplicationDbContext.IEntity
    {
        public string Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        public string DeviceId { get; set; }

        [Required]
        public string Endpoint { get; set; }

        public double? ExpirationTime { get; set; }

        [Required]
        public string P256Dh { get; set; }

        [Required]
        public string Auth { get; set; }

        public PushSubscription() { }

        public PushSubscription(string userId, WebPush.PushSubscription subscription)
        {
            UserId = userId;
            Endpoint = subscription.Endpoint;
            ExpirationTime = null;
            P256Dh = subscription.P256DH;
            Auth = subscription.Auth;
        }

        public WebPush.PushSubscription ToWebPushSubscription() => new WebPush.PushSubscription(Endpoint, P256Dh, Auth);
    }
}
