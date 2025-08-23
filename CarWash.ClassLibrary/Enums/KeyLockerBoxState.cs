namespace CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Types of states a key locker box can be in.
    /// </summary>
    public enum KeyLockerBoxState
    {
        /// <summary>
        /// The box is empty and its door is closed, ready to be opened and used.
        /// </summary>
        Empty = 0,

        /// <summary>
        /// The box contains a car key and its door is closed, only CarWash and the reservation owner is allowed to open it.
        /// </summary>
        Used = 1,

        /// <summary>
        /// The box is disabled and cannot be used.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// The box is disconnected or unavailable.
        /// </summary>
        Unavailable = 3,
    }
}
