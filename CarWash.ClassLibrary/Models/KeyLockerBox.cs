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
        /// </summary>
        public required string LockerId { get; set; }

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
        public KeyLockerBoxState State { get; set; } = KeyLockerBoxState.Unavailable;

        /// <summary>
        /// Indicates if the box is currently open.
        /// </summary>
        public bool IsDoorOpen { get; set; } = false;

        /// <summary>
        /// Identifies if the box is currently connected.
        /// </summary>
        public bool IsConnected { get; set; } = false;

        /// <summary>
        /// Gets or sets the date and time when the entity was last modified.
        /// </summary>
        public DateTime LastModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the user who last modified the entity.
        /// </summary>
        public string? LastModifiedBy { get; set; }
    }
}
