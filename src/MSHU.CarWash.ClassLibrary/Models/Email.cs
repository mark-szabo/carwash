using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Storage queue email message.
    /// </summary>
    public class Email
    {
        /// <summary>
        /// Gets or sets the recipient of the email.
        /// </summary>
        [JsonProperty("to")]
        public string To { get; set; }

        /// <summary>
        /// Gets or sets the subject of the email.
        /// </summary>
        [JsonProperty("subject")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the text (body) of the email.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }
    }
}
