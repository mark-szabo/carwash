using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace MSHU.CarWash.Bot.Extensions
{
    public static class WaterfallStepContextExtension
    {
        public static async Task<DialogTurnResult> ClearStateAndEndDialogAsync<TState>(this WaterfallStepContext step, IStatePropertyAccessor<TState> stateAccessor,  object result = null, CancellationToken cancellationToken = default(CancellationToken))
            where TState : new()
        {
            var state = new TState();
            await stateAccessor.SetAsync(step.Context, state, cancellationToken);

            return await step.EndDialogAsync(result, cancellationToken);
        }
    }
}
