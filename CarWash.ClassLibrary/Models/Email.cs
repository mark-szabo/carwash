using System.Text.Json.Serialization;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Storage queue email message.
    /// </summary>
    public class Email
    {
        /// <summary>
        /// Gets or sets the recipient of the email.
        /// </summary>
        [JsonPropertyName("to")]
        public string To { get; set; }

        /// <summary>
        /// Gets or sets the subject of the email.
        /// </summary>
        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the text (body) of the email.
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}
