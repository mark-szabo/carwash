using System;
using System.Diagnostics.CodeAnalysis;
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
        public KeyLockerBoxHistory() { }

        /// <summary>
        /// Creates a new KeyLockerBoxHistory from a KeyLockerBox.
        /// </summary>
        /// <param name="box">The KeyLockerBox to copy values from.</param>
        [SetsRequiredMembers]
        public KeyLockerBoxHistory(KeyLockerBox box)
        {
            BoxId = box.Id;
            LockerId = box.LockerId;
            BoxSerial = box.BoxSerial;
            Name = box.Name;
            State = box.State;
            IsDoorClosed = box.IsDoorClosed;
            ModifiedAt = DateTime.UtcNow;
            ModifiedBy = box.LastModifiedBy;
        }

        /// <inheritdoc />
        public required string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the original unique identifier for the box (not unique in the history table).
        /// </summary>
        public required string BoxId { get; set; }

        /// <summary>
        /// Id of the key locker, containing multiple boxes.
        /// Device id of the IoT Hub device.
        /// </summary>
        public required string LockerId { get; set; }

        /// <summary>
        /// Serial number of the key locker box, incrementing from 1 to N.
        /// IoT Hub Direct method name that will open the given box can be generated from this by prefixing it with <see cref="CarWashConfiguration.KeyLockerConfiguration.BoxIotIdPrefix"/>
        /// </summary>
        public required int BoxSerial { get; set; }

        /// <summary>
        /// Friendly name of the box, used to identify it.
        /// </summary>
        public required string Name { get; set; }

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
