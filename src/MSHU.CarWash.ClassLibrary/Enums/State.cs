namespace MSHU.CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Types of states a reservation can be in.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// Reservation was submitted, but is in the future.
        /// No task needed.
        /// </summary>
        SubmittedNotActual = 0,

        /// <summary>
        /// A reminder was sent to the user that they need to drop-off 
        /// their key and confirm the vehicle location.
        /// </summary>
        ReminderSentWaitingForKey = 1,

        /// <summary>
        /// Key was dropped off and the vehicle location is confirmed.
        /// </summary>
        DropoffAndLocationConfirmed = 2,

        /// <summary>
        /// Car wash has started, is in-progress right now.
        /// </summary>
        WashInProgress = 3,

        /// <summary>
        /// Car is ready, but as the reservation is private, they need to pay.
        /// </summary>
        NotYetPaid = 4,

        /// <summary>
        /// Car is ready, and if needed, paid.
        /// </summary>
        Done = 5
    }

    /// <summary>
    /// Extension class for State.
    /// </summary>
    public static class StateExtensions
    {
        /// <summary>
        /// Converts the State enum to a display-friendly string.
        /// </summary>
        /// <param name="state">The state type to convert.</param>
        /// <returns>A display-friendly string.</returns>
        public static string ToFriendlyString(this State state)
        {
            switch (state)
            {
                case State.SubmittedNotActual:
                    return "Scheduled";
                case State.ReminderSentWaitingForKey:
                    return "Leave the key at reception";
                case State.DropoffAndLocationConfirmed:
                    return "All set, ready to wash";
                case State.WashInProgress:
                    return "Wash in progress";
                case State.NotYetPaid:
                    return "You need to pay";
                case State.Done:
                    return "Completed";
                default:
                    return "No info";
            }
        }
    }
}
