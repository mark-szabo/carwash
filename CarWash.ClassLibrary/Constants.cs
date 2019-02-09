using System.Collections.Generic;
using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary
{
    /// <summary>
    /// Class containing constants.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Bookable slots and their capacity (in washes and not in minutes!)
        /// </summary>
        public static readonly List<Slot> Slots = new List<Slot>
        {
            new Slot {StartTime = 8, EndTime = 11, Capacity = 12},
            new Slot {StartTime = 11, EndTime = 14, Capacity = 12},
            new Slot {StartTime = 14, EndTime = 17, Capacity = 11}
        };
    }
}
