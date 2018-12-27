using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.ClassLibrary.Models;

namespace MSHU.CarWash.Bot.Dialogs
{
    /// <summary>
    /// Find reservation dialog.
    /// </summary>
    public class FindReservationDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "findReservation";

        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindReservationDialog"/> class.
        /// </summary>
        public FindReservationDialog() : base(nameof(FindReservationDialog))
        {
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                DisplayReservationsStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
        }

        /// <summary>
        /// Fetch the token and display it for the user if they asked to see it.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private async Task<DialogTurnResult> DisplayReservationsStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<Reservation> reservations = new List<Reservation>();

            try
            {
                var options = step.Options as FindReservationDialogOptions ?? new FindReservationDialogOptions();

                var api = options.Token != null ? new CarwashService(options.Token) : new CarwashService(step, cancellationToken);

                if (options.ReservationId != null)
                {
                    // Reservation id was predefined -> dialog was started from eg. DropoffReminder
                    reservations.Add(await api.GetReservationAsync(options.ReservationId, cancellationToken));
                }
                else
                {
                    reservations = await api.GetMyActiveReservationsAsync(cancellationToken);

                    // Chit-chat
                    switch (reservations.Count)
                    {
                        case 0:
                            await step.Context.SendActivityAsync("No pending reservations. Get started by making a new reservation!", cancellationToken: cancellationToken);
                            return await step.EndDialogAsync(cancellationToken: cancellationToken);
                        case 1:
                            await step.Context.SendActivityAsync("I have found one active reservation!", cancellationToken: cancellationToken);
                            break;
                        default:
                            await step.Context.SendActivityAsync($"Nice! You have {reservations.Count} reservations in-progress.", cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync("You have to be authenticated.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            foreach (var reservation in reservations)
            {
                var card = new ReservationCard(reservation);

                var response = step.Context.Activity.CreateReply();
                response.Attachments = card.ToAttachmentList();

                await step.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);
            }

            return await step.EndDialogAsync(cancellationToken: cancellationToken);
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
        }
    }
}
