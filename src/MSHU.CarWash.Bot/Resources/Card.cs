using System.Collections.Generic;
using System.IO;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Resources
{
    /// <summary>
    /// Adaptive card for displaying something in a chat visually.
    /// </summary>
    public class Card
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Card"/> class.
        /// </summary>
        /// <param name="path">Path to the card's json file.</param>
        public Card(string path)
        {
            card = JsonConvert.DeserializeObject<AdaptiveCard>(File.ReadAllText(path));
        }

#pragma warning disable IDE1006, SA1300, CS1591
        protected AdaptiveCard card { get; }
#pragma warning restore IDE1006, SA1300, CS1591

        /// <summary>
        /// Converts the card to an attachment.
        /// </summary>
        /// <returns>An attachment containing the card.</returns>
        public Attachment ToAttachment()
        {
            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }

        /// <summary>
        /// Converts the card to an attachment and places it into new a list of attachments.
        /// </summary>
        /// <returns>A list of attachments containing the only one card.</returns>
        public List<Attachment> ToAttachmentList()
        {
            return new List<Attachment>
            {
                new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card,
                },
            };
        }
    }
}
