using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.Bot.States;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;

namespace MSHU.CarWash.Bot.Dialogs
{
    /// <summary>
    /// Reservation drop-off dialog.
    /// </summary>
    public class ConfirmDropoffDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "confirmDropoff";
        private const string BuildingPromptName = "buildingPrompt";
        private const string FloorPromptName = "floorPrompt";
        private const string SeatPromptName = "seatPrompt";

        private readonly IStatePropertyAccessor<ConfirmDropoffState> _stateAccessor;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmDropoffDialog"/> class.
        /// </summary>
        /// <param name="stateAccessor">The <see cref="ConversationState"/> for storing properties at conversation-scope.</param>
        public ConfirmDropoffDialog(IStatePropertyAccessor<ConfirmDropoffState> stateAccessor) : base(nameof(ConfirmDropoffDialog))
        {
            _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                InitializeStateStepAsync,
                SelectReservationStepAsync,
                PromptForBuildingStepAsync,
                PromptForFloorStepAsync,
                PromptForSeatStepAsync,
                DisplayDropoffConfirmationStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
            AddDialog(new FindReservationDialog());

            AddDialog(new ChoicePrompt(BuildingPromptName));
            AddDialog(new ChoicePrompt(FloorPromptName));
            AddDialog(new TextPrompt(SeatPromptName, ValidateSeat));
        }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, () => null, cancellationToken);
            if (state == null)
            {
                if (step.Options is ConfirmDropoffState options)
                {
                    await _stateAccessor.SetAsync(step.Context, options, cancellationToken);
                }
                else
                {
                    await _stateAccessor.SetAsync(step.Context, new ConfirmDropoffState(), cancellationToken);
                }
            }

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> SelectReservationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);
            var options = step.Options as ConfirmDropoffDialogOptions ?? new ConfirmDropoffDialogOptions();

            List<Reservation> reservations;
            try
            {
                // Workaround: https://github.com/Microsoft/botbuilder-dotnet/pull/1243
                step.Context.Activity.Text = "workaround";
                var api = new CarwashService(step, cancellationToken);

                reservations = (await api.GetMyActiveReservationsAsync(cancellationToken))
                    .Where(r => r.State == State.SubmittedNotActual || r.State == State.ReminderSentWaitingForKey)
                    .OrderBy(r => r.StartDate)
                    .ToList();
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (reservations.Count == 0)
            {
                await step.Context.SendActivityAsync("You do not have any active reservations.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
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

            if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey) == 1)
            {
                state.ReservationId = reservations.Single(r => r.State == State.ReminderSentWaitingForKey).Id;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            /*
             * var activity = MessageFactory.Carousel(new ReservationCarousel(reservations).ToAttachmentList());
             * await step.Context.SendActivityAsync("Please choose one reservation!", cancellationToken: cancellationToken);
             * await step.Context.SendActivityAsync(activity, cancellationToken);
             * return await step.NextAsync(cancellationToken: cancellationToken);
             */

            await step.Context.SendActivityAsync("I wasn't able to find your reservation. These are your active reservations, please choose one:", cancellationToken: cancellationToken);

            return await step.ReplaceDialogAsync(nameof(FindReservationDialog), cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForBuildingStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.ReservationId))
            {
                state.ReservationId = step.Result as string;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            Reservation reservation;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                reservation = await api.GetReservationAsync(state.ReservationId, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(reservation.Location))
            {
                var location = reservation.Location.Split('/');
                state.Building = location[0];
                state.Floor = location[1];
                state.Seat = location[2];

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            return await step.PromptAsync(
                BuildingPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("In which building did you park the car?"),
                    Choices = new List<Choice> { new Choice("M"), new Choice("S1"), new Choice("GS"), new Choice("HX") },
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForFloorStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Building) && step.Result is FoundChoice choice)
            {
                state.Building = choice.Value.ToUpper();
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(state.Floor)) return await step.NextAsync(cancellationToken: cancellationToken);

            var floors = new List<string>();
            switch (state.Building)
            {
                case "M":
                    floors = new List<string> { "-1", "-2", "-2.5", "-3", "-3.5", "outdoor" };
                    break;
                case "S1":
                    floors = new List<string> { "-1", "-2", "-3" };
                    break;
                case "GS":
                    floors = new List<string> { "-1", "outdoor" };
                    break;
                case "HX":
                    floors = new List<string> { "-3" };
                    break;
            }

            if (floors.Count == 1)
            {
                state.Floor = floors[0];
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            var floorChoiceList = new List<Choice>();
            foreach (var floor in floors)
            {
                floorChoiceList.Add(new Choice(floor));
            }

            return await step.PromptAsync(
                FloorPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("On which floor?"),
                    Choices = floorChoiceList,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForSeatStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Floor) && step.Result is FoundChoice choice)
            {
                state.Floor = choice.Value;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            var prompt = MessageFactory.Text("Which seat? If you don't know, just say skip.");
            prompt.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "skip", Type = ActionTypes.ImBack, Value = "skip" },
                },
            };

            return await step.PromptAsync(
                SeatPromptName,
                new PromptOptions
                {
                    Prompt = prompt,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayDropoffConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Seat) && step.Result is string seat && seat.ToLower() != "skip")
            {
                state.Seat = seat;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            Reservation reservation;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                // Confirm key drop-off and location
                await api.ConfirmDropoffAsync(state.ReservationId, state.Location, cancellationToken);

                // Get reservation object for displaying
                reservation = await api.GetReservationAsync(state.ReservationId, cancellationToken);

                // Reset state
                state.ReservationId = null;
                state.Building = null;
                state.Floor = null;
                state.Seat = null;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await step.Context.SendActivityAsync("OK, I have confirmed that you've dropped off the key.", cancellationToken: cancellationToken);
            await step.Context.SendActivityAsync("Thanks! 👌", cancellationToken: cancellationToken);
            await step.Context.SendActivityAsync("Here is the current state of your reservation. I'll let you know when it changes.", cancellationToken: cancellationToken);

            var response = step.Context.Activity.CreateReply();
            response.Attachments = new ReservationCard(reservation).ToAttachmentList();

            await step.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);

            return await step.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Validator function to verify if seat number is a whole number or the string "skip".
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateSeat(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Value.ToLower() == "skip") return true;

            if (int.TryParse(promptContext.Recognized.Value, out var value))
            {
                promptContext.Recognized.Value = value.ToString();
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Please enter a number.").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Options param type for <see cref="ConfirmDropoffDialog"/>.
        /// </summary>
        public class ConfirmDropoffDialogOptions
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
