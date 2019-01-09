namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a bookable slot in the CarWash app.
    /// </summary>
    public class Slot
    {
        /// <summary>
        /// Gets or sets the slot start time in hours.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Gets or sets the slot end time in hours.
        /// </summary>
        public int EndTime { get; set; }

        /// <summary>
        /// Gets or sets the slot capacity in washes. (Not in minutes!)
        /// </summary>
        public int Capacity { get; set; }
    }
}
