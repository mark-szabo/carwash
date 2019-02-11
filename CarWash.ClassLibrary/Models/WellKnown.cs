using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Well-known information
    /// </summary>
    public class WellKnown
    {
        /// <summary>
        /// List of bookable slots and their capacity.
        /// </summary>
        /// <remarks>
        /// Capacity is in number of cars per slot, not in minutes!
        /// </remarks>
        public List<Slot> Slots { get; set; } = new List<Slot>();

        /// <summary>
        /// List of companies whose users can use the CarWash app.
        /// </summary>
        public List<Company> Companies { get; set; } = new List<Company>();

        /// <summary>
        /// List of parking garages where cars are allowed to be left.
        /// </summary>
        public List<Garage> Garages { get; set; } = new List<Garage>();

        /// <summary>
        /// CarWash app settings referring to reservations.
        /// </summary>
        public CarWashConfiguration.ReservationSettings ReservationSettings { get; set; } = new CarWashConfiguration.ReservationSettings();
    }
}
