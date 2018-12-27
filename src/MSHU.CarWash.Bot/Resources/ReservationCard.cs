using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Resources
{
    /// <summary>
    /// Reservation adaptive card for displaying a reservation in a chat visally.
    /// </summary>
    public class ReservationCard
    {
        private readonly AdaptiveCard _card;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationCard"/> class.
        /// </summary>
        /// <param name="reservation">The reservation to be rendered tha card based upon.</param>
        public ReservationCard(Reservation reservation)
        {
            var services = new List<string>();
            reservation.Services.ForEach(s => services.Add(s.ToFriendlyString()));

            _card = JsonConvert.DeserializeObject<AdaptiveCard>(File.ReadAllText(@".\Resources\reservationCard.json"));

            ((Image)_card.Body[0]).Url = $"https://carwashu.azurewebsites.net/images/state{(int)reservation.State}.png";
            ((TextBlock)((ColumnSet)((Container)_card.Body[1]).Items[0]).Columns[0].Items[0]).Text = reservation.State.ToFriendlyString();
            ((TextBlock)((ColumnSet)((Container)_card.Body[1]).Items[0]).Columns[1].Items[0]).Text = reservation.Private ? "🔒" : string.Empty;
            ((TextBlock)((Container)_card.Body[1]).Items[1]).Text = reservation.StartDate.ToString("MMMM d, h:mm tt") + reservation.EndDate?.ToString(" - h:mm tt");
            ((FactSet)((Container)_card.Body[2]).Items[0]).Facts[0].Value = reservation.VehiclePlateNumber;
            ((FactSet)((Container)_card.Body[2]).Items[0]).Facts[1].Value = reservation.Location;
            ((FactSet)((Container)_card.Body[2]).Items[0]).Facts[2].Value = string.Join(", ", services);
            ((FactSet)((Container)_card.Body[2]).Items[0]).Facts[3].Value = reservation.Comment;
            ((FactSet)((Container)_card.Body[2]).Items[0]).Facts[4].Value = reservation.CarwashComment;
            if (string.IsNullOrWhiteSpace(reservation.CarwashComment)) ((FactSet)((Container)_card.Body[2]).Items[0]).Facts.RemoveAt(4);
            if (string.IsNullOrWhiteSpace(reservation.Location)) ((FactSet)((Container)_card.Body[2]).Items[0]).Facts.RemoveAt(1);

            ((OpenUrlAction)_card.Actions.Single(a => a.Title == "Edit")).Url = $"https://carwashu.azurewebsites.net/reserve/{reservation.Id}";

            foreach (var action in _card.Actions)
            {
                if (action is SubmitAction submitAction) ((dynamic)submitAction.Data).id = reservation.Id;
            }

            // Remove Drop-off, Edit and Cancel buttons if the key was already dropped off
            switch (reservation.State)
            {
                case State.SubmittedNotActual:
                case State.ReminderSentWaitingForKey:
                    break;
                case State.DropoffAndLocationConfirmed:
                case State.WashInProgress:
                case State.NotYetPaid:
                case State.Done:
                    _card.Actions.RemoveAll(a => true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts the card to an attachment.
        /// </summary>
        /// <returns>An attachment containing the card.</returns>
        public Attachment ToAttachment()
        {
            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = _card,
            };
        }

        /// <summary>
        /// Converts the card to an attachment and places it into new a list of attachments.
        /// </summary>
        /// <returns>A list of attachments containing the one reservation card.</returns>
        public List<Attachment> ToAttachmentList()
        {
            return new List<Attachment>
            {
                new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = _card,
                },
            };
        }
    }
}
