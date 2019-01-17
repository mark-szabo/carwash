using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace MSHU.CarWash.Bot.Extensions
{
    /// <summary>
    /// Extension class for WaterfallStepContext.
    /// </summary>
    public static class WaterfallStepContextExtension
    {
        /// <summary>
        /// Clears the dialog state and ends the dialog.
        /// </summary>
        /// <typeparam name="TState">Type of dialog state.</typeparam>
        /// <param name="step">Waterflow step.</param>
        /// <param name="stateAccessor">State accessor.</param>
        /// <param name="result">(Optional) An object to return as the result of the dialog.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="DialogTurnResult"/> object.</returns>
        public static async Task<DialogTurnResult> ClearStateAndEndDialogAsync<TState>(this WaterfallStepContext step, IStatePropertyAccessor<TState> stateAccessor, object result = null, CancellationToken cancellationToken = default(CancellationToken))
            where TState : new()
        {
            var state = new TState();
            await stateAccessor.SetAsync(step.Context, state, cancellationToken);

            return await step.EndDialogAsync(result, cancellationToken);
        }
    }
}
