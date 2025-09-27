using System;

namespace CarWash.ClassLibrary.Models.Exceptions
{
    /// <summary>
    /// Represents an exception that is thrown when a reservation cannot be found.
    /// </summary>
    public class ReservationNotFoundException : Exception
    {
        /// <inheritdoc />
        public ReservationNotFoundException() : base("Reservation was not found.")
        {
        }

        /// <inheritdoc />
        public ReservationNotFoundException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public ReservationNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a reservation cannot be found.
    /// </summary>
    public class ReservationConflictException : Exception
    {
        /// <inheritdoc />
        public ReservationConflictException() : base("Reservation conflict.")
        {
        }

        /// <inheritdoc />
        public ReservationConflictException(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public ReservationConflictException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Represents an exception that is thrown when a reservation validation error occurs.
    /// </summary>
    /// <remarks>This exception is typically used to indicate that a reservation operation has failed due to
    /// invalid input or a violation of business rules. It can be used to provide additional context about the
    /// validation failure through the exception message or an inner exception.</remarks>
    public class ReservationValidationExeption : Exception
    {
        /// <inheritdoc />
        public ReservationValidationExeption()
        {
        }

        /// <inheritdoc />
        public ReservationValidationExeption(string? message) : base(message)
        {
        }

        /// <inheritdoc />
        public ReservationValidationExeption(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
