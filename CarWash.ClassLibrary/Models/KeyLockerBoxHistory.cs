using System;
using CarWash.ClassLibrary.Enums;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Audit table storing KeyLockerBox state history.
    /// DB mapped entity.
    /// </summary>
    public class KeyLockerBoxHistory(KeyLockerBox box) : ApplicationDbContext.IEntity
    {
        /// <inheritdoc />
        public string Id { get; set; } = box.Id;

        /// <summary>
        /// Id of the key locker, containing multiple boxes.
        /// Device id of the IoT Hub device.
        /// </summary>
        public string LockerId { get; set; } = box.LockerId;

        /// <summary>
        /// Friendly name of the box, used to identify it.
        /// </summary>
        public string Name { get; set; } = box.Name;

        /// <summary>
        /// Curent state of the box.
        /// </summary>
        public KeyLockerBoxState State { get; set; } = box.State;

        /// <summary>
        /// Indicates if the box door is currently closed.
        /// </summary>
        public bool IsDoorClosed { get; set; } = box.IsDoorClosed;

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who last modified the entity.
        /// </summary>
        public string? ModifiedBy { get; set; } = box.LastModifiedBy;
    }
}
