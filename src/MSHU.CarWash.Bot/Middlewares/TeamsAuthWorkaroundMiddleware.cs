using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MSHU.CarWash.Bot.Middlewares
{
    /// <summary>
    /// WORKAROUND
    ///
    /// Middleware wich will change the sign-in button's type on the Teams channel only to be able to sign in if the user opened the bot from a link.
    /// More info: https://github.com/Microsoft/BotBuilder/issues/4768#issuecomment-437669120.
    /// </summary>
    public class TeamsAuthWorkaroundMiddleware : IMiddleware
    {
        /// <summary>
        /// This gets called on the beginning of the turn.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="next">Next delegate in the middleware pipeline.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new CancellationToken())
        {
            // hook up onSend pipeline
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                foreach (var activity in activities)
                {
                    if (activity.ChannelId != ChannelIds.Msteams) continue;
                    if (activity.Attachments == null) continue;
                    if (!activity.Attachments.Any()) continue;
                    if (activity.Attachments[0].ContentType != "application/vnd.microsoft.card.signin") continue;
                    if (!(activity.Attachments[0].Content is SigninCard card)) continue;
                    if (!(card.Buttons is CardAction[] buttons)) continue;
                    if (!buttons.Any()) continue;

                    // Modify button type to openUrl as signIn is not working in teams
                    buttons[0].Type = ActionTypes.OpenUrl;
                }

                // run full pipeline
                return await nextSend().ConfigureAwait(false);
            });

            await next(cancellationToken);
        }
    }
}