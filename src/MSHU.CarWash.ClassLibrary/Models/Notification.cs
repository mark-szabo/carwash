using System;
using System.Collections.Generic;

namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    ///     <see href="https://notifications.spec.whatwg.org/#dictdef-notificationoptions">Notification API Standard</see>
    /// </summary>
    public class Notification
    {
        public string Lang { get; set; }
        public string Body { get; set; }
        public string Tag { get; set; }
        public string Image { get; set; }
        public string Icon { get; set; }
        public string Badge { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool RequireInteraction { get; set; } = true;
        public List<NotificationAction> Actions { get; set; }
    }

    /// <summary>
    ///     <see href="https://notifications.spec.whatwg.org/#dictdef-notificationaction">Notification API Standard</see>
    /// </summary>
    public class NotificationAction
    {
        public string Action { get; set; }
        public string Title { get; set; }
    }
}
