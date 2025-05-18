namespace CarWash.ClassLibrary.Enums
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
        Done = 5,
    }
}
