using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Extensions;
using CarWash.Bot.States;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.ServiceBus;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace CarWash.Bot.Proactive
{
    /// <summary>
    /// Proactive messaging service.
    /// </summary>
    /// <typeparam name="T">Type the incoming Service Bus message should be serialized to.</typeparam>
    public abstract class ProactiveMessage<T>
        where T : ServiceBusMessage
    {
        private readonly QueueClient _queueClient;
        private readonly CloudTable _table;
        private readonly EndpointService _endpoint;
        private readonly StateAccessors _accessors;
        private readonly BotFrameworkAdapter _botFrameworkAdapter;
        private readonly DialogSet _dialogs;
        private readonly IHostingEnvironment _env;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProactiveMessage{T}"/> class.
        /// </summary>
        /// <param name="accessors">The state accessors for managing bot state.</param>
        /// <param name="adapterIntegration">The <see cref="BotFrameworkAdapter"/> connects the bot to the service endpoint of the given channel.</param>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="services">External services.</param>
        /// <param name="queueName">Service Bus queue name.</param>
        /// <param name="dialogs">List of Types of other <see cref="Dialog"/>s used when sending out the proactive message.</param>
        /// <param name="telemetryClient">Telemetry client.</param>
        public ProactiveMessage(StateAccessors accessors, IAdapterIntegration adapterIntegration, IHostingEnvironment env, BotServices services, string queueName, Dialog[] dialogs, TelemetryClient telemetryClient)
        {
            _accessors = accessors;
            _env = env;
            _botFrameworkAdapter = (BotFrameworkAdapter)adapterIntegration;
            _telemetryClient = telemetryClient;

            _dialogs = new DialogSet(_accessors.DialogStateAccessor);
            foreach (var dialog in dialogs) _dialogs.Add(dialog);

            // Verify Endpoint configuration.
            var endpointConfig = env.IsProduction() ? CarWashBot.EndpointConfiguration : CarWashBot.EndpointConfigurationDev;
            if (!services.EndpointServices.ContainsKey(endpointConfig))
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a Endpoint service named '{endpointConfig}'.");

            _endpoint = services.EndpointServices[endpointConfig];

            // Verify Storage configuration.
            if (!services.StorageServices.ContainsKey(CarWashBot.StorageConfiguration))
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a Storage service named '{CarWashBot.StorageConfiguration}'.");

            var tableClient = services.StorageServices[CarWashBot.StorageConfiguration].CreateCloudTableClient();
            _table = tableClient.GetTableReference(CarWashBot.UserStorageTableName);

            // Verify ServiceBus configuration.
            if (!services.ServiceBusServices.ContainsKey(CarWashBot.ServiceBusConfiguration))
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a Service Bus service named '{CarWashBot.ServiceBusConfiguration}'.");

            _queueClient = new QueueClient(services.ServiceBusServices[CarWashBot.ServiceBusConfiguration], queueName, ReceiveMode.PeekLock, null);
        }

        /// <summary>
        /// Register Service Bus message handler.
        /// This will trigger <see cref="ProcessMessagesAsync(Message, CancellationToken)"/> when a new message is inserted into the queue.
        /// </summary>
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

        /// <summary>
        /// Get activities to be sent out as proactive messages in a new conversation.
        /// </summary>
        /// <param name="context">Dialog context.</param>
        /// <param name="message">Serialized Service Bus message.</param>
        /// <param name="userProfile">User profile from state.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>An array of activities to be sent out.</returns>
        protected abstract IActivity[] GetActivities(DialogContext context, T message, UserProfile userProfile, CancellationToken cancellationToken = default);

        /// <summary>
        /// Begin new dialogs if needed.
        /// This gets called after the proactive messages were successfully sent out.
        /// </summary>
        /// <param name="context">Dialog context.</param>
        /// <param name="message">Serialized Service Bus message.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Task.</returns>
        protected abstract Task BeginDialogAfterMessageAsync(DialogContext context, T message, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Process incoming Service Bus messages, and create a new conversation with the user.
        /// This will call <see cref="MessageCallback(T, UserInfoEntity)"/> when teh new conversation was successfuly created.
        /// </summary>
        /// <param name="message">Service Bus message object.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Task.</returns>
        private async Task ProcessMessagesAsync(Message message, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(message.Body);
            var parsedMessage = JsonConvert.DeserializeObject<T>(json);

            var logProperties = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            logProperties.Add("ProactiveMessageType", GetType().Name);
            logProperties.Add("ServiceBusSequenceNumber", message.SystemProperties.SequenceNumber.ToString());

            var userInfos = await _table.RetrieveUserInfoAsync(parsedMessage.UserId);

            if (userInfos == null || userInfos.Count == 0)
            {
                await _queueClient.CompleteAsync(message.SystemProperties.LockToken);

                _telemetryClient.TrackEvent(
                    "Failed to send bot proactive message: User was not found in the bot's database.",
                    logProperties);
            }

            if (!_env.IsProduction()) userInfos = userInfos.Where(i => i.ChannelId == ChannelIds.Emulator).ToList();

            foreach (var userInfo in userInfos)
            {
                try
                {
                    await _botFrameworkAdapter.CreateConversationAsync(
                        userInfo.ChannelId,
                        userInfo.ServiceUrl,
                        new MicrosoftAppCredentials(_endpoint.AppId, _endpoint.AppPassword),
                        new ConversationParameters(bot: userInfo.Bot, members: new List<ChannelAccount> { userInfo.User }, channelData: userInfo.ChannelData),
                        MessageCallback(parsedMessage, userInfo),
                        cancellationToken);

                    // Same with an existing conversation
                    // var conversation = new ConversationReference(
                    //   null,
                    //   userInfo.User,
                    //   userInfo.Bot,
                    //   new ConversationAccount(null, null, userInfo.CurrentConversation.Conversation.Id, null, null, null),
                    //   userInfo.ChannelId,
                    //   userInfo.ServiceUrl);
                    // await _botFrameworkAdapter.ContinueConversationAsync(_endpoint.AppId, conversation, MessageCallback(), cancellationToken);
                }
                catch (ErrorResponseException e)
                {
                    _telemetryClient.TrackException(
                        e,
                        new Dictionary<string, string>
                        {
                            { "Error message", e.Message },
                            { "Response body", e.Response.Content },
                            { "Request body", e.Request.Content },
                            { "Request uri", $"{e.Request.Method.ToString()} {e.Request.RequestUri.AbsoluteUri}" },
                            { "Request headers", e.Request.Headers.ToJson() },
                        });
                }
                catch (Exception e)
                {
                    _telemetryClient.TrackException(e);
                }
            }

            // Note: Use the cancellationToken passed as necessary to determine if the queueClient has already been closed.
            // If queueClient has already been Closed, you may chose to not call CompleteAsync() or AbandonAsync() etc. calls
            // to avoid unnecessary exceptions.
            //
            // Complete the message so that it is not received again.
            // This can be done only if the queueClient is created in ReceiveMode.PeekLock mode (which is default).
            await _queueClient.CompleteAsync(message.SystemProperties.LockToken);

            _telemetryClient.TrackEvent(
                "Proactive message sent by bot.",
                logProperties,
                new Dictionary<string, double>
                {
                    { "Proactive message", 1 },
                    { $"Proactive{GetType().Name}", 1 },
                });
        }

        /// <summary>
        /// Sends out the activities from <see cref="GetActivities(DialogContext, T, UserProfile, CancellationToken)"/>
        /// and calls <see cref="BeginDialogAfterMessageAsync(DialogContext, T, CancellationToken)"/> to begin new dialogs if needed.
        /// </summary>
        /// <param name="message">Serialized Service Bus message.</param>
        /// <param name="userInfo">User information from Storage Tables.</param>
        /// <returns>A callback handler.</returns>
        private BotCallbackHandler MessageCallback(T message, UserInfoEntity userInfo) =>
            async (turnContext, cancellationToken) =>
            {
                try
                {
                    // Set the From object for the state accessor (CreateConversationAsync() does not fill the Activity.From object)
                    turnContext.Activity.From = userInfo.User;

                    var profile = await _accessors.UserProfileAccessor.GetAsync(turnContext, () => null, cancellationToken);

                    // Create a dialog context
                    var dc = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

                    var activities = GetActivities(dc, message, profile, cancellationToken);

                    // Send the user a proactive reminder message.
                    await turnContext.SendActivitiesAsync(activities, cancellationToken);

                    // Start a dialog after proactive message was sent to user
                    await BeginDialogAfterMessageAsync(dc, message, cancellationToken);
                }
                catch (Exception e)
                {
                    var logProperties = new Dictionary<string, string>
                    {
                        { "Activity", turnContext?.Activity?.ToJson() ?? "No activity." },
                    };
                    if (message is ReservationServiceBusMessage m) logProperties.Add("Reservation ID", m.ReservationId ?? "Reservation ID missing.");

                    _telemetryClient.TrackException(e, logProperties);
                }
            };
    }
}
