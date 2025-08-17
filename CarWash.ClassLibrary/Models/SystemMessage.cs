using System;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Represents a system message with a severity level and active time range.
    /// </summary>
    public class SystemMessage : ApplicationDbContext.IEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the system message.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the start date and time when the message becomes active.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date and time when the message is no longer active.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the severity level of the message.
        /// </summary>
        public Severity Severity { get; set; }
    }

    /// <summary>
    /// Defines the severity levels for a system message.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// Indicates an error message.
        /// </summary>
        Error,

        /// <summary>
        /// Indicates a warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates an informational message.
        /// </summary>
        Info,

        /// <summary>
        /// Indicates a success message.
        /// </summary>
        Success
    }
}
