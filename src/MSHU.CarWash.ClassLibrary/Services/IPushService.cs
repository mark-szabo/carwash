using System.Threading.Tasks;

namespace MSHU.CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to manage Push subscriptions and send Push notifications
    /// </summary>
    public interface IPushService
    {
        /// <summary>
        /// Checks VAPID info and if invalid generates new keys and throws exception
        /// </summary>
        /// <param name="subject">This should be a URL or a 'mailto:' email address</param>
        /// <param name="vapidPublicKey">The VAPID public key as a base64 encoded string</param>
        /// <param name="vapidPrivateKey">The VAPID private key as a base64 encoded string</param>
        void CheckOrGenerateVapidDetails(string subject, string vapidPublicKey, string vapidPrivateKey);

        /// <summary>
        /// Get the server's saved VAPID public key
        /// </summary>
        /// <returns>VAPID public key</returns>
        string GetVapidPublicKey();

        /// <summary>
        /// Register a push subscription (save to the database for later use)
        /// </summary>
        /// <param name="subscription">push subscription</param>
        Task Register(PushSubscription subscription);

        /// <summary>
        /// Send a push notification to a user with a string text (payload)
        /// </summary>
        /// <param name="userId">user id the push should be sent to</param>
        /// <param name="payload">text payload</param>
        Task Send(string userId, string payload);

        /// <summary>
        /// Send a push notification to a user with an object as payload
        /// </summary>
        /// <param name="userId">user id the push should be sent to</param>
        /// <param name="payload">payload object</param>
        Task Send(string userId, object payload);
    }
}
