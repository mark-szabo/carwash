// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MSHU.CarWash.Bot.Dialogs.FindReservation;

namespace MSHU.CarWash.Bot.Dialogs.Auth
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

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthDialog"/> class.
        /// </summary>
        public AuthDialog() : base(nameof(AuthDialog))
        {
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
        private static async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)step.Result;
            if (tokenResponse == null)
            {
                await step.Context.SendActivityAsync("Login was not successful, please try again.", cancellationToken: cancellationToken);
                return EndOfTurn;
            }

            await step.Context.SendActivityAsync("You are now logged in.", cancellationToken: cancellationToken);

            // Display user's active reservations after login
            return await step.ReplaceDialogAsync(nameof(FindReservationDialog), tokenResponse.Token, cancellationToken: cancellationToken);
        }
    }
}
