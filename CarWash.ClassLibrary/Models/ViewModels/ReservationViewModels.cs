using System;
using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models.ViewModels
{
    /// <summary>
    /// View model for not available dates and times
    /// </summary>
    public record NotAvailableDatesAndTimesViewModel(IEnumerable<DateOnly> Dates, IEnumerable<DateTime> Times);

    /// <summary>
    /// View model for last user settings
    /// </summary>
    public record LastSettingsViewModel(string VehiclePlateNumber, string Location, List<int> Services);

    /// <summary>
    /// View model for reservation capacity
    /// </summary>
    public record ReservationCapacityViewModel(DateTime StartTime, int FreeCapacity);

    /// <summary>
    /// View model for obfuscated reservations
    /// </summary>
    public record ObfuscatedReservationViewModel(
        string Company,
        List<int> Services,
        int? TimeRequirement,
        DateTime StartDate,
        DateTime EndDate);

    /// <summary>
    /// View model for reservation percentage
    /// </summary>
    public record ReservationPercentageViewModel(DateTime StartTime, double Percentage);

    /// <summary>
    /// View model for confirm dropoff by email
    /// </summary>
    public record ConfirmDropoffByEmailViewModel(string Email, string Location, string VehiclePlateNumber);
}