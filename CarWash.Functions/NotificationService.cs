﻿using Azure.Messaging.ServiceBus;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using CarWash.ClassLibrary.Services;
using Microsoft.Extensions.Logging;
using System;
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
            var vapidSubject = Environment.GetEnvironmentVariable("Vapid:Subject", EnvironmentVariableTarget.Process);
            var vapidPublicKey = Environment.GetEnvironmentVariable("Vapid:PublicKey", EnvironmentVariableTarget.Process);
            var vapidPrivateKey = Environment.GetEnvironmentVariable("Vapid:PrivateKey", EnvironmentVariableTarget.Process);

            var pushService = new PushService(context, vapidSubject, vapidPublicKey, vapidPrivateKey, new Microsoft.ApplicationInsights.TelemetryClient());

            try { await pushService.Send(reservation.UserId, notification); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send push to user with id: {reservation.UserId}. See inner exception.", e);
            }
        }

        public static async Task SendBotReminderMessage(Reservation reservation, string queueName, ILogger? log = null)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:ServiceBus", EnvironmentVariableTarget.Process);

            if (connectionString == null)
            {
                log?.LogWarning("Skipped sending bot message: ServiceBus connection string is not provided.");
            }

            // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
            await using var client = new ServiceBusClient(connectionString);

            // create the sender
            ServiceBusSender sender = client.CreateSender(queueName);

            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            // Encoding.UTF8.GetBytes(message.ToJson())
            // Send the message to the queue.            
            try { await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(message.ToJson())); }
            catch (Exception e)
            {
                throw new Exception($"Failed to send message to the bot, who would have pinged the user with id: {reservation.UserId}. See inner exception.", e);
            }

            await sender.CloseAsync();
        }
    }
}
