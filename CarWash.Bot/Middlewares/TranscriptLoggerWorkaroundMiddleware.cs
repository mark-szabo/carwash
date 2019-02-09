using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace CarWash.Bot.Middlewares
{
    /// <summary>
    /// Middleware for logging incoming and outgoing activitites to an <see cref="ITranscriptStore"/>.
    ///
    /// WORKAROUND
    /// This Middleware is a slightly modified version of <see cref="TranscriptLoggerMiddleware"/>
    /// including a bugfix for a NullReferenceException.
    /// More info and pull request: https://github.com/Microsoft/botbuilder-dotnet/pull/1261.
    /// </summary>
    public class TranscriptLoggerWorkaroundMiddleware : IMiddleware
    {
        private static JsonSerializerSettings _jsonSettings = new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore };
        private ITranscriptLogger logger;

        private Queue<IActivity> transcript = new Queue<IActivity>();

        /// <summary>
        /// Initializes a new instance of the <see cref="TranscriptLoggerWorkaroundMiddleware"/> class.
        /// </summary>
        /// <param name="transcriptLogger">The conversation store to use.</param>
        public TranscriptLoggerWorkaroundMiddleware(ITranscriptLogger transcriptLogger)
        {
            logger = transcriptLogger ?? throw new ArgumentNullException("TranscriptLoggerMiddleware requires a ITranscriptLogger implementation.  ");
        }

        /// <summary>
        /// Records incoming and outgoing activities to the conversation store.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="nextTurn">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <seealso cref="ITurnContext"/>
        /// <seealso cref="IActivity"/>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate nextTurn, CancellationToken cancellationToken)
        {
            // log incoming activity at beginning of turn
            if (turnContext.Activity != null)
            {
                if (turnContext.Activity.From == null) turnContext.Activity.From = new ChannelAccount();

                if (string.IsNullOrEmpty((string)turnContext.Activity.From.Properties["role"]))
                {
                    turnContext.Activity.From.Properties["role"] = "user";
                }

                LogActivity(CloneActivity(turnContext.Activity));
            }

            // hook up onSend pipeline
            turnContext.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                // run full pipeline
                var responses = await nextSend().ConfigureAwait(false);

                foreach (var activity in activities)
                {
                    LogActivity(CloneActivity(activity));
                }

                return responses;
            });

            // hook up update activity pipeline
            turnContext.OnUpdateActivity(async (ctx, activity, nextUpdate) =>
            {
                // run full pipeline
                var response = await nextUpdate().ConfigureAwait(false);

                // add Message Update activity
                var updateActivity = CloneActivity(activity);
                updateActivity.Type = ActivityTypes.MessageUpdate;
                LogActivity(updateActivity);
                return response;
            });

            // hook up delete activity pipeline
            turnContext.OnDeleteActivity(async (ctx, reference, nextDelete) =>
            {
                // run full pipeline
                await nextDelete().ConfigureAwait(false);

                // add MessageDelete activity
                // log as MessageDelete activity
                var deleteActivity = new Activity
                {
                    Type = ActivityTypes.MessageDelete,
                    Id = reference.ActivityId,
                }
                .ApplyConversationReference(reference, isIncoming: false)
                .AsMessageDeleteActivity();

                LogActivity(deleteActivity);
            });

            // process bot logic
            await nextTurn(cancellationToken).ConfigureAwait(false);

            // flush transcript at end of turn
            while (transcript.Count > 0)
            {
                var activity = transcript.Dequeue();

                // As we are deliberately not using await, disable teh associated warning.
#pragma warning disable 4014
                logger.LogActivityAsync(activity).ContinueWith(
                    task =>
                    {
                        try
                        {
                            task.Wait();
                        }
                        catch (Exception err)
                        {
                            System.Diagnostics.Trace.TraceError($"Transcript logActivity failed with {err}");
                        }
                    },
                    cancellationToken);
#pragma warning restore 4014
            }
        }

        private static IActivity CloneActivity(IActivity activity)
        {
            activity = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity, _jsonSettings));
            return activity;
        }

        private void LogActivity(IActivity activity)
        {
            lock (transcript)
            {
                if (activity.Timestamp == null)
                {
                    activity.Timestamp = DateTime.UtcNow;
                }

                transcript.Enqueue(activity);
            }
        }
    }
}
