using CarWash.ClassLibrary.Enums;

namespace CarWash.ClassLibrary.Extensions
{
    /// <summary>
    /// Extension class for State.
    /// </summary>
    public static class StateExtensions
    {
        /// <summary>
        /// Converts the <see cref="State"/> enum to a display-friendly string.
        /// </summary>
        /// <param name="state">The state type to convert.</param>
        /// <returns>A display-friendly string.</returns>
        public static string ToFriendlyString(this State state) => state switch
        {
            State.SubmittedNotActual => "Scheduled",
            State.ReminderSentWaitingForKey => "Drop-off the key",
            State.DropoffAndLocationConfirmed => "All set, ready to wash",
            State.WashInProgress => "Wash in progress",
            State.NotYetPaid => "You need to pay",
            State.Done => "Completed",
            _ => "No info",
        };

        /// <summary>
        /// Converts the <see cref="KeyLockerBoxState"/> enum to a display-friendly string.
        /// </summary>
        /// <param name="state">The state type to convert.</param>
        /// <returns>A display-friendly string.</returns>
        public static string ToFriendlyString(this KeyLockerBoxState state) => state switch
        {
            KeyLockerBoxState.Empty => "Box is empty.",
            KeyLockerBoxState.Used => "Box is in use.",
            KeyLockerBoxState.Disabled => "Box is disabled.",
            KeyLockerBoxState.Unavailable => "Box is unavailable.",
            _ => "Box is unavailable (invalid state).",
        };
    }
}
