using System;
using CarWash.ClassLibrary.Enums;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Audit table storing KeyLockerBox state history.
    /// DB mapped entity.
    /// </summary>
    public class KeyLockerBoxHistory : ApplicationDbContext.IEntity
    {
        /// <summary>
        /// Default constructor for EF and serialization.
        /// </summary>
        public KeyLockerBoxHistory()
        {
        }

        /// <summary>
        /// Creates a new KeyLockerBoxHistory from a KeyLockerBox.
        /// </summary>
        /// <param name="box">The KeyLockerBox to copy values from.</param>
        public KeyLockerBoxHistory(KeyLockerBox box)
        {
            Id = box.Id;
            LockerId = box.LockerId;
            Name = box.Name;
            State = box.State;
            IsDoorClosed = box.IsDoorClosed;
            ModifiedAt = DateTime.UtcNow;
            ModifiedBy = box.LastModifiedBy;
        }

        /// <inheritdoc />
        public string Id { get; set; }

        /// <summary>
        /// Id of the key locker, containing multiple boxes.
        /// Device id of the IoT Hub device.
        /// </summary>
        public string LockerId { get; set; }

        /// <summary>
        /// Friendly name of the box, used to identify it.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Curent state of the box.
        /// </summary>
        public KeyLockerBoxState State { get; set; }

        /// <summary>
        /// Indicates if the box door is currently closed.
        /// </summary>
        public bool IsDoorClosed { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified the entity.
        /// </summary>
        public string? ModifiedBy { get; set; }
    }
}
