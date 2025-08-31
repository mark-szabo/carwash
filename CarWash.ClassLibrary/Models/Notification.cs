using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Notification model of the standard.
    /// <see href="https://notifications.spec.whatwg.org/#dictdef-notificationoptions">Notification API Standard</see>.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        public Notification() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Notification"/> class.
        /// </summary>
        /// <param name="text">Text of the notification.</param>
        public Notification(string text)
        {
            Body = text;
        }

        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; } = "CarWash";

        /// <summary>
        /// Gets or sets the language of the notification. Defaults to "en".
        /// </summary>
        [JsonPropertyName("lang")]
        public string Lang { get; set; } = "en";

        /// <summary>
        /// Gets or sets the text (body) of the notification.
        /// </summary>
        [JsonPropertyName("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="NotificationTag"/> of the notification.
        /// </summary>
        [JsonPropertyName("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the url of an image to be displayed in the notification.
        /// </summary>
        [JsonPropertyName("image")]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the url of an icon to be displayed in the notification.
        /// </summary>
        [JsonPropertyName("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the url of a badge icon to be displayed in the notification.
        /// </summary>
        [JsonPropertyName("badge")]
        public string Badge { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the notification. Defaults to <see cref="DateTime.UtcNow"/>.
        /// </summary>
        [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether the notification requires interaction. Defaults to false.
        /// </summary>
        [JsonPropertyName("requireInteraction")]
        public bool RequireInteraction { get; set; } = false;

        /// <summary>
        /// Gets or sets a list of <see cref="NotificationAction"/>s to be displayed in the notification.
        /// </summary>
        [JsonPropertyName("actions")]
        public List<NotificationAction> Actions { get; set; } = new List<NotificationAction>();
    }

    /// <summary>
    /// NotificationAction model of the standard.
    /// <see href="https://notifications.spec.whatwg.org/#dictdef-notificationaction">Notification API Standard</see>.
    /// </summary>
    public class NotificationAction
    {
        /// <summary>
        /// Gets or sets the tag of the action.
        /// </summary>
        [JsonPropertyName("action")]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the title (button text) of the action.
        /// </summary>
        [JsonPropertyName("title")]
        public string Title { get; set; }
    }

    /// <summary>
    /// Possible tags for CarWash notifications.
    /// </summary>
    public class NotificationTag
    {
        /// <summary>
        /// The notification is a reminder to drop-off the keys.
        /// </summary>
        public const string Reminder = "carwash_reminder";

        /// <summary>
        /// The notification is an FYI that the car is ready.
        /// </summary>
        public const string Done = "carwash_done";

        /// <summary>
        /// The notification is an FYI that the CarWash has left a comment.
        /// </summary>
        public const string Comment = "carwash_comment";
    }
}
