using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.Azure.Functions.Worker;

namespace CarWash.Functions
{
    public class ReminderFunction(ILogger<ReminderFunction> log)
    {
        private static readonly FunctionsDbContext _context = new FunctionsDbContext();

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
        [Function("ReminderFunction")]
        public async Task Run([TimerTrigger("*/10 * * * *")] TimerInfo timer)
        {
            log.LogInformation($"Reminder function executed at: {DateTime.Now}");
            var watch = Stopwatch.StartNew();

            // Get reservations from SQL database
            var reservations = await _context.Reservation
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
                await _context.SaveChangesAsync();
                log.LogInformation($"Reservation state updated to ReminderSentWaitingForKey for reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");

                if (reservation.User == null) throw new Exception($"Failed to load user with id: {reservation.UserId}");

                var email = new Email
                {
                    To = reservation.User.Email,
                    Subject = "CarWash reminder",
                    Body = $@"Hi {reservation.User.FirstName}, 
It's time to leave the key at the reception and <a href='https://carwashu.azurewebsites.net'>confirm drop-off & vehicle location by clicking here</a>!

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
                                Title = "CarWash reminder",
                                Body = "It's time to leave the key at the reception and confirm vehicle location!",
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
                await NotificationService.SendBotReminderMessage(reservation, BotReminderQueueName);
                log.LogInformation($"Bot reminder was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
            }

            log.LogInformation("All reminders have been sent successfully.");
            watch.Stop();
        }
    }
}
