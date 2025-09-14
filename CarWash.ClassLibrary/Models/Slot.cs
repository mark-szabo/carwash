using System;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a bookable slot in the CarWash app.
    /// </summary>
    public class Slot
    {
        /// <summary>
        /// Gets or sets the slot start time as time-of-day.
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Gets or sets the slot end time as time-of-day.
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Gets or sets the slot capacity in washes. (Not in minutes!)
        /// </summary>
        public int Capacity { get; set; }
    }
}
