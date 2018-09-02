using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MSHU.CarWash.ClassLibrary;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using MSHU.CarWash.ClassLibrary.Services;

namespace MSHU.CarWash.Functions
{
    public static class ReminderFunction
    {
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
                .Where(r => r.StartDate.AddMinutes(-30) < DateTime.Now.ToLocalTime() && DateTime.Now.ToLocalTime() < r.StartDate)
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

                switch (reservation.User.NotificationChannel)
                {
                    case NotificationChannel.NotSet:
                    case NotificationChannel.Disabled:
                        log.LogInformation($"Notifications are not enabled for the user with id: {reservation.User.Id}.");
                        break;
                    case NotificationChannel.Email:
                        await SendEmailReminder(reservation);
                        log.LogInformation($"Email notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        break;
                    case NotificationChannel.Push:
                        await SendPushReminder(reservation, context);
                        log.LogInformation($"Push notification was sent to the user ({reservation.User.Id}) about the reservation with id: {reservation.Id}. ({watch.ElapsedMilliseconds}ms)");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            log.LogInformation("All reminders have been sent successfully.");
            watch.Stop();
        }

        private static async Task SendEmailReminder(Reservation reservation)
        {
            var email = new Email
            {
                To = reservation.User.Email,
                Subject = "CarWash account deleted",
                Body = $@"Hi {reservation.User.FirstName}, 
It's time to leave the key at the reception and <a href='https://carwashu.azurewebsites.net'>confirm drop-off & vehicle location by clicking here</a>!

If don't want to get email reminders in the future, you can <a href='https://carwashu.azurewebsites.net'>disable it in the settings</a>."
            };

            await email.Send();
        }

        private static async Task SendPushReminder(Reservation reservation, IPushDbContext context)
        {
            var vapidSubject = Environment.GetEnvironmentVariable("VapidSubject", EnvironmentVariableTarget.Process);
            var vapidPublicKey = Environment.GetEnvironmentVariable("VapidPublicKey", EnvironmentVariableTarget.Process);
            var vapidPrivateKey = Environment.GetEnvironmentVariable("VapidPrivateKey", EnvironmentVariableTarget.Process);

            var pushService = new PushService(context, vapidSubject, vapidPublicKey, vapidPrivateKey);

            await pushService.Send(reservation.UserId, "It's time to leave the key at the reception and confirm vehicle location!");
        }
    }
}
