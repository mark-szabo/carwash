using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using CarWash.ClassLibrary.Services;
using Microsoft.Azure.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CarWash.Functions
{
    class NotificationService
    {
        public static async Task SendEmailReminder(Reservation reservation, Email email)
        {
            try { await email.Send(); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send email to user with id: {reservation.UserId}. See inner exception.", e);
            }
        }

        public static async Task SendPushReminder(Reservation reservation, IPushDbContext context, Notification notification)
        {
            var vapidSubject = Environment.GetEnvironmentVariable("VapidSubject", EnvironmentVariableTarget.Process);
            var vapidPublicKey = Environment.GetEnvironmentVariable("VapidPublicKey", EnvironmentVariableTarget.Process);
            var vapidPrivateKey = Environment.GetEnvironmentVariable("VapidPrivateKey", EnvironmentVariableTarget.Process);

            var pushService = new PushService(context, vapidSubject, vapidPublicKey, vapidPrivateKey, new Microsoft.ApplicationInsights.TelemetryClient());

            try { await pushService.Send(reservation.UserId, notification); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send push to user with id: {reservation.UserId}. See inner exception.", e);
            }
        }

        public static async Task SendBotReminderMessage(Reservation reservation, string queueName)
        {
            var connectionString = Environment.GetEnvironmentVariable("ServiceBus", EnvironmentVariableTarget.Process);
            var queueClient = new QueueClient(connectionString, queueName);

            var message = new ReservationServiceBusMessage
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
