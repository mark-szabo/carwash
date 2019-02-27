using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Dialogs;
using CarWash.Bot.States;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;

namespace CarWash.Bot.Proactive
{
    /// <summary>
    /// Wash started proactive messaging.
    /// </summary>
    public class WashStartedMessage : ProactiveMessage<ReservationServiceBusMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WashStartedMessage"/> class.
        /// </summary>
        /// <param name="configuration">CarWash app configuration.</param>
        /// <param name="accessors">The state accessors for managing bot state.</param>
        /// <param name="adapterIntegration">The <see cref="BotFrameworkAdapter"/> connects the bot to the service endpoint of the given channel.</param>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="services">External services.</param>
        public WashStartedMessage(CarWashConfiguration configuration, StateAccessors accessors, IAdapterIntegration adapterIntegration, IHostingEnvironment env, BotServices services)
            : base(accessors, adapterIntegration, env, services, configuration.ServiceBusQueues.BotWashStartedQueue, new Dialog[] { AuthDialog.LoginPromptDialog(), new FindReservationDialog() })
        {
        }

        /// <inheritdoc />
        protected override IActivity[] GetActivities(DialogContext context, ReservationServiceBusMessage message, UserProfile userProfile, CancellationToken cancellationToken = default)
        {
            return new IActivity[]
                {
                    new Activity(type: ActivityTypes.Message, text: "FYI, we just started washing your car! 💦"),
                };
        }

        /// <inheritdoc />
        protected override Task BeginDialogAfterMessageAsync(DialogContext context, ReservationServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
