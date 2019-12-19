using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Resources;
using CarWash.Bot.Services;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CarWash.Bot.Dialogs
{
    /// <summary>
    /// Find reservation dialog.
    /// </summary>
    public class FindReservationDialog : Dialog
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindReservationDialog"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client.</param>
        public FindReservationDialog(TelemetryClient telemetryClient) : base(nameof(FindReservationDialog))
        {
            _telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Load reservation (or one reservation if reservation id is specified in the options) and displays it on a card.
        /// </summary>
        /// <param name="dc">A <see cref="DialogContext"/> provides context for the dialog.</param>
        /// <param name="options">(Optional) A <see cref="FindReservationDialogOptions"/> object storing the input options.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var reservations = new List<Reservation>();
            var activities = new List<IActivity>();

            try
            {
                var o = options as FindReservationDialogOptions ?? new FindReservationDialogOptions();

                var api = o.Token != null ? new CarwashService(o.Token, _telemetryClient) : new CarwashService(dc, _telemetryClient, cancellationToken);

                if (o.ReservationId != null)
                {
                    // Reservation id was predefined -> dialog was started from eg. DropoffReminder
                    reservations.Add(await api.GetReservationAsync(o.ReservationId, cancellationToken));
                }
                else
                {
                    reservations = await api.GetMyActiveReservationsAsync(cancellationToken);

                    // Chit-chat
                    switch (reservations.Count)
                    {
                        case 0:
                            await dc.Context.SendActivityAsync("No pending reservations. Get started by making a new reservation!", cancellationToken: cancellationToken);
                            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
                        case 1:
                            if (!o.DisableChitChat) activities.Add(new Activity(type: ActivityTypes.Message, text: "I have found one active reservation!"));
                            break;
                        default:
                            if (!o.DisableChitChat) activities.Add(new Activity(type: ActivityTypes.Message, text: $"Nice! You have {reservations.Count} reservations in-progress."));
                            break;
                    }
                }
            }
            catch (AuthenticationException)
            {
                await dc.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await dc.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);

                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            foreach (var reservation in reservations)
            {
                var card = new ReservationCard(reservation);

                var response = dc.Context.Activity.CreateReply();
                response.Attachments = card.ToAttachmentList();

                activities.Add(response);
            }

            await dc.Context.SendActivitiesAsync(activities.ToArray(), cancellationToken);

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Options param type for <see cref="FindReservationDialog"/>.
        /// </summary>
        public class FindReservationDialogOptions
        {
            /// <summary>
            /// Gets or sets the authentication token.
            /// </summary>
            /// <value>
            /// JWT Bearer authentication token.
            /// </value>
            public string Token { get; set; }

            /// <summary>
            /// Gets or sets the reservation id.
            /// </summary>
            /// <value>
            /// <see cref="Reservation"/> id.
            /// </value>
            public string ReservationId { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the bot should include chit-chat in its response.
            /// Like: Nice! You have 2 reservations in-progress.
            /// </summary>
            /// <value>
            /// A value indicating whether the bot should include chit-chat in its response.
            /// </value>
            public bool DisableChitChat { get; set; }
        }
    }
}
