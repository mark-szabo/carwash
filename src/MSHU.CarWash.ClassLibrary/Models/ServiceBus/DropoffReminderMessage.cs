using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary.Models.ServiceBus
{
    public class DropoffReminderMessage
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("reservationId")]
        public string ReservationId { get; set; }
    }
}
