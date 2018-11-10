using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using MSHU.CarWash.Bot.Dialogs.Auth;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Dialogs.FindReservation
{
    /// <summary>
    /// Find reservation dialog.
    /// </summary>
    public class FindReservationDialog : ComponentDialog
    {
        // Dialogs
        private const string Name = "findReservation";

        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindReservationDialog"/> class.
        /// </summary>
        public FindReservationDialog() : base(nameof(FindReservationDialog))
        {
            _telemetryClient = new TelemetryClient();

            var dialogSteps = new WaterfallStep[]
            {
                DisplayReservationsStepAsync,
            };

            AddDialog(new WaterfallDialog(Name, dialogSteps));
            AddDialog(AuthDialog.LoginPromptDialog());
        }

        /// <summary>
        /// Fetch the token and display it for the user if they asked to see it.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private async Task<DialogTurnResult> DisplayReservationsStepAsync(WaterfallStepContext step, CancellationToken cancellationToken = default(CancellationToken))
        {
            List<Reservation> reservations;
            try
            {
                var token = (string)step.Options;
                var api = token == null ? new CarwashService(step, cancellationToken) : new CarwashService(token);
                reservations = await api.GetMyActiveReservations(cancellationToken);
            }
            catch (AuthenticationException)
            {
                await step.Context.SendActivityAsync("You have to be authenticated first.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
                await step.Context.SendActivityAsync("I am not able to access your reservations right now.", cancellationToken: cancellationToken);

                return await step.EndDialogAsync(cancellationToken: cancellationToken);
            }

            switch (reservations.Count)
            {
                case 0:
                    await step.Context.SendActivityAsync("No pending reservations. Get started by making a new reservation!", cancellationToken: cancellationToken);
                    return await step.EndDialogAsync(cancellationToken: cancellationToken);
                case 1:
                    await step.Context.SendActivityAsync("I have found one active reservation!", cancellationToken: cancellationToken);
                    break;
                default:
                    await step.Context.SendActivityAsync($"Nice! You have {reservations.Count} reservations in-progress.", cancellationToken: cancellationToken);
                    break;
            }

            foreach (var reservation in reservations)
            {
                var services = new List<string>();
                reservation.Services.ForEach(s => services.Add(s.ToFriendlyString()));

                var adaptiveCard = JsonConvert.DeserializeObject<AdaptiveCard>(File.ReadAllText(@".\Resources\reservationCard.json"));

                ((Image)adaptiveCard.Body[0]).Url = $"https://carwashu.azurewebsites.net/images/state{(int)reservation.State}.png";
                ((TextBlock)((ColumnSet)((Container)adaptiveCard.Body[1]).Items[0]).Columns[0].Items[0]).Text = reservation.State.ToFriendlyString();
                ((TextBlock)((ColumnSet)((Container)adaptiveCard.Body[1]).Items[0]).Columns[1].Items[0]).Text = reservation.Private ? "🔒" : string.Empty;
                ((TextBlock)((Container)adaptiveCard.Body[1]).Items[1]).Text = reservation.StartDate.ToString("MMMM d, h:mm tt") + reservation.EndDate?.ToString(" - h:mm tt");
                ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts[0].Value = reservation.VehiclePlateNumber;
                ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts[1].Value = reservation.Location;
                ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts[2].Value = string.Join(", ", services);
                ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts[3].Value = reservation.Comment;
                ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts[4].Value = reservation.CarwashComment;
                if (string.IsNullOrWhiteSpace(reservation.CarwashComment)) ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts.RemoveAt(4);
                if (string.IsNullOrWhiteSpace(reservation.Location)) ((FactSet)((Container)adaptiveCard.Body[2]).Items[0]).Facts.RemoveAt(1);

                foreach (dynamic action in adaptiveCard.Actions) action.Data.id = reservation.Id;

                var response = step.Context.Activity.CreateReply();
                response.Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = adaptiveCard,
                    },
                };

                await step.Context.SendActivityAsync(response, cancellationToken).ConfigureAwait(false);
            }

            return await step.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
