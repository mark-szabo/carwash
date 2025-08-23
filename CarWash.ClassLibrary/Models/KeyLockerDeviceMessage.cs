using System;
using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Represents a message received from a key locker IoT device.
    /// This class is used to parse the JSON message sent by the device,
    /// containing the state of the key locker boxes.
    /// </summary>
    public class KeyLockerDeviceMessage
    {
        /// <summary>
        /// Gets or sets the integer value representing the state of all boxes in the key locker device.
        /// Each bit in this integer corresponds to the state of a specific box.
        /// </summary>
        public int Inputs { get; set; }

        /// <summary>
        /// Returns a list of boolean values representing the state of each box in the key locker.
        /// Each element in the list corresponds to a box, where <c>true</c> indicates the box is closed,
        /// and <c>false</c> indicates the box is open.
        /// The number of boxes is rounded up to the nearest multiple of 8.
        /// </summary>
        /// <returns>
        /// A list of boolean values indicating the state of each box.
        /// </returns>
        public List<bool> GetBoxStates()
        {
            // Calculate the number of bits needed to represent the input value in binary.
            int bitLength = Convert.ToString(Inputs, 2).Length;

            // Round up the bit length to the nearest multiple of 8 to determine the total number of boxes.
            int boxCount = (bitLength + 7) / 8 * 8;

            // Initialize a list of boolean values to store the state of each box, with a capacity equal to boxCount.
            var states = new List<bool>(boxCount);

            // Iterate through each bit position up to boxCount.
            for (int i = 0; i < boxCount; i++)
            {
                // Check if the i-th bit in input is set (1) and add the result (true if set, false otherwise) to the states list.
                states.Add(((Inputs >> i) & 1) == Constants.KeyLockerBoxDoorState.Closed);
            }

            return states;
        }
    }
}
