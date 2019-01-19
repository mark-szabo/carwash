namespace CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Types of notification channels the user can choose from.
    /// </summary>
    public enum NotificationChannel
    {
        /// <summary>
        /// Notification channel not yet set.
        /// </summary>
        NotSet,

        /// <summary>
        /// Notifications are disabled.
        /// </summary>
        Disabled,

        /// <summary>
        /// Email channel is choosen for notifications.
        /// </summary>
        Email,

        /// <summary>
        /// Push channel is choosen for notifications.
        /// </summary>
        Push
    }
}
