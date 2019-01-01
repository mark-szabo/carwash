namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Carwash slot.
    /// </summary>
    public class Slot
    {
        /// <summary>
        /// Slot start time in hours.
        /// </summary>
        public int StartTime { get; set; }

        /// <summary>
        /// Slot end time in hours.
        /// </summary>
        public int EndTime { get; set; }

        /// <summary>
        /// Slot capacity in washes. (Not in minutes!)
        /// </summary>
        public int Capacity { get; set; }
    }
}
