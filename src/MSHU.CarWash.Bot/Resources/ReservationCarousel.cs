using System.Collections.Generic;
using Microsoft.Bot.Schema;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;

namespace MSHU.CarWash.Bot.Resources
{
    public class ReservationCarousel
    {
        private readonly List<ThumbnailCard> _cards = new List<ThumbnailCard>();

        public ReservationCarousel(IEnumerable<Reservation> reservations)
        {
            foreach (var reservation in reservations)
            {
                var services = new List<string>();
                reservation.Services.ForEach(s => services.Add(s.ToFriendlyString()));

                _cards.Add(new ThumbnailCard
                {
                    Title = reservation.VehiclePlateNumber,
                    Subtitle = reservation.StartDate.ToString("MMMM d, h:mm tt") + reservation.EndDate?.ToString(" - h:mm tt"),
                    Text = string.Join(", ", services),
                    Images = new List<CardImage> { new CardImage($"https://carwashu.azurewebsites.net/images/state{(int)reservation.State}.png") },
                    Buttons = new List<CardAction> { new CardAction(ActionTypes.PostBack, "This one", value: reservation.Id) },
                });
            }
        }

        public List<Attachment> ToAttachmentList()
        {
            var attachments = new List<Attachment>();

            foreach (var card in _cards)
            {
                attachments.Add(card.ToAttachment());
            }

            return attachments;
        }
    }
}
