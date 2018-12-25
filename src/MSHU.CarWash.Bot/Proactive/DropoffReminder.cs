using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.ServiceBus;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using MSHU.CarWash.ClassLibrary.Models.ServiceBus;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Proactive
{
    public class DropoffReminder
    {
        /// <summary>
        /// Key in the bot config (.bot file) for Service Bus.
        /// </summary>
        private const string ServiceBusConfiguration = "carwashuservicebus";

        /// <summary>
        /// Key in the bot config (.bot file) for the Endpoint.
        /// </summary>
        private const string EndpointConfiguration = "production";

        /// <summary>
        /// Service Bus queue name.
        /// </summary>
        private const string ServiceBusQueueName = "bot-dropoff-reminder";

        private readonly QueueClient _queueClient;
        private readonly string _botId;
        private readonly StateAccessors _accessors;
        private readonly BotFrameworkAdapter _botFrameworkAdapter;
        private readonly TelemetryClient _telemetryClient;

        public DropoffReminder(StateAccessors accessors, BotFrameworkAdapter botFrameworkAdapter, BotConfiguration botConfig, BotServices services)
        {
            _accessors = accessors;
            _botFrameworkAdapter = botFrameworkAdapter;
            _telemetryClient = new TelemetryClient();

            // Verify Endpoint configuration.
            if (!services.EndpointServices.ContainsKey(EndpointConfiguration))
            {
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a Endpoint service named '{EndpointConfiguration}'.");
            }

            _botId = services.EndpointServices[EndpointConfiguration].AppId;

            // Verify ServiceBus configuration.
            if (!services.ServiceBusServices.ContainsKey(ServiceBusConfiguration))
            {
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a Service Bus service named '{ServiceBusConfiguration}'.");
            }

            _queueClient = new QueueClient(services.ServiceBusServices[ServiceBusConfiguration], ServiceBusQueueName, ReceiveMode.PeekLock, null);
        }

        public void RegisterHandler()
        {
            // Configure the MessageHandler Options in terms of exception handling, number of concurrent messages to deliver etc.
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                // Maximum number of Concurrent calls to the callback `ProcessMessagesAsync`, set to 1 for simplicity.
                // Set it according to how many messages the application wants to process in parallel.
                MaxConcurrentCalls = 1,

                // Indicates whether MessagePump should automatically complete the messages after returning from User Callback.
                // False below indicates the Complete will be handled by the User Callback as in `ProcessMessagesAsync` below.
                AutoComplete = false,
            };

            // Register the function that will process messages
            _queueClient.RegisterMessageHandler(ProcessMessagesAsync, messageHandlerOptions);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            var context = args.ExceptionReceivedContext;
            _telemetryClient.TrackException(args.Exception, new Dictionary<string, string>
            {
                { "Endpoint", context.Endpoint },
                { "Entity Path", context.EntityPath },
                { "Executing Action", context.Action },
            });

            return Task.CompletedTask;
        }

        private async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(message.Body);
            var reminder = JsonConvert.DeserializeObject<DropoffReminderMessage>(json);

            // TODO
            var conversation = new ConversationReference(
                null,
                new ChannelAccount(),
                new ChannelAccount(),
                new ConversationAccount(null, null, "4d996330-07d4-11e9-a658-0fb8e400cbd0|livechat", null, null, null),
                "emulator",
                "http://localhost:1032");

            await _botFrameworkAdapter.ContinueConversationAsync(_botId, conversation, DropOffReminderCallback(), cancellationToken);

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls
            // to avoid unnecessary exceptions.
            //
            // Complete the message so that it is not received again.
            // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);

            _telemetryClient.TrackEvent(
                "Bot drop-off reminder proactive message sent.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reminder.ReservationId },
                    { "SequenceNumber", message.SystemProperties.SequenceNumber.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "Proactive message", 1 },
                });
        }

        private BotCallbackHandler DropOffReminderCallback()
        {
            return async (turnContext, cancellationToken) =>
            {
                // Send the user a proactive reminder message.
                await turnContext.SendActivityAsync("It's time to leave the key at the reception and confirm vehicle location!");
            };
        }
    }
}
