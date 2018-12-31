using System.Linq;
using AdaptiveCards;
using MSHU.CarWash.Bot.Services;

namespace MSHU.CarWash.Bot.Resources
{
    /// <summary>
    /// Adaptive card for reservation service selection (multiple-choice).
    /// </summary>
    internal class ServiceSelectionCard : Card
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceSelectionCard"/> class.
        /// </summary>
        /// <param name="lastSettings">Last reservation settings.</param>
        internal ServiceSelectionCard(CarwashService.LastSettings lastSettings) : base(@".\Resources\serviceSelectionCard.json")
        {
            var serviceNames = lastSettings?.Services?.Select(s => s.ToString())?.ToArray();
            if (serviceNames != null) ((ChoiceSet)_card.Body[1]).Value = string.Join(',', serviceNames);
        }
    }
}
