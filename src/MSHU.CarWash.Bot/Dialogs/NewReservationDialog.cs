using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MSHU.CarWash.Bot.States;

namespace MSHU.CarWash.Bot.Dialogs
{
    /// <summary>
    /// New reservation dialog.
    /// </summary>
    public class NewReservationDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "newReservation";

        private readonly IStatePropertyAccessor<NewReservationState> _stateAccessor;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewReservationDialog"/> class.
        /// </summary>
        /// <param name="stateAccessor">The <see cref="ConversationState"/> for storing properties at conversation-scope.</param>
        public NewReservationDialog(IStatePropertyAccessor<NewReservationState> stateAccessor) : base(nameof(NewReservationDialog))
        {
            _stateAccessor = stateAccessor ?? throw new ArgumentNullException(nameof(stateAccessor));
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                InitializeStateStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
            AddDialog(new FindReservationDialog());
        }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, () => null, cancellationToken);
            if (state == null)
            {
                if (step.Options is NewReservationState options)
                {
                    await _stateAccessor.SetAsync(step.Context, options, cancellationToken);
                }
                else
                {
                    await _stateAccessor.SetAsync(step.Context, new NewReservationState(), cancellationToken);
                }
            }

            return await step.NextAsync(cancellationToken: cancellationToken);
        }
    }
}
