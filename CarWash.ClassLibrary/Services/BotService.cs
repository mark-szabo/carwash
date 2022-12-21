using Azure.Messaging.ServiceBus;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.ApplicationInsights;
using System;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class BotService : IBotService
    {
        private readonly CarWashConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;

        /// <inheritdoc />
        public BotService(CarWashConfiguration configuration, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _telemetryClient = telemetryClient;
        }

        /// <inheritdoc />
        public Task SendDropoffReminderMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(_configuration.ServiceBusQueues.BotDropoffReminderQueue, message);
        }

        /// <inheritdoc />
        public Task SendWashStartedMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(_configuration.ServiceBusQueues.BotWashStartedQueue, message);
        }

        /// <inheritdoc />
        public Task SendWashCompletedMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(_configuration.ServiceBusQueues.BotWashCompletedQueue, message);
        }

        /// <inheritdoc />
        public Task SendCarWashCommentLeftMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(_configuration.ServiceBusQueues.BotCarWashCommentLeftQueue, message);
        }

        /// <summary>
        /// Try to send message through bot.
        /// </summary>
        /// <param name="queueName">Service Bus Queue name</param>
        /// <param name="message">Service Bus Queue message</param>
        /// <returns></returns>
        private async Task SendMessage(string queueName, Models.ServiceBus.ServiceBusMessage message)
        {
            try
            {
                // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
                await using var client = new ServiceBusClient(_configuration.ConnectionStrings.ServiceBus);

                // create the sender
                var sender = client.CreateSender(queueName);

                // send the message
                await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(message.ToJson()));

                await sender.CloseAsync();
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }
        }
    }
}
