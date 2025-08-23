using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a parking garage where cars are allowed to be left.
    /// </summary>
    public class Garage
    {
        /// <summary>
        /// Gets or sets the building name where the parking garage is located.
        /// </summary>
        public string Building { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the floors of the parking garage.
        /// </summary>
        public List<string> Floors { get; set; } = [];

        /// <summary>
        /// Gets or sets the unique identifier for the key locker.
        /// </summary>
        public string KeyLockerId { get; set; } = string.Empty;
    }
}
