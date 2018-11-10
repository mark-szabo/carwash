using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace MSHU.CarWash.Bot
{
    public class TeamsAuthWorkaroundMiddleware : IMiddleware
    {
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new CancellationToken())
        {
            await next(cancellationToken);

            if (turnContext.Activity.ChannelId == "msteams")
            {
                if (turnContext.Activity.Attachments.Any() && turnContext.Activity.Attachments[0].ContentType == "application/vnd.microsoft.card.signin")
                {
                    if (turnContext.Activity.Attachments[0].Content is SigninCard card && card.Buttons is CardAction[] buttons && buttons.Any())
                    {
                        buttons[0].Type = ActionTypes.OpenUrl;
                    }
                }
            }
        }
    }
}