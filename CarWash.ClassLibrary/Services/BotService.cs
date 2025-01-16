using Azure.Messaging.ServiceBus;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    /// <inheritdoc />
    public class BotService(IOptionsMonitor<CarWashConfiguration> configuration, TelemetryClient telemetryClient) : IBotService
    {

        /// <inheritdoc />
        public Task SendDropoffReminderMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(configuration.CurrentValue.ServiceBusQueues.BotDropoffReminderQueue, message);
        }

        /// <inheritdoc />
        public Task SendWashStartedMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(configuration.CurrentValue.ServiceBusQueues.BotWashStartedQueue, message);
        }

        /// <inheritdoc />
        public Task SendWashCompletedMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(configuration.CurrentValue.ServiceBusQueues.BotWashCompletedQueue, message);
        }

        /// <inheritdoc />
        public Task SendCarWashCommentLeftMessageAsync(Reservation reservation)
        {
            var message = new ReservationServiceBusMessage
            {
                UserId = reservation.UserId,
                ReservationId = reservation.Id,
            };

            return SendMessage(configuration.CurrentValue.ServiceBusQueues.BotCarWashCommentLeftQueue, message);
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
                if (configuration.CurrentValue.ConnectionStrings.ServiceBus == null)
                {
                    // ServiceBus is not configured, do nothing
                    return;
                }

                // since ServiceBusClient implements IAsyncDisposable we create it with "await using"
                await using var client = new ServiceBusClient(configuration.CurrentValue.ConnectionStrings.ServiceBus);

                // create the sender
                var sender = client.CreateSender(queueName);

                // send the message
                await sender.SendMessageAsync(new Azure.Messaging.ServiceBus.ServiceBusMessage(message.ToJson()));

                await sender.CloseAsync();
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);
            }
        }
    }
}
