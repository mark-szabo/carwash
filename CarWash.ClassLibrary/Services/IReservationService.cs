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
        /// Validates a reservation for business rules and constraints
        /// </summary>
        /// <param name="reservation">The reservation to validate</param>
        /// <param name="isUpdate">Whether this is an update operation</param>
        /// <param name="currentUser">The current user performing the operation</param>
        /// <param name="excludeReservationId">For updates, the ID of reservation to exclude from capacity calculations</param>
        /// <returns>Validation result with any error messages</returns>
        Task<ValidationResult> ValidateReservationAsync(Reservation reservation, bool isUpdate, User currentUser, string? excludeReservationId = null);

        /// <summary>
        /// Creates a new reservation with all necessary business logic
        /// </summary>
        /// <param name="reservation">The reservation to create</param>
        /// <param name="currentUser">The current user creating the reservation</param>
        /// <param name="dropoffConfirmed">Whether dropoff is pre-confirmed</param>
        /// <returns>The created reservation</returns>
        Task<Reservation> CreateReservationAsync(Reservation reservation, User currentUser, bool dropoffConfirmed = false);

        /// <summary>
        /// Updates an existing reservation with business logic validation
        /// </summary>
        /// <param name="reservation">The reservation to update</param>
        /// <param name="currentUser">The current user updating the reservation</param>
        /// <param name="dropoffConfirmed">Whether dropoff is pre-confirmed</param>
        /// <returns>The updated reservation</returns>
        Task<Reservation> UpdateReservationAsync(Reservation reservation, User currentUser, bool dropoffConfirmed = false);

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
    }

    /// <summary>
    /// Result of reservation validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}