using System;
using System.Collections.Generic;
using MSHU.CarWash.ClassLibrary.Enums;

namespace MSHU.CarWash.Bot.States
{
    /// <summary>
    /// State properties ('conversation' scope) for making a new reservation.
    /// </summary>
    public class NewReservationState
    {
        /// <summary>
        /// Gets or sets the reservation vehicle plate number.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> vehicle plate number.
        /// </value>
        public string VehiclePlateNumber { get; set; }

        /// <summary>
        /// Gets or sets the reservation services.
        /// </summary>
        /// <value>
        /// List of <see cref="ServiceType"/>s.
        /// </value>
        public List<ServiceType> Services { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reservation is private.
        /// </summary>
        /// <value>
        /// A boolean indicating whether the <see cref="ClassLibrary.Models.Reservation"/> is private.
        /// </value>
        public bool Private { get; set; }

        /// <summary>
        /// Gets or sets the reservation start date and time.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> start date and time.
        /// </value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the reservation comment.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> comment.
        /// </value>
        public string Comment { get; set; }
    }
}
