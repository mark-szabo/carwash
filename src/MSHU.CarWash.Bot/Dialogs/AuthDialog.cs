// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using MSHU.CarWash.Bot.Proactive;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.Bot.States;

namespace MSHU.CarWash.Bot.Dialogs
{
    /// <summary>
    /// User authentication.
    /// </summary>
    public class AuthDialog : ComponentDialog
    {
        /// <summary>
        /// The name of your connection. It can be found on Azure in
        /// your Bot Channels Registration on the settings blade.
        /// </summary>
        public const string AuthConnectionName = "adal";

        // Dialogs
        private const string Name = "auth";
        private const string LoginPromptName = "loginPrompt";

        private readonly IStatePropertyAccessor<UserProfile> _userProfileAccessor;
        private readonly CloudTable _table;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthDialog"/> class.
        /// </summary>
        /// <param name="userProfileAccessor">The <see cref="UserProfile"/> for storing properties at user-scope.</param>
        /// <param name="storage">Storage account.</param>
        public AuthDialog(IStatePropertyAccessor<UserProfile> userProfileAccessor, CloudStorageAccount storage) : base(nameof(AuthDialog))
        {
            _userProfileAccessor = userProfileAccessor;
            _table = storage.CreateCloudTableClient().GetTableReference(CarWashBot.UserStorageTableName);
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                PromptStepAsync,
                LoginStepAsync,
            };

            // Add the OAuth prompts and related dialogs into the dialog set
            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(LoginPromptDialog());
            AddDialog(new FindReservationDialog());
        }

        /// <summary>
        /// Gets the message to be sent out when the user is not authenticated.
        /// </summary>
        /// <value>
        /// You are not authenticated. Log in by typing 'login'.
        /// </value>
        public static Activity NotAuthenticatedMessage { get; } = new Activity
        {
            Type = ActivityTypes.Message,
            InputHint = InputHints.AcceptingInput,
            Text = "You are not authenticated. Log in by typing 'login'.",
            SuggestedActions = new SuggestedActions
            {
                Actions = new List<CardAction>
                {
                    new CardAction { Title = "login", Type = ActionTypes.ImBack, Value = "login" },
                },
            },
        };

        /// <summary>
        /// Get a token from prompt.
        /// </summary>
        /// <param name="dc">Dialog context.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Token string.</returns>
        public static async Task<string> GetToken(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Workaround: https://github.com/Microsoft/botbuilder-dotnet/pull/1243
            if (dc.Context.Activity.Text == null) dc.Context.Activity.Text = "workaround";

            var tokenResponse = await LoginPromptDialog().GetUserTokenAsync(dc.Context, cancellationToken);
            return tokenResponse?.Token;
        }

        /// <summary>
        /// Prompts the user to login using the OAuth provider specified by the connection name.
        /// </summary>
        /// <returns> An <see cref="OAuthPrompt"/> the user may use to log in.</returns>
        public static OAuthPrompt LoginPromptDialog()
        {
            return new OAuthPrompt(
                LoginPromptName,
                new OAuthPromptSettings
                {
                    ConnectionName = AuthConnectionName,
                    Text = "Click to sign in!",
                    Title = "Sign in",
                    Timeout = 300000, // User has 5 minutes to login (1000 * 60 * 5)
                });
        }

        /// <summary>
        /// This <see cref="WaterfallStep"/> prompts the user to log in using the OAuth provider specified by the connection name.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await step.BeginDialogAsync(LoginPromptName, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// In this step we check that a token was received and prompt the user as needed.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            if (!(step.Result is TokenResponse tokenResponse))
            {
                await step.Context.SendActivityAsync("Login was not successful, please try again.", cancellationToken: cancellationToken);
                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await step.Context.SendActivityAsync("You are now logged in.", cancellationToken: cancellationToken);

            // Call Carwash API and update UserProfile state
            var api = new CarwashService(tokenResponse.Token);
            var carwashUser = await api.GetMe(cancellationToken);
            var userProfile = await _userProfileAccessor.GetAsync(step.Context, () => new UserProfile());
            userProfile.CarwashUserId = carwashUser.Id;
            userProfile.NickName = carwashUser.FirstName;
            await _userProfileAccessor.SetAsync(step.Context, userProfile, cancellationToken);

            await UpdateUserInfoForProactiveMessages(step.Context, cancellationToken).ConfigureAwait(false);

            // Display user's active reservations after login
            return await step.ReplaceDialogAsync(
                nameof(FindReservationDialog),
                new FindReservationDialog.FindReservationDialogOptions { Token = tokenResponse.Token },
                cancellationToken: cancellationToken);
        }

        private async Task UpdateUserInfoForProactiveMessages(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var carwashUserId = (await _userProfileAccessor.GetAsync(turnContext, () => new UserProfile())).CarwashUserId;
                if (carwashUserId == null) return;

                var activity = turnContext.Activity;
                var user = (activity.Recipient.Role == RoleTypes.User || activity.From.Role == RoleTypes.Bot) ? activity.Recipient : activity.From;
                var bot = (activity.Recipient.Role == RoleTypes.User || activity.From.Role == RoleTypes.Bot) ? activity.From : activity.Recipient;

                var userInfo = new UserInfoEntity(
                    carwashUserId,
                    activity.ChannelId,
                    activity.ServiceUrl,
                    user,
                    bot,
                    activity.ChannelData,
                    activity.GetConversationReference());

                await _table.ExecuteAsync(TableOperation.InsertOrReplace(userInfo), null, null, cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }
        }
    }
}
