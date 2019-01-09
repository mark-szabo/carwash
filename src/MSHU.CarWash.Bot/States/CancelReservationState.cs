namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// State properties ('conversation' scope) for reservation cancellation.
    /// </summary>
    public class CancelReservationState
    {
        /// <summary>
        /// Gets or sets the reservation id.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> id.
        /// </value>
        public string ReservationId { get; set; }
    }
}
