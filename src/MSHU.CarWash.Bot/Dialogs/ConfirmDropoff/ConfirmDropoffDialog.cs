using System;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MSHU.CarWash.Bot.Dialogs.Auth;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
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
            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForBuildingStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForFloorStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForSeatStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayDropoffConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            Reservation reservation;
            try
            {
                var token = (string)step.Options;
                var api = token == null ? new CarwashService(step, cancellationToken) : new CarwashService(token);

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
