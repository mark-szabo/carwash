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

        // Backward compatibility properties for legacy integer-based configurations
        /// <summary>
        /// Legacy property for backward compatibility. Use StartTime instead.
        /// </summary>
        [Obsolete("Use StartTime property instead. This property is for backward compatibility only.")]
        public int StartTimeHour 
        { 
            get => StartTime.Hours; 
            set => StartTime = new TimeSpan(value, 0, 0); 
        }

        /// <summary>
        /// Legacy property for backward compatibility. Use EndTime instead.
        /// </summary>
        [Obsolete("Use EndTime property instead. This property is for backward compatibility only.")]
        public int EndTimeHour 
        { 
            get => EndTime.Hours; 
            set => EndTime = new TimeSpan(value, 0, 0); 
        }
    }
}
