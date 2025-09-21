using System;
using System.Collections.Generic;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;

namespace CarWash.PWA.ViewModels
{
    /// <summary>
    /// View model for confirm dropoff by email
    /// </summary>
    public record ConfirmDropoffByEmailViewModel(
        string Email, 
        string Location, 
        string VehiclePlateNumber, 
        string ReservationId, 
        string Hash);

    /// <summary>
    /// View model for reservations
    /// </summary>
    public record ReservationViewModel(
        string Id,
        string UserId,
        string VehiclePlateNumber,
        string Location,
        KeyLockerBoxViewModel KeyLockerBox,
        State State,
        List<int> Services,
        bool Private,
        bool Mpv,
        DateTime StartDate,
        DateTime EndDate,
        string Comments)
    {
        public ReservationViewModel(Reservation reservation) : this(
                reservation.Id,
                reservation.UserId,
                reservation.VehiclePlateNumber,
                reservation.Location,
                reservation.KeyLockerBox != null ? new KeyLockerBoxViewModel(reservation.KeyLockerBox) : null,
                reservation.State,
                reservation.Services,
                reservation.Private,
                reservation.Mpv,
                reservation.StartDate,
                reservation.EndDate ?? reservation.StartDate,
                reservation.CommentsJson ?? string.Empty)
        { }
    }

    /// <summary>
    /// View model for admin reservations
    /// </summary>
    public record AdminReservationViewModel(
        string Id,
        string UserId,
        UserViewModel User,
        string VehiclePlateNumber,
        string Location,
        KeyLockerBoxViewModel KeyLockerBox,
        State State,
        List<int> Services,
        bool Private,
        bool Mpv,
        DateTime StartDate,
        DateTime EndDate,
        string Comments)
    {
        public AdminReservationViewModel(Reservation reservation) : this(
                reservation.Id,
                reservation.UserId,
                new UserViewModel(reservation.User),
                reservation.VehiclePlateNumber,
                reservation.Location,
                reservation.KeyLockerBox != null ? new KeyLockerBoxViewModel(reservation.KeyLockerBox) : null,
                reservation.State,
                reservation.Services,
                reservation.Private,
                reservation.Mpv,
                reservation.StartDate,
                reservation.EndDate ?? reservation.StartDate,
                reservation.CommentsJson ?? string.Empty)
        { }
    }
}