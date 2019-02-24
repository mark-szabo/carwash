using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Dialogs;
using CarWash.Bot.Services;
using CarWash.Bot.States;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ServiceBus;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Schema;

namespace CarWash.Bot.Proactive
{
    /// <summary>
    /// Wash completed proactive messaging.
    /// </summary>
    public class WashCompletedMessage : ProactiveMessage<ReservationServiceBusMessage>
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="WashCompletedMessage"/> class.
        /// </summary>
        /// <param name="accessors">The state accessors for managing bot state.</param>
        /// <param name="adapterIntegration">The <see cref="BotFrameworkAdapter"/> connects the bot to the service endpoint of the given channel.</param>
        /// <param name="env">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="services">External services.</param>
        public WashCompletedMessage(StateAccessors accessors, IAdapterIntegration adapterIntegration, IHostingEnvironment env, BotServices services)
            : base(accessors, adapterIntegration, env, services, new Dialog[] { AuthDialog.LoginPromptDialog(), new FindReservationDialog() })
        {
            _telemetryClient = new TelemetryClient();
        }

        /// <inheritdoc />
        protected override string ServiceBusQueueName { get => "bot-wash-completed"; }

        /// <inheritdoc />
        protected override string ServiceBusQueueNameDev { get => "bot-wash-completed-dev"; }

        /// <inheritdoc />
        protected override IActivity[] GetActivities(DialogContext context, ReservationServiceBusMessage message, UserProfile userProfile, CancellationToken cancellationToken = default)
        {
            Reservation reservation = null;
            try
            {
                var api = new CarwashService(context, cancellationToken);
                reservation = api.GetReservationAsync(message.ReservationId, cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }

            var greeting = userProfile?.NickName == null ? "Hi!" : $"Hi {userProfile.NickName}!";

            var activities = new List<IActivity>
                {
                    new Activity(type: ActivityTypes.Message, text: greeting),
                    new Activity(type: ActivityTypes.Message, text: reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!"),
                };

            if (reservation != null) activities.Add(new Activity(type: ActivityTypes.Message, text: $"You can find it here: {reservation.Location}"));

            return activities.ToArray();
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
