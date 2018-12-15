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
using MSHU.CarWash.Bot.Dialogs.Auth;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;

namespace MSHU.CarWash.Bot.Dialogs.ConfirmDropoff
{
    /// <summary>
    /// Reservation drop-off dialog.
    /// </summary>
    public class ConfirmDropoffDialog : ComponentDialog
    {
        // TODO: to private
        public IStatePropertyAccessor<ConfirmDropoffState> StateAccessor { get; }

        // Dialogs
        private const string Name = "confirmDropoff";
        private const string BuildingPromptName = "buildingPrompt";
        private const string FloorPromptName = "floorPrompt";

        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmDropoffDialog"/> class.
        /// </summary>
        /// <param name="stateAccessor">The <see cref="ConversationState"/> for storing properties at conversation-scope.</param>
        public ConfirmDropoffDialog(IStatePropertyAccessor<ConfirmDropoffState> stateAccessor) : base(nameof(ConfirmDropoffDialog))
        {
            StateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
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

            AddDialog(new TextPrompt(BuildingPromptName));
            AddDialog(new TextPrompt(FloorPromptName));
        }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(step.Context, () => null, cancellationToken);
            if (state == null)
            {
                if (step.Options is ConfirmDropoffState options)
                {
                    await StateAccessor.SetAsync(step.Context, options, cancellationToken);
                }
                else
                {
                    await StateAccessor.SetAsync(step.Context, new ConfirmDropoffState(), cancellationToken);
                }
            }

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> SelectReservationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);
            var reservationId = step.Options as string;

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
                await step.Context.SendActivityAsync("You are not authenticated.", cancellationToken: cancellationToken);

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

            if (reservations.Count == 1)
            {
                state.ReservationId = reservations[0].Id;
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey) == 1)
            {
                state.ReservationId = reservations.Single(r => r.State == State.ReminderSentWaitingForKey).Id;
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);

                return await step.NextAsync(cancellationToken: cancellationToken);
            }

            var activity = MessageFactory.Carousel(new ReservationCarousel(reservations).ToAttachmentList());

            await step.Context.SendActivityAsync("Please choose one reservation!", cancellationToken: cancellationToken);
            await step.Context.SendActivityAsync(activity, cancellationToken);

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForBuildingStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.ReservationId))
            {
                state.ReservationId = step.Result as string;
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            Reservation reservation;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                reservation = await api.GetReservationAsync(state.ReservationId, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync("You are not authenticated.", cancellationToken: cancellationToken);

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
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Building) && step.Result is string building)
            {
                state.Building = building.ToUpper();
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);
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
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);

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
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Floor))
            {
                state.Floor = step.Result as string;
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            var reply = step.Context.Activity.CreateReply("Which seat? If you don't know, just say skip.");
            reply.SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "skip", Type = ActionTypes.ImBack, Value = "skip" },
                },
            };
            await step.Context.SendActivityAsync(reply, cancellationToken);

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayDropoffConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(state.Seat) && step.Result is string seat && seat.ToLower() != "skip")
            {
                state.Seat = seat;
                await StateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            Reservation reservation;
            try
            {
                var api = new CarwashService(step, cancellationToken);

                // Confirm key drop-off and location
                await api.ConfirmDropoffAsync(state.ReservationId, $"{state.Building}/{state.Floor}/{state.Seat}", cancellationToken);

                // Get reservation object for displaying
                reservation = await api.GetReservationAsync(state.ReservationId, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync("You are not authenticated.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var card = new ReservationCard(reservation).DisableDropoffAction();

            var response = step.Context.Activity.CreateReply();
            response.Attachments = card.ToAttachmentList();

            await step.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);

            return await step.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
