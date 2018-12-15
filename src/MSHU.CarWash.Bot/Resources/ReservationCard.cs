using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Resources
{
    public class ReservationCard
    {
        private AdaptiveCard _card;

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

            foreach (dynamic action in _card.Actions) action.Data.id = reservation.Id;
        }

        public ReservationCard DisableDropoffAction()
        {
            _card.Actions.Remove(_card.Actions.Find(a => a.Title == "Confirm key drop-off"));

            return this;
        }

        public Attachment ToAttachment()
        {
            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = _card,
            };
        }

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
