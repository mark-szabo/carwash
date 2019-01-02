using System;
using System.Collections.Generic;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using MSHU.CarWash.Bot.Services;
using MSHU.CarWash.ClassLibrary.Enums;
using Newtonsoft.Json;

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
        public List<ServiceType> Services { get; set; } = new List<ServiceType>();

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
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the reservation start date and time in a Timex object.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> start date and time Timex object.
        /// </value>
        public TimexProperty Timex { get; set; }

        /// <summary>
        /// Gets or sets the reservation comment.
        /// </summary>
        /// <value>
        /// <see cref="ClassLibrary.Models.Reservation"/> comment.
        /// </value>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the last reservation settings.
        /// </summary>
        /// <value>
        /// LastSettings object.
        /// </value>
        public CarwashService.LastSettings LastSettings { get; set; }

        /// <summary>
        /// Gets or sets the recommended slots.
        /// </summary>
        /// <value>
        /// A list of DateTimes recommended to the user.
        /// </value>
        public List<DateTime> RecommendedSlots { get; set; } = new List<DateTime>();

        /// <summary>
        /// Gets or sets the slots the user can choose from on a given day.
        /// </summary>
        /// <value>
        /// A list of DateTimes sent to the user as choices.
        /// </value>
        public List<DateTime> SlotChoices { get; set; } = new List<DateTime>();
    }
}
