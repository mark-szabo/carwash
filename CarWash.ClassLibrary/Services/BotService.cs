using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class BotService : IBotService
    {
        private readonly CarWashConfiguration _configuration;
        private readonly TelemetryClient _telemetryClient;

        /// <inheritdoc />
        public BotService(CarWashConfiguration configuration)
        {
            _configuration = configuration;
            _telemetryClient = new TelemetryClient();
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
        private async Task SendMessage(string queueName, ServiceBusMessage message)
        {
            try
            {
                var queueClient = new QueueClient(_configuration.ConnectionStrings.ServiceBus, queueName);

                // Create a new message to send to the queue.
                var serviceBusMessage = new Message(Encoding.UTF8.GetBytes(message.ToJson()));

                // Send the message to the queue. 
                await queueClient.SendAsync(serviceBusMessage);

                await queueClient.CloseAsync();
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }
        }
    }
}
