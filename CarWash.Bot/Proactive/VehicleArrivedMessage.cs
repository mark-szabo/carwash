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
    /// Vehicle arrived proactive messaging.
    /// </summary>
    public class VehicleArrivedMessage : ProactiveMessage<ReservationServiceBusMessage>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VehicleArrivedMessage"/> class.
        /// </summary>
        /// <param name="configuration">CarWash app configuration.</param>
        /// <param name="accessors">The state accessors for managing bot state.</param>
        /// <param name="adapterIntegration">The <see cref="BotFrameworkAdapter"/> connects the bot to the service endpoint of the given channel.</param>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="services">External services.</param>
        public VehicleArrivedMessage(CarWashConfiguration configuration, StateAccessors accessors, IAdapterIntegration adapterIntegration, IHostingEnvironment env, BotServices services)
            : base(accessors, adapterIntegration, env, services, configuration.ServiceBusQueues.BotVehicleArrivedNotificationQueue, new Dialog[] { AuthDialog.LoginPromptDialog(), new FindReservationDialog() })
        {
        }

        /// <inheritdoc />
        protected override IActivity[] GetActivities(DialogContext context, ReservationServiceBusMessage message, UserProfile userProfile, CancellationToken cancellationToken = default)
        {
            var greeting = userProfile?.NickName == null ? "Welcome in the office! 🙋‍" : $"Welcome {userProfile.NickName} in the office! 🙋‍";

            return new IActivity[]
                {
                    new Activity(type: ActivityTypes.Message, text: greeting),
                    new Activity(type: ActivityTypes.Message, text: "I just noticed that you've arrived! 👀"),
                    new Activity(type: ActivityTypes.Message, text: "Don't forget to leave the key at the reception."),
                    new Activity(type: ActivityTypes.Message, text: "And please don't forget to confirm the vehicle location! You can do it here by clicking the 'Confirm key drop-off' button below."),
                };
        }

        /// <inheritdoc />
        protected override Task BeginDialogAfterMessageAsync(DialogContext context, ReservationServiceBusMessage message, CancellationToken cancellationToken = default)
        {
            return context.BeginDialogAsync(
                nameof(FindReservationDialog),
                new FindReservationDialog.FindReservationDialogOptions { ReservationId = message.ReservationId },
                cancellationToken: cancellationToken);
        }
    }
}
