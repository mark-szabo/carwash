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
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using MSHU.CarWash.Bot.CognitiveModels;
using MSHU.CarWash.Bot.Resources;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.Bot.States;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Extensions;
using Newtonsoft.Json.Linq;
using static MSHU.CarWash.ClassLibrary.Constants;

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
        private const string RecommendedSlotsPromptName = "recommendedSlotsPrompt";
        private const string DatePromptName = "datePrompt";
        private const string SlotPromptName = "slotPrompt";
        private const string VehiclePlateNumberConfirmationPromptName = "vehiclePlateNumberConfirmationPrompt";
        private const string VehiclePlateNumberPromptName = "vehiclePlateNumberPrompt";
        private const string PrivatePromptName = "privatePrompt";

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
                // PromptUsualStepAsync, // Can I start your usual order?
                PromptForServicesStepAsync,
                RecommendSlotsStepAsync,
                PromptForDateStepAsync,
                PromptForSlotStepAsync,
                PromptForVehiclePlateNumberConfirmationStepAsync,
                PromptForVehiclePlateNumberStepAsync,
                PromptForPrivateStepAsync,
                // PropmtForCommentStepAsync,
                // DisplayReservationStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
            AddDialog(new FindReservationDialog());

            AddDialog(new TextPrompt(ServicesPromptName, ValidateServices));
            AddDialog(new ChoicePrompt(RecommendedSlotsPromptName, ValidateRecommendedSlots));
            AddDialog(new DateTimePrompt(DatePromptName, ValidateDate));
            AddDialog(new ChoicePrompt(SlotPromptName));
            AddDialog(new ConfirmPrompt(VehiclePlateNumberConfirmationPromptName));
            AddDialog(new TextPrompt(VehiclePlateNumberPromptName, ValidateVehiclePlateNumber));
            AddDialog(new ConfirmPrompt(PrivatePromptName));
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

                        if (dateTime.Timex.Year != null &&
                            dateTime.Timex.Month != null &&
                            dateTime.Timex.DayOfMonth != null)
                        {
                            var hour = dateTime.Timex.Hour ?? 0;
                            state.StartDate = new DateTime(
                                dateTime.Timex.Year.Value,
                                dateTime.Timex.Month.Value,
                                dateTime.Timex.DayOfMonth.Value,
                                dateTime.Timex.Hour.Value,
                                0,
                                0);
                        }

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

                    state.LastSettings = await api.GetLastSettingsAsync(cancellationToken);
                }
                catch (AuthenticationException)
                {
                    await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                    return await step.EndDialogAsync(cancellationToken: cancellationToken);
                }
                catch (Exception e)
                {
                    _telemetryClient.TrackException(e);
                    await step.Context.SendActivityAsync("I am not able to access the CarWash app right now.", cancellationToken: cancellationToken);

                    return await step.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }

            await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

            return await step.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForServicesStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            // Check whether we already know the services.
            if (state.Services.Count > 0) return await step.NextAsync(cancellationToken: cancellationToken);

            var response = step.Context.Activity.CreateReply();
            response.Attachments = new ServiceSelectionCard(state.LastSettings).ToAttachmentList();

            var activities = new IActivity[]
            {
                new Activity(type: ActivityTypes.Message, text: "Please select from these services!"),
                response,
            };
            await step.Context.SendActivitiesAsync(activities, cancellationToken);

            return await step.PromptAsync(
                ServicesPromptName,
                new PromptOptions { },
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

            // Check whether we already know something about the date.
            if (state.StartDate != null) return await step.NextAsync(cancellationToken: cancellationToken);

            var recommendedSlots = new List<DateTime>();

            try
            {
                var api = new CarwashService(step, cancellationToken);

                var notAvailable = await api.GetNotAvailableDatesAndTimesAsync(cancellationToken);
                recommendedSlots = GetRecommendedSlots(notAvailable);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access the CarWash app right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // Save recommendations to state
            state.RecommendedSlots = recommendedSlots;
            await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

            var choices = new List<Choice>();

            foreach (var slot in recommendedSlots)
            {
                var timex = TimexProperty.FromDateTime(slot);
                choices.Add(new Choice(timex.ToNaturalLanguage(DateTime.Now)));
            }

            return await step.PromptAsync(
                RecommendedSlotsPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Can I recommend you one of these slots? If you want to choose something else, just say skip."),
                    Choices = choices,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForDateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (state.StartDate == null && step.Result is FoundChoice choice)
            {
                state.StartDate = state.RecommendedSlots[choice.Index];
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            // Check whether we already know something about the date.
            if (state.StartDate != null) return await step.NextAsync(cancellationToken: cancellationToken);

            return await step.PromptAsync(
                DatePromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("When do you want to wash your car?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForSlotStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (state.StartDate == null && step.Result is IList<DateTimeResolution> resolution)
            {
                var timex = new TimexProperty(resolution[0].Timex);
                var hour = timex.Hour ?? 0;
                state.StartDate = new DateTime(
                    timex.Year.Value,
                    timex.Month.Value,
                    timex.DayOfMonth.Value,
                    hour,
                    0,
                    0);
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            var reservationCapacity = new List<CarwashService.ReservationCapacity>();
            try
            {
                var api = new CarwashService(step, cancellationToken);

                reservationCapacity = (List<CarwashService.ReservationCapacity>)await api.GetReservationCapacityAsync(state.StartDate, cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access the CarWash app right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // Check whether we already know the slot.
            if (Slots.Any(s => s.StartTime == state.StartDate.Hour))
            {
                // Check if slot is available.
                if (!reservationCapacity.Any(c => c.StartTime == state.StartDate && c.FreeCapacity > 0))
                {
                    await step.Context.SendActivityAsync("Sorry, this slot is already full.", cancellationToken: cancellationToken);
                }
                else
                {
                    return await step.NextAsync(cancellationToken: cancellationToken);
                }
            }

            var choices = new List<Choice>();
            state.SlotChoices = new List<DateTime>();
            foreach (var slot in reservationCapacity)
            {
                var timex = TimexProperty.FromDateTime(slot.StartTime);
                choices.Add(new Choice($"{timex.ToNaturalLanguage(DateTime.Now)} ({slot.StartTime.Hour}-{Slots.Single(s => s.StartTime == slot.StartTime.Hour).EndTime})"));

                // Save recommendations to state
                state.SlotChoices.Add(slot.StartTime);
            }

            await _stateAccessor.SetAsync(step.Context, state, cancellationToken);

            return await step.PromptAsync(
                SlotPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Please choose one of these slots:"),
                    Choices = choices,
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForVehiclePlateNumberConfirmationStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (Slots.Any(s => s.StartTime == state.StartDate.Hour) && step.Result is FoundChoice choice)
            {
                state.StartDate = state.SlotChoices[choice.Index];
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            // Check whether we don't know the vehicle plate number.
            if (state.VehiclePlateNumber == null) return await step.NextAsync(cancellationToken: cancellationToken);

            return await step.PromptAsync(
                VehiclePlateNumberConfirmationPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"I have {state.VehiclePlateNumber} as you plate number, is that correct?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForVehiclePlateNumberStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (state.VehiclePlateNumber != null && step.Result is bool confirmed) return await step.NextAsync(cancellationToken: cancellationToken);

            return await step.PromptAsync(
                VehiclePlateNumberPromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text($"I have {state.VehiclePlateNumber} as you plate number, is that correct?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForPrivateStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(step.Context, cancellationToken: cancellationToken);

            if (step.Result is string plate)
            {
                state.VehiclePlateNumber = plate;
                await _stateAccessor.SetAsync(step.Context, state, cancellationToken);
            }

            // TODO: Check from API, whether the car is private or not by plate number.
            return await step.PromptAsync(
                PrivatePromptName,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Is this your private car?"),
                },
                cancellationToken);
        }

        /// <summary>
        /// Validator function to verify if at least one service was chosen.
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

        /// <summary>
        /// Validator function to verify if a recommendation was chosen or the user wants to skip the step.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateRecommendedSlots(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Context.Activity.Text.ToLower() == "skip") return true;

            if (promptContext.Recognized.Succeeded)
            {
                // var dateTime = DateTimeRecognizer.RecognizeDateTime(promptContext.Recognized.Value.Value, Culture.English, DateTimeOptions.None, DateTime.Now);
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Please choose one of the options or say skip!").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Validator function to verify if the date is valid.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateDate(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            var timex = new TimexProperty(promptContext.Recognized.Value?[0]?.Timex);

            if (promptContext.Recognized.Succeeded &&
                timex?.Year != null &&
                timex?.Month != null &&
                timex?.DayOfMonth != null)
            {
                var hour = timex.Hour ?? 0;
                var date = new DateTime(
                    timex.Year.Value,
                    timex.Month.Value,
                    timex.DayOfMonth.Value,
                    hour,
                    0,
                    0);

                if (date.Subtract(DateTime.Today).Days > 365)
                {
                    await promptContext.Context.SendActivityAsync($"Sorry, you cannot reserve for more than 365 days in the future.").ConfigureAwait(false);
                    return false;
                }

                if (date < DateTime.Today)
                {
                    await promptContext.Context.SendActivityAsync($"Sorry, you cannot reserve in the past.").ConfigureAwait(false);
                    return false;
                }

                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"Sorry, I wasn't able to get the date. Can you please rephrase it?").ConfigureAwait(false);
                return false;
            }
        }

        /// <summary>
        /// Validator function to verify if vehicle plate number meets required constraints.
        /// </summary>
        /// <param name="promptContext">Context for this prompt.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        private async Task<bool> ValidateVehiclePlateNumber(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var plate = promptContext.Recognized.Value?.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty) ?? string.Empty;

            if (plate.Length == 6)
            {
                promptContext.Recognized.Value = plate;
                return true;
            }
            else
            {
                await promptContext.Context.SendActivityAsync($"This does not seem to be a vehicle plate number...").ConfigureAwait(false);
                return false;
            }
        }

        private List<ServiceType> ParseServiceSelectionCardResponse(Activity activity)
        {
            var servicesStringArray = JObject.FromObject(activity.Value)?["services"]?.ToString()?.Split(',');
            var services = new List<ServiceType>();
            if (servicesStringArray == null) return services;
            foreach (var service in servicesStringArray)
            {
                if (Enum.TryParse(service, true, out ServiceType serviceType)) services.Add(serviceType);
            }

            return services;
        }

        private List<DateTime> GetRecommendedSlots(CarwashService.NotAvailableDatesAndTimes notAvailable)
        {
            var recommendedSlots = new List<DateTime>();

            // Try to find a slot today.
            var isOpenSlotToday = !notAvailable.Dates.Any(d => d == DateTime.Today);
            if (DateTime.Today.IsWeekend()) isOpenSlotToday = false;

            if (isOpenSlotToday)
            {
                try
                {
                    recommendedSlots.Add(FindOpenSlot(DateTime.Today));
                }
                catch (Exception e)
                {
                    _telemetryClient.TrackException(e);
                }
            }

            // Find the next next nearest slot (excluding today).
            var nextDayWithOpenSlots = DateTime.Today.AddDays(1);
            while (notAvailable.Dates.Contains(nextDayWithOpenSlots) || nextDayWithOpenSlots.IsWeekend()) nextDayWithOpenSlots = nextDayWithOpenSlots.AddDays(1);

            try
            {
                recommendedSlots.Add(FindOpenSlot(nextDayWithOpenSlots));
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }

            // Find a slot next week.
            var nextWeek = GetNextWeekday(DayOfWeek.Monday);
            while (notAvailable.Dates.Contains(nextWeek) || nextWeek.IsWeekend()) nextWeek = nextWeek.AddDays(1);

            try
            {
                recommendedSlots.Add(FindOpenSlot(nextWeek));
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }

            return recommendedSlots;

            DateTime FindOpenSlot(DateTime date)
            {
                foreach (var slot in Slots)
                {
                    if (!notAvailable.Times.Any(t => t.Date == date.Date && t.Hour == slot.StartTime))
                    {
                        return new DateTime(date.Year, date.Month, date.Day, slot.StartTime, 0, 0);
                    }
                }

                throw new ArgumentException($"No open slot found on the given date. The API should have specified this date in the not available dates array. Date: {date.ToShortDateString()}");
            }

            DateTime GetNextWeekday(DayOfWeek day)
            {
                DateTime result = DateTime.Today.AddDays(1);
                while (result.DayOfWeek != day) result = result.AddDays(1);

                return result;
            }
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
