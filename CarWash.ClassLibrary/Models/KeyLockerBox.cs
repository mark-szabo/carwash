using System;
using CarWash.ClassLibrary.Enums;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database representation of a key locker box object.
    /// Key lockers are used by CarWash and its users to easily and securely drop off and pick up car keys.
    /// A key locker is deployed to a fixed location and contain many singular boxes which can be remotely opened.
    /// DB mapped entity.
    /// </summary>
    public class KeyLockerBox : ApplicationDbContext.IEntity
    {
        /// <inheritdoc />
        public string Id { get; set; } = Guid.NewGuid().ToString();

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
        /// Name of the building where the key locker is located.
        /// </summary>
        public required string Building { get; set; }

        /// <summary>
        /// Name of the floor where the key locker is located.
        /// </summary>
        public required string Floor { get; set; }

        /// <summary>
        /// Friendly name of the box, used to identify it.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Curent state of the box.
        /// </summary>
        public KeyLockerBoxState State { get; set; } = KeyLockerBoxState.Empty;

        /// <summary>
        /// Indicates if the box door is currently closed.
        /// </summary>
        public bool IsDoorClosed { get; set; } = false;

        /// <summary>
        /// Identifies if the box is currently connected.
        /// </summary>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the user who last modified the entity.
        /// </summary>
        public string? LastModifiedBy { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the box last transmitted any information.
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
}
