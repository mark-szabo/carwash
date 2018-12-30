using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MSHU.CarWash.Bot.CognitiveModels;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.Bot.States;
using MSHU.CarWash.ClassLibrary.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSHU.CarWash.Bot.Dialogs
{
    /// <summary>
    /// New reservation dialog.
    /// </summary>
    public class NewReservationDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "newReservation";
        private const string ServicesPromptName = "servicesPrompt";

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
                // PromptUsualStepAsync, // Can i start your usual order?
                PromptForServicesStepAsync,
                RecommendSlotsStepAsync,
                // PromptForDateStepAsync,
                // PromptForSlotStepAsync,
                // PromptForVehiclePlateNumberStepAsync,
                // PromptForPrivateSteapAsync,
                // PropmtForCommentStepAsync,
                // DisplayReservationStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
            AddDialog(new FindReservationDialog());

            AddDialog(new TextPrompt(ServicesPromptName, ValidateServices));
        }

        private async Task<DialogTurnResult> InitializeStateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, () => null, cancellationToken);
            if (state == null)
            {
                if (step.Options is NewReservationState o) state = o;
                else state = new NewReservationState();

                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            // Load LUIS entities
            var options = step.Options as NewReservationDialogOptions ?? new NewReservationDialogOptions();
            foreach (var entity in options.LuisEntities)
            {
                switch (entity.Type)
                {
                    case LuisEntityType.Service:
                        var service = (ServiceModel)entity;
                        state.Services.Add(service.Service);
                        break;
                    case LuisEntityType.DateTime:
                        var dateTime = (DateTimeModel)entity;
                        state.Timex = dateTime.Timex;
                        break;
                    case LuisEntityType.Comment:
                        state.Comment = entity.Text;
                        break;
                    case LuisEntityType.Private:
                        state.Private = true;
                        break;
                    case LuisEntityType.VehiclePlateNumber:
                        state.VehiclePlateNumber = entity.Text;
                        break;
                }
            }

            // Load last reservation settings
            if (string.IsNullOrWhiteSpace(state.VehiclePlateNumber))
            {
                try
                {
                    var api = new CarwashService(step, cancellationToken);

                    var lastSettings = await api.GetLastSettingsAsync(cancellationToken);
                    state.VehiclePlateNumber = lastSettings.VehiclePlateNumber;
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
            }

            await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForServicesStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (state.Services.Count > 0) return await step.NextAsync(cancellationToken: cancellationToken);

            var response = step.Context.Activity.CreateReply();
            response.Attachments = new ServiceSelectionCard().ToAttachmentList();

            var activities = new IActivity[]
            {
                new Activity(type: ActivityTypes.Message, text: "Please select from these services!"),
                response,
            };

            await step.Context.SendActivitiesAsync(activities, cancellationToken);

            return await step.PromptAsync(
                ServicesPromptName,
                new PromptOptions
                {
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> RecommendSlotsStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (state.Services.Count == 0)
            {
                state.Services = ParseServiceSelectionCardResponse(step.Context.Activity);
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Validator function to verify if at least one service was choosen.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateServices(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var services = ParseServiceSelectionCardResponse(promptContext.Context.Activity);

            if (services.Count > 0)
            {
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Please choose at least one service!").ConfigureAwait(false);
                return false;
            }
        }

        private List<ServiceType> ParseServiceSelectionCardResponse(Activity activity)
        {
            var servicesStringArray = JObject.FromObject(activity.Value)?["services"].ToString()?.Split(',');
            var services = new List<ServiceType>();
            if (servicesStringArray == null) return services;
            foreach (var service in servicesStringArray)
            {
                if (Enum.TryParse(service, true, out ServiceType serviceType)) services.Add(serviceType);
            }

            return services;
        }

        /// <summary>
        /// Options param type for <see cref="NewReservationDialog"/>.
        /// </summary>
        internal class NewReservationDialogOptions
        {
            /// <summary>
            /// Gets or sets the LUIS entities.
            /// </summary>
            /// <value>
            /// List of LUIS entities.
            /// </value>
            internal List<CognitiveModel> LuisEntities { get; set; }
        }
    }
}
