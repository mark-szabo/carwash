using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Service for managing reservation business logic and operations
    /// </summary>
    public interface IReservationService
    {
        /// <summary>
        /// Creates a new reservation with all necessary business logic
        /// </summary>
        /// <param name="reservation">The reservation to create</param>
        /// <param name="currentUser">The current user creating the reservation</param>
        /// <returns>The created reservation</returns>
        Task<Reservation> CreateReservationAsync(Reservation reservation, User currentUser);

        /// <summary>
        /// Updates an existing reservation with business logic validation
        /// </summary>
        /// <param name="reservation">The reservation to update</param>
        /// <param name="currentUser">The current user updating the reservation</param>
        /// <returns>The updated reservation</returns>
        Task<Reservation> UpdateReservationAsync(Reservation reservation, User currentUser);

        /// <summary>
        /// Deletes a reservation and handles cleanup
        /// </summary>
        /// <param name="reservationId">The ID of the reservation to delete</param>
        /// <param name="currentUser">The current user deleting the reservation</param>
        /// <returns>The deleted reservation</returns>
        Task<Reservation> DeleteReservationAsync(string reservationId, User currentUser);

        /// <summary>
        /// Confirms dropoff and location for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="location">The car location</param>
        /// <param name="currentUser">The current user</param>
        /// <returns>Task representing the operation</returns>
        Task ConfirmDropoffAsync(string reservationId, string location, User currentUser);

        /// <summary>
        /// Starts the wash process for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task StartWashAsync(string reservationId, User currentUser);

        /// <summary>
        /// Completes the wash process for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task CompleteWashAsync(string reservationId, User currentUser);

        /// <summary>
        /// Confirms payment for a private reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task ConfirmPaymentAsync(string reservationId, User currentUser);

        /// <summary>
        /// Sets the state of a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="state">The new state</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task SetReservationStateAsync(string reservationId, State state, User currentUser);

        /// <summary>
        /// Adds a comment to a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="comment">The comment text</param>
        /// <param name="currentUser">The current user</param>
        /// <returns>Task representing the operation</returns>
        Task AddCommentAsync(string reservationId, string comment, User currentUser);

        /// <summary>
        /// Checks if a vehicle is an MPV based on historical data
        /// </summary>
        /// <param name="vehiclePlateNumber">The vehicle plate number</param>
        /// <returns>True if the vehicle is categorized as MPV</returns>
        Task<bool> IsMpvAsync(string vehiclePlateNumber);

        /// <summary>
        /// Sets the MPV flag for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="mpv">The MPV flag value</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task SetMpvAsync(string reservationId, bool mpv, User currentUser);

        /// <summary>
        /// Updates the services for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="services">The new services list</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task UpdateServicesAsync(string reservationId, List<int> services, User currentUser);

        /// <summary>
        /// Updates the location for a reservation
        /// </summary>
        /// <param name="reservationId">The reservation ID</param>
        /// <param name="location">The new location</param>
        /// <param name="currentUser">The current user (must be carwash admin)</param>
        /// <returns>Task representing the operation</returns>
        Task UpdateLocationAsync(string reservationId, string location, User currentUser);

        /// <summary>
        /// Gets not available dates and times
        /// </summary>
        /// <param name="currentUser">The current user</param>
        /// <param name="daysAhead">Number of days ahead to check</param>
        /// <returns>Not available dates and times view model</returns>
        Task<NotAvailableDatesAndTimes> GetNotAvailableDatesAndTimesAsync(User currentUser, int daysAhead = 365);

        /// <summary>
        /// Gets last settings for a user
        /// </summary>
        /// <param name="currentUser">The current user</param>
        /// <returns>Last settings view model or null if no previous reservation exists</returns>
        Task<LastSettings?> GetLastSettingsAsync(User currentUser);

        /// <summary>
        /// Gets reservation capacity for a specific date
        /// </summary>
        /// <param name="date">The date to check capacity for</param>
        /// <returns>List of reservation capacity view models</returns>
        Task<IEnumerable<ReservationCapacity>> GetReservationCapacityAsync(DateTime date);

        /// <summary>
        /// Exports reservations to Excel for a given timespan
        /// </summary>
        /// <param name="currentUser">The current user</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <returns>Excel file data as byte array</returns>
        Task<byte[]> ExportReservationsAsync(User currentUser, DateTime? startDate = null, DateTime? endDate = null);
    }

    /// <summary>
    /// Model for not available dates and times
    /// </summary>
    public record NotAvailableDatesAndTimes(IEnumerable<DateOnly> Dates, IEnumerable<DateTime> Times);

    /// <summary>
    /// Model for last user settings
    /// </summary>
    public record LastSettings(string VehiclePlateNumber, string Location, List<int> Services);

    /// <summary>
    /// Model for reservation capacity
    /// </summary>
    public record ReservationCapacity(DateTime StartTime, int FreeCapacity);

    /// <summary>
    /// Result of reservation validation
    /// </summary>
    /// <param name="IsValid"></param>
    /// <param name="ErrorMessage"></param>
    internal record ValidationResult(bool IsValid, string ErrorMessage = "");

    [Serializable]
    public class ReservationValidationExeption : Exception
    {
        public ReservationValidationExeption()
        {
        }

        public ReservationValidationExeption(string? message) : base(message)
        {
        }

        public ReservationValidationExeption(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}