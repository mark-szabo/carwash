namespace MSHU.CarWash.Bot.Resources
{
    /// <summary>
    /// Adaptive card for reservation service selection (multiple-choice).
    /// </summary>
    public class ServiceSelectionCard : Card
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceSelectionCard"/> class.
        /// </summary>
        public ServiceSelectionCard() : base(@".\Resources\serviceSelectionCard.json")
        {
        }
    }
}
