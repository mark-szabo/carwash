using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Functions.Worker;

namespace CarWash.Functions
{
    public static class ParkIntegrationFunction
    {
        private static readonly HttpClient _client = new HttpClient();
        private static readonly FunctionsDbContext _context = new FunctionsDbContext();
        private static readonly string _parkApiEmail = Environment.GetEnvironmentVariable("ParkApi:Email", EnvironmentVariableTarget.Process);
        private static readonly string _parkApiPassword = Environment.GetEnvironmentVariable("ParkApi:Password", EnvironmentVariableTarget.Process);

        /// <summary>
        /// Service Bus queue name for the chat bot's vehicle-arrived notification.
        /// </summary>
        private const string BotNotificationQueueName = "bot-vehicle-arrived-notification";

        /// <summary>
        /// Checks the park management system for newly arrived cars
        /// and sends a notification to the owners if they have a reservation on the given day.
        /// This function will get triggered/executed every minute from 6h through 15h on every workday: "0 * 6-15 * * 1-5"
        /// Debug every 15 seconds: "0,15,30,45 * * * * *"
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="log"></param>
        [Function("ParkIntegrationFunction")]
        public static async Task RunAsync([TimerTrigger("0 * 6-15 * * 1-5")]TimerInfo timer, ILogger log)
        {
            log.LogInformation($"Park integration function executed at: {DateTime.Now}");
            var watch = Stopwatch.StartNew();

            var parkingSessions = await GetParkingSessions(2, log);
            log.LogInformation($"Parking sessions retrived from Park API. ({watch.ElapsedMilliseconds}ms)");

            var licensePlates = parkingSessions
                .Where(s => s.start > DateTime.UtcNow.AddMinutes(-5))
                .Select(s => s.vehicle.normalized_licence_plate.ToUpper())
                .ToList();

            log.LogMetric("VehicleArrived", parkingSessions.Count(s => s.start > DateTime.UtcNow.AddMinutes(-1)));

            if (licensePlates.Count == 0)
            {
                log.LogInformation($"No new vehicles have arrived. Exiting. ({watch.ElapsedMilliseconds}ms)");
                return;
            }
            log.LogInformation($"{licensePlates.Count} vehicles have arrived in the last 5 minutes. ({watch.ElapsedMilliseconds}ms)");


            // Get reservations from SQL database where the car has just arrived
            var reservations = await _context.Reservation
                .Include(r => r.User)
                .Where(r =>
                    r.StartDate.Date == DateTime.Today &&
                    r.State == State.SubmittedNotActual &&
                    licensePlates.Contains(r.VehiclePlateNumber))
                .ToListAsync();

            if (reservations.Count == 0)
            {
                log.LogInformation($"No reservations found where notification should be sent. Exiting. ({watch.ElapsedMilliseconds}ms)");
                return;
            }

            log.LogInformation($"Found {reservations.Count} reservations. Starting notification sending. ({watch.ElapsedMilliseconds}ms)");

            // Send notifications
            foreach (var reservation in reservations)
            {
                reservation.State = State.ReminderSentWaitingForKey;
                await _context.SaveChangesAsync();
                log.LogInformation($"Reservation state updated to ReminderSentWaitingForKey for reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");

                if (reservation.User == null) throw new Exception($"Failed to load user with id: {reservation.UserId}");

                var email = new Email
                {
                    To = reservation.User.Email,
                    Subject = "Welcome in the office! Don't forget to drop off your key!",
                    Body = $@"Welcome {reservation.User.FirstName}, 
I just noticed that you've arrived! 👀 
Don't forget to leave the key at the reception and <a href='https://carwashu.azurewebsites.net'>confirm drop-off & vehicle location by clicking here</a>!

If don't want to get email reminders in the future, you can <a href='https://carwashu.azurewebsites.net/settings'>disable it in the settings</a>."
                };

                switch (reservation.User.NotificationChannel)
                {
                    case NotificationChannel.Disabled:
                        log.LogInformation($"Notifications are not enabled for the user with id: {reservation.User.Id}.");
                        break;
                    case NotificationChannel.NotSet:
                    case NotificationChannel.Email:
                        await NotificationService.SendEmailReminder(reservation, email);
                        log.LogInformation($"Email notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        break;
                    case NotificationChannel.Push:
                        try
                        {
                            var notification = new Notification
                            {
                                Title = "Welcome! 🙋‍",
                                Body = "I just noticed that you've arrived! 👀 Don't forget to leave the key at the reception and confirm vehicle location!",
                                Tag = NotificationTag.Reminder,
                                RequireInteraction = true,
                                Actions = new List<NotificationAction>
                                {
                                    new NotificationAction { Action = "confirm-dropoff", Title = "Confirm drop-off" }
                                }
                            };
                            await NotificationService.SendPushReminder(reservation, _context, notification);
                            log.LogInformation($"Push notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Push notification cannot be sent. Failover to email.");
                            await NotificationService.SendEmailReminder(reservation, email);
                            log.LogInformation($"Email notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Send message to ServiceBus which is monitored by the bot, who will ping the user
                await NotificationService.SendBotReminderMessage(reservation, BotNotificationQueueName);
                log.LogInformation($"Bot reminder was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
            }

            log.LogMetric("VehicleArrivedNotificationSent", reservations.Count);
            log.LogInformation("All reminders have been sent successfully.");
            watch.Stop();
        }

        private static async Task Authenticate(ILogger log)
        {
            try
            {
                var json = new { email = _parkApiEmail, password = _parkApiPassword }.ToJson();
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                if (!_client.DefaultRequestHeaders.Contains("accept")) _client.DefaultRequestHeaders.Add("accept", "application/json");
                if (!_client.DefaultRequestHeaders.Contains("referer")) _client.DefaultRequestHeaders.Add("referer", "https://graphisoft.x.rollet.app/login/");

                var response = await _client.PostAsync("https://graphisoft.x.rollet.app/api/latest/any/login/", content);

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.Unauthorized:
                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.Forbidden:
                    case System.Net.HttpStatusCode.InternalServerError:
                    case System.Net.HttpStatusCode.NotFound:
                        var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : null;
                        throw new Exception($"{response.ReasonPhrase}: {responseContent}");
                }

                response.EnsureSuccessStatusCode();

                log.LogInformation($"Authenticated successfully against Park API.");
            }
            catch (Exception e)
            {
                throw new Exception($"HTTP request error.", e);
            }
        }

        private static async Task<List<ParkingSession>> GetParkingSessions(int retryCount, ILogger log)
        {
            try
            {
                var response = await _client.GetAsync("https://graphisoft.x.rollet.app/api/latest/ten/parking-sessions/?is_running=true&fields=url,uuid,vehicle,start,start_image_color,start_image_infra,price,with_pass,is_voided,is_free,tenant,segments,payment,is_started_offline,is_vehicle_editable,session_type&page_size=15&page=1&ordering=-start&with_pass=true&with_ticket=true&with_individual_entry=true&session_type__in=ROLLET,GUEST,GUEST_BOOKING,PASS,TICKET,INDIVIDUAL_ENTRY");
                var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : null;

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.Forbidden:
                    case System.Net.HttpStatusCode.Unauthorized:
                        if (retryCount > 0)
                        {
                            await Authenticate(log);
                            return await GetParkingSessions(retryCount - 1, log);
                        }
                        else throw new Exception($"Authentication unsuccessful: {responseContent}");

                    case System.Net.HttpStatusCode.BadRequest:
                    case System.Net.HttpStatusCode.InternalServerError:
                    case System.Net.HttpStatusCode.NotFound:
                        throw new Exception($"{response.ReasonPhrase}: {responseContent}");
                }

                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<List<ParkingSession>>(responseContent);
            }
            catch (Exception e)
            {
                throw new Exception($"HTTP request error.", e);
            }
        }

        private class ParkingSession
        {
            public string url { get; set; }
            public string uuid { get; set; }
            public Vehicle vehicle { get; set; }
            public Payment payment { get; set; }
            public float price { get; set; }
            public object with_pass { get; set; }
            public string start_image_color { get; set; }
            public string start_image_infra { get; set; }
            public bool is_free { get; set; }
            public bool is_voided { get; set; }
            public DateTime start { get; set; }
            public string tenant { get; set; }
            public Segment[] segments { get; set; }
            public bool is_vehicle_editable { get; set; }
            public bool is_started_offline { get; set; }
            public string session_type { get; set; }
        }

        private class Vehicle
        {
            public string url { get; set; }
            public string licence_plate { get; set; }
            public string normalized_licence_plate { get; set; }
        }

        private class Payment
        {
            public float amount { get; set; }
            public string currency { get; set; }
            public object invoice_type { get; set; }
        }

        private class Segment
        {
            public Zone zone { get; set; }
            public string pass_contract { get; set; }
            public float price { get; set; }
            public DateTime start { get; set; }
            public DateTime? end { get; set; }
            public string start_image_color { get; set; }
            public string start_image_infra { get; set; }
            public string end_image_color { get; set; }
            public string end_image_infra { get; set; }
        }

        private class Zone
        {
            public string url { get; set; }
            public string name { get; set; }
            public string color_code { get; set; }
        }
    }
}
