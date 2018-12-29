namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// State properties ('conversation' scope) for drop-off confirmation.
    /// </summary>
    public class ConfirmDropoffState
    {
        /// <summary>
        /// Gets or sets the reservation id.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> id.
        /// </value>
        public string ReservationId { get; set; }

        /// <summary>
        /// Gets or sets the reservation location (building).
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> location (building).
        /// </value>
        public string Building { get; set; }

        /// <summary>
        /// Gets or sets the reservation location (floor).
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> location (floor).
        /// </value>
        public string Floor { get; set; }

        /// <summary>
        /// Gets or sets the reservation location (seat).
        /// </summary>
        /// <value>
        /// (Optional) <see cref="ClassLibrary.Models.Reservation"/> location (seat).
        /// </value>
        public string Seat { get; set; }

        /// <summary>
        /// Gets the reservation location.
        /// </summary>
        /// <value>
        /// A concatenation of the building, floor and seat separated by '/'.
        /// </value>
        public string Location { get => $"{Building}/{Floor}/{Seat}"; }
    }
}
