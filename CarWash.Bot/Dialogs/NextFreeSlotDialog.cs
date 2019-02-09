using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Services;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace CarWash.Bot.Dialogs
{
    /// <summary>
    /// Next free slots dialog.
    /// </summary>
    public class NextFreeSlotDialog : Dialog
    {
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NextFreeSlotDialog"/> class.
        /// </summary>
        public NextFreeSlotDialog() : base(nameof(NextFreeSlotDialog))
        {
            _telemetryClient = new TelemetryClient();
        }

        /// <summary>
        /// Load and responds with the next available, free slots.
        /// </summary>
        /// <param name="dc">A <see cref="DialogContext"/> provides context for the dialog.</param>
        /// <param name="options">(Optional) An object storing the input options.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activities = new List<IActivity>();

            try
            {
                var api = new CarwashService(dc, cancellationToken);

                var freeSlots = await api.GetNextFreeSlotsAsync(cancellationToken: cancellationToken);
                var numberOfSlots = freeSlots.Count();

                // Chit-chat
                switch (numberOfSlots)
                {
                    case 0:
                        await dc.Context.SendActivityAsync("I haven't found any free slots in the next year! Crazy times... 🤦‍", cancellationToken: cancellationToken);
                        return await dc.EndDialogAsync(cancellationToken: cancellationToken);
                    case 1:
                        activities.Add(new Activity(type: ActivityTypes.Message, text: "I have found just this one:"));
                        break;
                    default:
                        activities.Add(new Activity(type: ActivityTypes.Message, text: $"Here are the next {numberOfSlots} free slots:"));
                        break;
                }

                foreach (var slot in freeSlots)
                {
                    activities.Add(new Activity(type: ActivityTypes.Message, text: slot));
                }
            }
            catch (AuthenticationException)
            {
                await dc.Context.SendActivityAsync(AuthDialog.NotAuthenticatedMessage, cancellationToken: cancellationToken);

                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await dc.Context.SendActivityAsync(e.Message, cancellationToken: cancellationToken);

                return await dc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            await dc.Context.SendActivitiesAsync(activities.ToArray(), cancellationToken);

            return await dc.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
