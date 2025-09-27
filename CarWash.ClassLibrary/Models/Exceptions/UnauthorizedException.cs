using System;

namespace CarWash.ClassLibrary.Models.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when an authorization attempt fails.
    /// </summary>
    /// <remarks>This exception is used to indicate that a user does not have the
    /// necessary permissions to perform a specific operation. It can be instantiated with a default message, a custom
    /// message, or a custom message along with an inner exception for additional context.</remarks>
    public class UnauthorizedException : Exception
    {
        /// <inheritdoc />
        public UnauthorizedException() : base("Authorization failed.")
        {
        }

        /// <inheritdoc />
        public UnauthorizedException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public UnauthorizedException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
