using Microsoft.Azure.WebJobs;
using Microsoft.Azure.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using MSHU.CarWash.ClassLibrary.Services;
using MSHU.CarWash.ClassLibrary.Models.ServiceBus;
using MSHU.CarWash.ClassLibrary.Extensions;

namespace MSHU.CarWash.Functions
{
    public static class ReminderFunction
    {
        /// <summary>
        /// Number of minutes before a reservation after we should send the reminder
        /// The function is running every 10 minutes, so use a number not dividable with 10 for consistent execution.
        /// </summary>
        private const int MinutesBeforeReservationToSendReminder = 35;

        /// <summary>
        /// Service Bus queue name for the chat bot's drop-off reminders.
        /// </summary>
        private const string BotReminderQueueName = "bot-dropoff-reminder";

        /// <summary>
        /// Checks for reservations where a reminder should be sent to drop-off the key and confirm vehicle location
        /// This function will get triggered/executed every 10 minutes: "*/10 * * * *"
        /// Debug every 15 seconds: "0,15,30,45 * * * * *"
        /// </summary>
        /// <param name="timer"></param>
        /// <param name="log"></param>
        [FunctionName("ReminderFunction")]
        public static async Task Run([TimerTrigger("*/10 * * * *")]TimerInfo timer, ILogger log)
        {
            log.LogInformation($"Reminder function executed at: {DateTime.Now}");
            var watch = Stopwatch.StartNew();

            // Get reservations from SQL database
            var context = new FunctionsDbContext();
            var reservations = await context.Reservation
                .Include(r => r.User)
                .Where(r => r.State == State.SubmittedNotActual && r.StartDate.Date == DateTime.Today)
                .ToListAsync();

            // Cannot use normal LINQ as dates are not in UTC in database. TODO: refactor database to use UTC based times
            reservations = reservations
                .Where(r => r.StartDate.AddMinutes(MinutesBeforeReservationToSendReminder * -1) < DateTime.Now.ToLocalTime() && DateTime.Now.ToLocalTime() < r.StartDate)
                .ToList();

            if (reservations.Count == 0)
            {
                log.LogInformation($"No reservations found where reminder should be sent. Exiting. ({watch.ElapsedMilliseconds}ms)");
                return;
            }

            log.LogInformation($"Found {reservations.Count} reservations. Starting reminder sending. ({watch.ElapsedMilliseconds}ms)");

            foreach (var reservation in reservations)
            {
                reservation.State = State.ReminderSentWaitingForKey;
                await context.SaveChangesAsync();
                log.LogInformation($"Reservation state updated to ReminderSentWaitingForKey for reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");

                if (reservation.User == null) throw new Exception($"Failed to load user with id: {reservation.UserId}");

                switch (reservation.User.NotificationChannel)
                {
                    case NotificationChannel.Disabled:
                        log.LogInformation($"Notifications are not enabled for the user with id: {reservation.User.Id}.");
                        break;
                    case NotificationChannel.NotSet:
                    case NotificationChannel.Email:
                        await SendEmailReminder(reservation);
                        log.LogInformation($"Email notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        break;
                    case NotificationChannel.Push:
                        try
                        {
                            await SendPushReminder(reservation, context);
                            log.LogInformation($"Push notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        }
                        catch (Exception e)
                        {
                            log.LogError(e, "Push notification cannot be sent. Failover to email.");
                            await SendEmailReminder(reservation);
                            log.LogInformation($"Email notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Send message to ServiceBus which is monitored by the bot, who will ping the user
                await SendBotReminderMessage(reservation);
                log.LogInformation($"Bot reminder was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
            }

            log.LogInformation("All reminders have been sent successfully.");
            watch.Stop();
        }

        private static async Task SendEmailReminder(Reservation reservation)
        {
            var email = new Email
            {
                To = reservation.User.Email,
                Subject = "CarWash reminder",
                Body = $@"Hi {reservation.User.FirstName}, 
It's time to leave the key at the reception and <a href='https://carwashu.azurewebsites.net'>confirm drop-off & vehicle location by clicking here</a>!

If don't want to get email reminders in the future, you can <a href='https://carwashu.azurewebsites.net/settings'>disable it in the settings</a>."
            };

            try { await email.Send(); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send email to user with id: {reservation.UserId}. See inner exception.", e);
            }
        }

        private static async Task SendPushReminder(Reservation reservation, IPushDbContext context)
        {
            var vapidSubject = Environment.GetEnvironmentVariable("VapidSubject", EnvironmentVariableTarget.Process);
            var vapidPublicKey = Environment.GetEnvironmentVariable("VapidPublicKey", EnvironmentVariableTarget.Process);
            var vapidPrivateKey = Environment.GetEnvironmentVariable("VapidPrivateKey", EnvironmentVariableTarget.Process);

            var pushService = new PushService(context, vapidSubject, vapidPublicKey, vapidPrivateKey);

            var notification = new Notification
            {
                Title = "CarWash reminder",
                Body = "It's time to leave the key at the reception and confirm vehicle location!",
                Tag = NotificationTag.Reminder,
                RequireInteraction = true,
                Actions = new List<NotificationAction>
                {
                    new NotificationAction { Action = "confirm-dropoff", Title = "Confirm drop-off" }
                }
            };

            try { await pushService.Send(reservation.UserId, notification); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send push to user with id: {reservation.UserId}. See inner exception.", e);
            }
        }

        private static async Task SendBotReminderMessage(Reservation reservation)
        {
            var connectionString = Environment.GetEnvironmentVariable("ServiceBus", EnvironmentVariableTarget.Process);
            var queueClient = new QueueClient(connectionString, BotReminderQueueName);

            var message = new DropoffReminderMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            // Create a new message to send to the queue.
            var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(message.ToJson()));

            // Send the message to the queue.            
            try { await queueClient.SendAsync(serviceBusMessage); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send message to the bot, who would have pinged the user with id: {reservation.UserId}. See inner exception.", e);
            }

            await queueClient.CloseAsync();
        }
    }
}
