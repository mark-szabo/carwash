using System;
using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;

namespace CarWash.Bot.Resources
{
    /// <summary>
    /// Reservation adaptive card for displaying a reservation in a chat visually.
    /// </summary>
    internal class ReservationCard : Card
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReservationCard"/> class.
        /// </summary>
        /// <param name="reservation">The reservation to be rendered tha card based upon.</param>
        internal ReservationCard(Reservation reservation) : base(@".\Resources\reservationCard.json")
        {
            var services = new List<string>();
            reservation.Services.ForEach(s => services.Add(s.ToFriendlyString()));

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
    }
}
