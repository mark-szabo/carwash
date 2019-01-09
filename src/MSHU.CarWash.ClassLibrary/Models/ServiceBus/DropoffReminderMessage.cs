using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary.Models.ServiceBus
{
    /// <summary>
    /// ServiceBus key drop-off reminder message.
    /// </summary>
    public class DropoffReminderMessage
    {
        /// <summary>
        /// Gets or sets the user id whom the reminder should be sent.
        /// </summary>
        [JsonProperty("userId")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the id of the reservation where the key should be dropped-off.
        /// </summary>
        [JsonProperty("reservationId")]
        public string ReservationId { get; set; }
    }
}
