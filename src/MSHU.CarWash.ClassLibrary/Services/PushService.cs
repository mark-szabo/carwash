using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebPush;
using PushSubscription = MSHU.CarWash.ClassLibrary.Models.PushSubscription;

namespace MSHU.CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class PushService : IPushService
    {
        private readonly WebPushClient _client;
        private readonly IPushDbContext _context;
        private readonly TelemetryClient _telemetryClient;
        private readonly VapidDetails _vapidDetails;

        /// <inheritdoc />
        public PushService(IPushDbContext context, string vapidSubject, string vapidPublicKey, string vapidPrivateKey)
        {
            _context = context;
            _client = new WebPushClient();
            _telemetryClient = new TelemetryClient();

            CheckOrGenerateVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);

            _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        }

        /// <inheritdoc />
        public PushService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _client = new WebPushClient();
            _telemetryClient = new TelemetryClient();

            var vapidSubject = configuration.GetValue<string>("Vapid:Subject");
            var vapidPublicKey = configuration.GetValue<string>("Vapid:PublicKey");
            var vapidPrivateKey = configuration.GetValue<string>("Vapid:PrivateKey");

            CheckOrGenerateVapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);

            _vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        }

        /// <inheritdoc />
        public void CheckOrGenerateVapidDetails(string vapidSubject, string vapidPublicKey, string vapidPrivateKey)
        {
            if (string.IsNullOrEmpty(vapidSubject) || string.IsNullOrEmpty(vapidPublicKey) ||
                string.IsNullOrEmpty(vapidPrivateKey))
            {
                var vapidKeys = VapidHelper.GenerateVapidKeys();

                // Prints 2 URL Safe Base64 Encoded Strings
                Debug.WriteLine($"Public {vapidKeys.PublicKey}");
                Debug.WriteLine($"Private {vapidKeys.PrivateKey}");

                throw new Exception(
                    "You must set the Vapid:Subject, Vapid:PublicKey and Vapid:PrivateKey application settings or pass them to the service in the constructor. You can use the ones just printed to the debug console.");
            }
        }

        /// <inheritdoc />
        public string GetVapidPublicKey() => _vapidDetails.PublicKey;

        /// <inheritdoc />
        public async Task Register(PushSubscription subscription)
        {
            if (await _context.PushSubscription.AnyAsync(s => s.P256Dh == subscription.P256Dh)) return;

            await _context.PushSubscription.AddAsync(subscription);
            await _context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task Send(string userId, Notification notification)
        {
            foreach (var subscription in await GetUserSubscriptions(userId))
                try
                {
                    _client.SendNotification(subscription.ToWebPushSubscription(), JsonConvert.SerializeObject(notification), _vapidDetails);
                }
                catch (WebPushException e)
                {
                    if (e.Message == "Subscription no longer valid")
                    {
                        _context.PushSubscription.Remove(subscription);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _telemetryClient.TrackException(e);
                    }
                }
        }

        /// <inheritdoc />
        public async Task Send(string userId, string text)
        {
            await Send(userId, new Notification(text));
        }

        /// <summary>
        /// Loads a list of user subscriptions from the database
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns>List of subscriptions</returns>
        private async Task<List<PushSubscription>> GetUserSubscriptions(string userId) =>
            await _context.PushSubscription.Where(s => s.UserId == userId).ToListAsync();
    }
}
