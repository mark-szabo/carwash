using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Extensions;
using CarWash.Bot.Services;
using CarWash.Bot.States;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace CarWash.Bot.Dialogs
{
    /// <summary>
    /// Dialog for cancelling a reservation.
    /// </summary>
    public class CancelReservationDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "cancelReservation";
        private const string ConfirmationPromptName = "confirmationPrompt";

        private readonly IStatePropertyAccessor<CancelReservationState> _stateAccessor;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelReservationDialog"/> class.
        /// </summary>
        /// <param name="stateAccessor">The <see cref="ConversationState"/> for storing properties at conversation-scope.</param>
        public CancelReservationDialog(IStatePropertyAccessor<CancelReservationState> stateAccessor) : base(nameof(CancelReservationDialog))
        {
            _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                InitializeStateStepAsync,
                SelectReservationStepAsync,
                PromptForConfirmationStepAsync,
                DisplayCancellationConfirmationStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
            AddDialog(new FindReservationDialog());

            AddDialog(new ConfirmPrompt(ConfirmationPromptName));
        }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, () => null, cancellationToken);
            if (state == null)
            {
                if (step.Options is CancelReservationState options)
                {
                    await _stateAccessor.SetAsync(step.Context, options, cancellationToken);
                }
                else
                {
                    await _stateAccessor.SetAsync(step.Context, new CancelReservationState(), cancellationToken);
                }
            }

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> SelectReservationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);
            var options = step.Options as CancelReservationDialogOptions ?? new CancelReservationDialogOptions();

            List<Reservation> reservations;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                reservations = (await api.GetMyActiveReservationsAsync(cancellationToken))
                    .Where(r => r.State == State.SubmittedNotActual || r.State == State.ReminderSentWaitingForKey)
                    .OrderBy(r => r.StartDate)
                    .ToList();
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }

            if (reservations.Count == 0)
            {
                await step.Context.SendActivityAsync("You do not have any active reservations which could be cancelled.", cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }

            if (reservations.Any(r => r.Id == options.ReservationId))
            {
                state.ReservationId = options.ReservationId;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            if (reservations.Count == 1)
            {
                state.ReservationId = reservations[0].Id;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            /*
             * var activity = MessageFactory.Carousel(new ReservationCarousel(reservations).ToAttachmentList());
             * await step.Context.SendActivityAsync("Please choose one reservation!", cancellationToken: cancellationToken);
             * await step.Context.SendActivityAsync(activity, cancellationToken);
             * return await step.NextAsync(cancellationToken: cancellationToken);
             */

            await step.Context.SendActivityAsync("I'm not sure which one. These are your active reservations, please choose one:", cancellationToken: cancellationToken);

            return await step.ReplaceDialogAsync(
                nameof(FindReservationDialog),
                new FindReservationDialog.FindReservationDialogOptions { DisableChitChat = true },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            Reservation reservation;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                reservation = await api.GetReservationAsync(state.ReservationId, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }

            return await step.PromptAsync(
                ConfirmationPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"Are you sure you want to cancel your reservation for {reservation.StartDate.ToNaturalLanguage()} for {reservation.VehiclePlateNumber}?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayCancellationConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (!(step.Result is bool confirmed && confirmed))
            {
                await step.Context.SendActivityAsync("Fine, I won't cancel that.", cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }

            try
            {
                var api = new CarwashService(step, cancellationToken);

                // Cancel the reservation
                await api.CancelReservationAsync(state.ReservationId, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);

                return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
            }

            await step.Context.SendActivityAsync("Fine, I have cancelled your reservation.", cancellationToken: cancellationToken);

            _telemetryClient.TrackEvent(
                "Reservation cancelled from chat bot.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", state.ReservationId },
                },
                new Dictionary<string, double>
                {
                    { "Reservation cancellation", 1 },
                });

            return await step.ClearStateAndEndDialogAsync(_stateAccessor, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Options param type for <see cref="CancelReservationDialog"/>.
        /// </summary>
        public class CancelReservationDialogOptions
        {
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
