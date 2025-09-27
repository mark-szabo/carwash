using System;

namespace CarWash.ClassLibrary.Models.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when a user cannot be found.
    /// </summary>
    public class UserNotFoundException : Exception
    {
        /// <inheritdoc />
        public UserNotFoundException() : base("User was not found.")
        {
        }

        /// <inheritdoc />
        public UserNotFoundException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public UserNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
