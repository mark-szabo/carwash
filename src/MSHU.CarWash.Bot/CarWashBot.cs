// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using MSHU.CarWash.Bot.Dialogs.Auth;
using MSHU.CarWash.Bot.Dialogs.ConfirmDropoff;
using MSHU.CarWash.Bot.Dialogs.FindReservation;

namespace MSHU.CarWash.Bot
{
    /// <summary>
    /// Main entry point and orchestration for bot.
    /// </summary>
    public class CarWashBot : IBot
    {
        // Supported LUIS Intents
        public const string NewReservationIntent = "Reservation_Add";
        public const string CancelReservationIntent = "Reservation_Delete";
        public const string EditReservationIntent = "Reservation_Edit";
        public const string FindReservationIntent = "Reservation_Find";
        public const string HelpIntent = "Help";
        public const string NoneIntent = "None";

        // Card actions
        public const string DropoffAction = "dropoff";
        public const string CancelAction = "cancel";

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public const string LuisConfiguration = "carwashubot";
        public const string QnAMakerConfiguration = "carwashufaq";

        private readonly IStatePropertyAccessor<GreetingState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CarWashBot"/> class.
        /// </summary>
        /// <param name="services">Bot services.</param>
        /// <param name="userState">User state.</param>
        /// <param name="conversationState">Conversation state.</param>
        /// <param name="loggerFactory">Logger.</param>
        public CarWashBot(BotServices services, UserState userState, ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _userState = userState ?? throw new ArgumentNullException(nameof(userState));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));

            _telemetryClient = new TelemetryClient();

            // Verify QnAMaker configuration.
            if (!_services.QnAServices.ContainsKey(QnAMakerConfiguration))
            {
                throw new ArgumentException($"Invalid configuration. Please check your '.bot' file for a QnA service named '{QnAMakerConfiguration}'.");
            }

            // Verify LUIS configuration.
            if (!_services.LuisServices.ContainsKey(LuisConfiguration))
            {
                throw new InvalidOperationException($"The bot configuration does not contain a service type of `luis` with the id `{LuisConfiguration}`.");
            }

            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            Dialogs = new DialogSet(_dialogStateAccessor);

            _greetingStateAccessor = _userState.CreateProperty<GreetingState>(nameof(GreetingState));
            Dialogs.Add(new GreetingDialog(_greetingStateAccessor, loggerFactory));

            var confirmDropoffStateAccessor = _conversationState.CreateProperty<ConfirmDropoffState>(nameof(ConfirmDropoffState));
            Dialogs.Add(new ConfirmDropoffDialog(confirmDropoffStateAccessor));

            Dialogs.Add(new FindReservationDialog());

            Dialogs.Add(new AuthDialog());
            Dialogs.Add(AuthDialog.LoginPromptDialog());
        }

        private DialogSet Dialogs { get; set; }

        /// <summary>
        /// Run every turn of the conversation. Handles orchestration of messages.
        /// </summary>
        /// <param name="turnContext">Bot Turn Context.</param>
        /// <param name="cancellationToken">Task CancellationToken.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;

            // Create a dialog context
            var dc = await Dialogs.CreateContextAsync(turnContext, cancellationToken);

            switch (activity.Type)
            {
                case ActivityTypes.Message:
                    {
                        // This bot is not case sensitive.
                        var text = turnContext.Activity.Text?.ToLowerInvariant();

                        if (text == null)
                        {
                            string action = ((dynamic)turnContext.Activity.Value)?.action;
                            string id = ((dynamic)turnContext.Activity.Value)?.id;

                            switch (action)
                            {
                                case DropoffAction:
                                    await dc.BeginDialogAsync(nameof(ConfirmDropoffDialog), id, cancellationToken: cancellationToken);
                                    break;
                                case CancelAction:
                                    await dc.BeginDialogAsync(nameof(ConfirmDropoffDialog), id, cancellationToken: cancellationToken);
                                    break;
                            }

                            break;
                        }

                        if (text == "help")
                        {
                            await SendWelcomeMessageAsync(turnContext, cancellationToken);
                            break;
                        }

                        if (text == "login")
                        {
                            // Start the Login process.
                            await dc.BeginDialogAsync(nameof(AuthDialog), cancellationToken: cancellationToken);
                            break;
                        }

                        if (text == "logout")
                        {
                            // The bot adapter encapsulates the authentication processes.
                            var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                            await botAdapter.SignOutUserAsync(turnContext, AuthDialog.AuthConnectionName, cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync("You have been signed out.", cancellationToken: cancellationToken);
                            break;
                        }

                        // Perform a call to LUIS to retrieve results for the current activity message.
                        var luisResults = await _services.LuisServices[LuisConfiguration].RecognizeAsync(dc.Context, cancellationToken).ConfigureAwait(false);

                        // If any entities were updated, treat as interruption.
                        // For example, "no my name is tony" will manifest as an update of the name to be "tony".
                        var topScoringIntent = luisResults?.GetTopScoringIntent().intent;

                        // update greeting state with any entities captured
                        await UpdateGreetingState(luisResults, dc.Context);

                        // Handle conversation interrupts first.
                        var interrupted = await IsTurnInterruptedAsync(dc, topScoringIntent);
                        if (interrupted)
                        {
                            // Bypass the dialog.
                            // Save state before the next turn.
                            await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
                            await _userState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
                            return;
                        }

                        // Continue the current dialog
                        var dialogResult = await dc.ContinueDialogAsync(cancellationToken);

                        // if no one has responded,
                        if (!dc.Context.Responded)
                        {
                            // examine results from active dialog
                            switch (dialogResult.Status)
                            {
                                case DialogTurnStatus.Empty:
                                    switch (topScoringIntent)
                                    {
                                        case NewReservationIntent:
                                            await dc.BeginDialogAsync(nameof(FindReservationDialog), cancellationToken: cancellationToken);
                                            break;

                                        case EditReservationIntent:
                                            var reply = turnContext.Activity.CreateReply();
                                            reply.Attachments.Add(new HeroCard
                                            {
                                                Text = "Please open the app to make modifications to your reservations.",
                                                Buttons = new List<CardAction> { new CardAction(ActionTypes.OpenUrl, "Open the app", value: "https://carwashu.azurewebsites.net/") },
                                            }.ToAttachment());
                                            await turnContext.SendActivityAsync(reply, cancellationToken);
                                            break;

                                        case CancelReservationIntent:
                                            await dc.BeginDialogAsync(nameof(ConfirmDropoffDialog), cancellationToken: cancellationToken);
                                            break;

                                        case FindReservationIntent:
                                            await dc.BeginDialogAsync(nameof(FindReservationDialog), cancellationToken: cancellationToken);
                                            break;

                                        case NoneIntent:
                                            // Check QnA Maker model
                                            var response = await _services.QnAServices[QnAMakerConfiguration].GetAnswersAsync(turnContext);
                                            if (response != null && response.Length > 0)
                                            {
                                                await turnContext.SendActivityAsync(response[0].Answer, cancellationToken: cancellationToken);
                                            }
                                            else
                                            {
                                                await dc.Context.SendActivityAsync("I didn't understand what you just said to me.", cancellationToken: cancellationToken);
                                            }

                                            break;

                                        default:
                                            // Help or no intent identified, either way, let's provide some help to the user
                                            await dc.Context.SendActivityAsync("I didn't understand what you just said to me.", cancellationToken: cancellationToken);
                                            break;
                                    }

                                    break;

                                case DialogTurnStatus.Waiting:
                                    // The active dialog is waiting for a response from the user, so do nothing.
                                    break;

                                case DialogTurnStatus.Complete:
                                    await dc.EndDialogAsync(cancellationToken: cancellationToken);
                                    break;

                                default:
                                    await dc.CancelAllDialogsAsync(cancellationToken);
                                    break;
                            }
                        }

                        break;
                    }

                case ActivityTypes.Event:
                case ActivityTypes.Invoke:
                    // This handles the MS Teams Invoke Activity sent when magic code is not used.
                    // See: https://docs.microsoft.com/en-us/microsoftteams/platform/concepts/authentication/auth-oauth-card#getting-started-with-oauthcard-in-teams
                    // The Teams manifest schema is found here: https://docs.microsoft.com/en-us/microsoftteams/platform/resources/schema/manifest-schema
                    // It also handles the Event Activity sent from the emulator when the magic code is not used.
                    // See: https://blog.botframework.com/2018/08/28/testing-authentication-to-your-bot-using-the-bot-framework-emulator/
                    // dc = await Dialogs.CreateContextAsync(turnContext, cancellationToken);
                    await dc.ContinueDialogAsync(cancellationToken);
                    if (!turnContext.Responded)
                    {
                        await dc.BeginDialogAsync(nameof(AuthDialog), cancellationToken: cancellationToken);
                    }

                    break;

                case ActivityTypes.ConversationUpdate:
                    {
                        if (activity.MembersAdded.Any())
                        {
                            // Iterate over all new members added to the conversation.
                            foreach (var member in activity.MembersAdded)
                            {
                                // Greet anyone that was not the target (recipient) of this message.
                                if (member.Id == activity.Recipient.Id) continue;
                                await SendWelcomeMessageAsync(turnContext, cancellationToken, member.Name);
                            }
                        }

                        break;
                    }
            }

            await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
            await _userState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        // Determine if an interruption has occured before we dispatch to any active dialog.
        private static async Task<bool> IsTurnInterruptedAsync(DialogContext dc, string topIntent)
        {
            /*
            // See if there are any conversation interrupts we need to handle.
            if (topIntent.Equals(CancelIntent))
            {
                if (dc.ActiveDialog != null)
                {
                    await dc.CancelAllDialogsAsync();
                    await dc.Context.SendActivityAsync("Ok. I've cancelled our last activity.");
                }
                else
                {
                    await dc.Context.SendActivityAsync("I don't have anything to cancel.");
                }

                return true;        // Handled the interrupt.
            }
            */

            if (topIntent.Equals(HelpIntent))
            {
                await dc.Context.SendActivityAsync("Let me try to provide some help.");
                await dc.Context.SendActivityAsync("I understand greetings, being asked for help, or being asked to cancel what I am doing.");
                if (dc.ActiveDialog != null)
                {
                    await dc.RepromptDialogAsync();
                }

                return true;        // Handled the interrupt.
            }

            return false;           // Did not handle the interrupt.
        }

        /// <summary>
        /// Send the welcome message.
        /// </summary>
        /// <param name="turnContext">Provides the <see cref="ITurnContext"/> for the turn of the bot.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// <param name="name">(Optional) Name to be included in the greeting. Like "Hi Mark!".</param>
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken, string name = null)
        {
            var greeting = name == null ? "Hi!" : $"Hi {name}!";
            await turnContext.SendActivityAsync(greeting, cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("I'm your bot 🤖 who will help you reserve car washing services and answer your questions.", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("Ask me questions like 'How to use the app?' or 'What does interior cleaning cost?'.", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync(
                new Activity
                {
                    Type = ActivityTypes.Message,
                    InputHint = InputHints.AcceptingInput,
                    Text = "Or I can make reservations for you. But before that you need to log in by typing 'login'.",
                    SuggestedActions = new SuggestedActions
                    {
                        Actions = new List<CardAction>
                        {
                            new CardAction { Title = "login", Type = ActionTypes.ImBack, Value = "login" },
                            new CardAction { Title = "How to use the app?", Type = ActionTypes.ImBack, Value = "How to use the app?" },
                            new CardAction { Title = "What does interior cleaning cost?", Type = ActionTypes.ImBack, Value = "What does interior cleaning cost?" },
                        },
                    },
                },
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Helper function to update greeting state with entities returned by LUIS.
        /// </summary>
        /// <param name="luisResult">LUIS recognizer <see cref="RecognizerResult"/>.</param>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task UpdateGreetingState(RecognizerResult luisResult, ITurnContext turnContext)
        {
            if (luisResult.Entities != null && luisResult.Entities.HasValues)
            {
                // Get latest GreetingState
                var greetingState = await _greetingStateAccessor.GetAsync(turnContext, () => new GreetingState());
                var entities = luisResult.Entities;

                // Supported LUIS Entities
                string[] userNameEntities = { "userName", "userName_paternAny" };
                string[] userLocationEntities = { "userLocation", "userLocation_patternAny" };

                // Update any entities
                // Note: Consider a confirm dialog, instead of just updating.
                foreach (var name in userNameEntities)
                {
                    // Check if we found valid slot values in entities returned from LUIS.
                    if (entities[name] != null)
                    {
                        // Capitalize and set new user name.
                        var newName = (string)entities[name][0];
                        greetingState.Name = char.ToUpper(newName[0]) + newName.Substring(1);
                        break;
                    }
                }

                foreach (var city in userLocationEntities)
                {
                    if (entities[city] != null)
                    {
                        // Captilize and set new city.
                        var newCity = (string)entities[city][0];
                        greetingState.City = char.ToUpper(newCity[0]) + newCity.Substring(1);
                        break;
                    }
                }

                // Set the new values into state.
                await _greetingStateAccessor.SetAsync(turnContext, greetingState);
            }
        }
    }
}
