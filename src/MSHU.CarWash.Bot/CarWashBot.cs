// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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

        /// <summary>
        /// Key in the bot config (.bot file) for the LUIS instance.
        /// In the .bot file, multiple instances of LUIS can be configured.
        /// </summary>
        public const string LuisConfiguration = "carwashubot";
        public const string QnAMakerConfiguration = "carwashufaq";

        /// <summary>
        /// The name of your connection. It can be found on Azure in
        /// your Bot Channels Registration on the settings blade.
        /// </summary>
        public const string AuthConnectionName = "adal";

        // Dialogs
        private const string AuthDialog = "authDialog";

        // Prompts
        private const string LoginPrompt = "loginPrompt";
        private const string DisplayTokenPrompt = "displayTokenPrompt";

        private readonly IStatePropertyAccessor<GreetingState> _greetingStateAccessor;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly BotServices _services;

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

            _greetingStateAccessor = _userState.CreateProperty<GreetingState>(nameof(GreetingState));
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

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

            Dialogs = new DialogSet(_dialogStateAccessor);
            Dialogs.Add(new GreetingDialog(_greetingStateAccessor, loggerFactory));

            // Add the OAuth prompts and related dialogs into the dialog set
            Dialogs.Add(Prompt(AuthConnectionName));
            Dialogs.Add(new ConfirmPrompt(DisplayTokenPrompt));
            Dialogs.Add(new WaterfallDialog(AuthDialog, new WaterfallStep[] { PromptStepAsync, LoginStepAsync, DisplayTokenAsync }));
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
                        var text = turnContext.Activity.Text.ToLowerInvariant();

                        if (text == "help")
                        {
                            await SendWelcomeMessageAsync(turnContext, cancellationToken);
                            break;
                        }

                        if (text == "login")
                        {
                            // Start the Login process.
                            await dc.BeginDialogAsync(AuthDialog, cancellationToken: cancellationToken);
                            break;
                        }

                        if (text == "logout")
                        {
                            // The bot adapter encapsulates the authentication processes.
                            var botAdapter = (BotFrameworkAdapter)turnContext.Adapter;
                            await botAdapter.SignOutUserAsync(turnContext, AuthConnectionName, cancellationToken: cancellationToken);
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
                                            await dc.BeginDialogAsync(nameof(GreetingDialog), cancellationToken: cancellationToken);
                                            break;

                                        case EditReservationIntent:
                                            await dc.BeginDialogAsync(nameof(GreetingDialog), cancellationToken: cancellationToken);
                                            break;

                                        case CancelReservationIntent:
                                            await dc.BeginDialogAsync(nameof(GreetingDialog), cancellationToken: cancellationToken);
                                            break;

                                        case FindReservationIntent:
                                            await dc.BeginDialogAsync(nameof(GreetingDialog), cancellationToken: cancellationToken);
                                            break;

                                        case NoneIntent:
                                            // Check QnA Maker model
                                            var response = await _services.QnAServices[QnAMakerConfiguration].GetAnswersAsync(turnContext);
                                            if (response != null && response.Length > 0)
                                            {
                                                await turnContext.SendActivityAsync(response[0].Answer, cancellationToken: cancellationToken);
                                            }

                                            break;

                                        default:
                                            // Help or no intent identified, either way, let's provide some help.
                                            // to the user
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
                        await dc.BeginDialogAsync(AuthDialog, cancellationToken: cancellationToken);
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

                                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                                //var welcomeCard = CreateAdaptiveCardAttachment();
                                //var response = CreateResponse(activity, welcomeCard);
                                //await dc.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);
                            }
                        }

                        break;
                    }
            }

            await _conversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
            await _userState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
        }

        // Determine if an interruption has occured before we dispatch to any active dialog.
        private async Task<bool> IsTurnInterruptedAsync(DialogContext dc, string topIntent)
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
            var greeting = name == null ? "Hi!" : $"Hi {name}";
            await turnContext.SendActivityAsync(greeting, cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("I'm your bot 🤖 who will help you reserve car washing services and answer your questions.", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("Ask me questions like 'How to use the app?' or 'What does interior cleaning cost?'.", cancellationToken: cancellationToken);
            await turnContext.SendActivityAsync("Or I can make reservations for you. But before that you need to log in by typing 'login'.", cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Prompts the user to login using the OAuth provider specified by the connection name.
        /// </summary>
        /// <param name="connectionName"> The name of your connection. It can be found on Azure in
        /// your Bot Channels Registration on the settings blade. </param>
        /// <returns> An <see cref="OAuthPrompt"/> the user may use to log in.</returns>
        private static OAuthPrompt Prompt(string connectionName)
        {
            return new OAuthPrompt(
                LoginPrompt,
                new OAuthPromptSettings
                {
                    ConnectionName = connectionName,
                    Text = "Click to sign in!",
                    Title = "Sign in",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                });
        }

        /// <summary>
        /// This <see cref="WaterfallStep"/> prompts the user to log in.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.BeginDialogAsync(LoginPrompt, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// In this step we check that a token was received and prompt the user as needed.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)step.Result;
            if (tokenResponse == null)
            {
                await step.Context.SendActivityAsync("Login was not successful, please try again.", cancellationToken: cancellationToken);
                return Dialog.EndOfTurn;
            }

            await step.Context.SendActivityAsync("You are now logged in.", cancellationToken: cancellationToken);
            return await step.PromptAsync(
                DisplayTokenPrompt,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Would you like to view your token?"),
                    Choices = new List<Choice> { new Choice("Yes"), new Choice("No") },
                },
                cancellationToken);
        }

        /// <summary>
        /// Fetch the token and display it for the user if they asked to see it.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            var result = (bool)step.Result;
            if (result)
            {
                // Call the prompt again because we need the token. The reasons for this are:
                // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
                // about refreshing it. We can always just call the prompt again to get the token.
                // 2. We never know how long it will take a user to respond. By the time the
                // user responds the token may have expired. The user would then be prompted to login again.
                //
                // There is no reason to store the token locally in the bot because we can always just call
                // the OAuth prompt to get the token or get a new token if needed.
                var prompt = await step.BeginDialogAsync(LoginPrompt, cancellationToken: cancellationToken);
                var tokenResponse = (TokenResponse)prompt.Result;
                if (tokenResponse != null)
                {
                    await step.Context.SendActivityAsync($"Here is your token {tokenResponse.Token}", cancellationToken: cancellationToken);
                }
            }

            return Dialog.EndOfTurn;
        }

        // Create an attachment message response.
        private static Activity CreateResponse(Activity activity, Attachment attachment)
        {
            var response = activity.CreateReply();
            response.Attachments = new List<Attachment> { attachment };
            return response;
        }

        // Load attachment from file.
        private static Attachment CreateAdaptiveCardAttachment()
        {
            var adaptiveCard = File.ReadAllText(@".\Dialogs\Welcome\Resources\welcomeCard.json");
            return new Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCard),
            };
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
