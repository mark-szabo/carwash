using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.Exceptions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    internal interface IReservationValidator
    {
        /// <summary>
        /// Sums the capacity of all not started slots, what are left from the day and capacity of slots (not time in minutes!).
        /// </summary>
        /// <remarks>
        /// eg. It is 9:00 AM.
        /// The slot 8-11 has already started.
        /// The slot 11-14 is not yet started, so add the capacity (eg. 12) to the sum.
        /// The slot 14-17 is not yet started, so add the capacity (eg. 11) to the sum.
        /// Sum will be 23.
        /// </remarks>
        int RemainingSlotCapacityToday { get; }

        /// <summary>
        /// Validates a reservation for business rules and constraints
        /// </summary>
        /// <param name="reservation">The reservation to validate</param>
        /// <param name="isUpdate">Whether this is an update operation</param>
        /// <param name="currentUser">The current user performing the operation</param>
        /// <param name="excludeReservationId">For updates, the ID of reservation to exclude from capacity calculations</param>
        /// <returns>Validation result with any error messages</returns>
        Task<ValidationResult> ValidateReservationAsync(Reservation reservation, bool isUpdate, User currentUser, string? excludeReservationId = null);
    }

    internal class ReservationValidator(ApplicationDbContext context, IOptionsMonitor<CarWashConfiguration> configuration, TelemetryClient telemetryClient) : IReservationValidator
    {
        /// <inheritdoc />
        public int RemainingSlotCapacityToday
        {
            get
            {
                var capacity = 0;
                var now = DateTime.UtcNow;
                var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;
                TimeSpan currentTimeOfDay;

                if (timeZoneId == "UTC")
                {
                    currentTimeOfDay = now.TimeOfDay;
                }
                else
                {
                    var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    var nowInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(now, providerTimeZone);
                    currentTimeOfDay = nowInProviderZone.TimeOfDay;
                }

                foreach (var slot in configuration.CurrentValue.Slots)
                {
                    if (currentTimeOfDay < slot.StartTime) capacity += slot.Capacity;

                    if (currentTimeOfDay >= slot.StartTime && currentTimeOfDay < slot.EndTime)
                    {
                        DateTime endTimeUtc;

                        if (timeZoneId == "UTC")
                        {
                            endTimeUtc = now.Date.Add(slot.EndTime);
                        }
                        else
                        {
                            var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                            var nowInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(now, providerTimeZone);
                            var todayInProviderZone = nowInProviderZone.Date;
                            var endTimeInProviderZone = todayInProviderZone.Add(slot.EndTime);
                            endTimeUtc = TimeZoneInfo.ConvertTimeToUtc(endTimeInProviderZone, providerTimeZone);
                        }

                        var timeDifference = endTimeUtc - now;
                        var slotTimeSpan = (slot.EndTime - slot.StartTime).TotalMinutes;
                        var slotCapacity = timeDifference.TotalMinutes / slotTimeSpan * slot.Capacity;
                        capacity += (int)Math.Floor(slotCapacity);
                    }
                }

                return capacity;
            }
        }

        /// <inheritdoc />
        public async Task<ValidationResult> ValidateReservationAsync(Reservation reservation, bool isUpdate, User currentUser, string? excludeReservationId = null)
        {
            // Input validation
            if (reservation.Services == null || reservation.Services.Count == 0)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: No service chosen.",
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Services", reservation.ServicesJson }
                    });
                return new ValidationResult(false, "No service chosen.");
            }

            // Calculate EndDate if not provided (needed for validation)
            if (reservation.EndDate == null || reservation.EndDate == default(DateTime))
            {
                try
                {
                    reservation.EndDate = CalculateEndTime(reservation.StartDate, reservation.EndDate);
                }
                catch (ArgumentOutOfRangeException)
                {
                    return new ValidationResult(false, "Reservation can be made to slots only.");
                }
            }

            // Calculate time requirement for validation
            var timeRequirement = reservation.Services.Contains(Constants.ServiceType.Carpet) ?
                configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * configuration.CurrentValue.Reservation.TimeUnit :
                configuration.CurrentValue.Reservation.TimeUnit;

            // Authorization validation - these should throw exceptions for proper HTTP responses
            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                {
                    telemetryClient.TrackTrace(
                        "Forbid: User cannot reserve in the name of others unless admin.",
                        SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", reservation.UserId },
                            { "IsAdmin", currentUser.IsAdmin.ToString() },
                            { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                            { "CreatedById", currentUser.Id }
                        });
                    throw new UnauthorizedException("User cannot reserve in the name of others unless admin.");
                }

                if (reservation.UserId != null)
                {
                    var reservationUser = await context.Users.FindAsync(reservation.UserId);
                    if (currentUser.IsAdmin && reservationUser?.Company != currentUser.Company)
                    {
                        telemetryClient.TrackTrace(
                            "Forbid: Admin cannot reserve in the name of other companies' users.",
                            SeverityLevel.Error,
                            new Dictionary<string, string>
                            {
                                { "UserId", reservation.UserId },
                                { "IsAdmin", currentUser.IsAdmin.ToString() },
                                { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                                { "CreatedById", currentUser.Id }
                            });
                        throw new UnauthorizedException("Admin cannot reserve in the name of other companies' users.");
                    }
                }
            }

            // Time and business validations
            if (!IsStartAndEndTimeOnSameDay(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult(false, "Reservation time range should be located entirely on the same day.");

            if (!IsEndTimeLaterThanStartTime(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult(false, "Reservation end time should be later than the start time.");

            if (IsInPast(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult(false, "Cannot reserve in the past.");

            if (!IsInSlot(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult(false, "Reservation can be made to slots only.");

            if (!isUpdate && await IsUserConcurrentReservationLimitMetAsync(currentUser))
                return new ValidationResult(false, $"Cannot have more than {configuration.CurrentValue.Reservation.UserConcurrentReservationLimit} concurrent active reservations.");

            if (await IsBlocked(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult(false, "This time is blocked.");

            if (!await IsEnoughTimeOnDateAsync(reservation.StartDate, timeRequirement, currentUser, excludeReservationId))
                return new ValidationResult(false, "Company limit has been met for this day or there is not enough time at all.");

            if (!await IsEnoughTimeInSlotAsync(reservation.StartDate, timeRequirement, currentUser, excludeReservationId))
                return new ValidationResult(false, "There is not enough time in that slot.");

            return new ValidationResult(true);
        }

        private DateTime CalculateEndTime(DateTime startTime, DateTime? endTime)
        {
            if (endTime != null) return (DateTime)endTime;

            // Ensure startTime is treated as UTC
            startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);

            TimeSpan startTimeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                // For UTC timezone, no conversion needed
                startTimeOfDay = startTime.TimeOfDay;
            }
            else
            {
                // Convert UTC start time to provider's timezone to find matching slot
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                startTimeOfDay = startTimeInProviderZone.TimeOfDay;
            }

            var slot = configuration.CurrentValue.Slots.Find(s => s.StartTime == startTimeOfDay)
                ?? throw new ArgumentOutOfRangeException(nameof(startTime), "Start time does not fit into any slot.");

            if (timeZoneId == "UTC")
            {
                // For UTC timezone, create end time directly
                return startTime.Date.Add(slot.EndTime);
            }
            else
            {
                // Create end time in provider's timezone and convert back to UTC
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                var endTimeInProviderZone = startTimeInProviderZone.Date.Add(slot.EndTime);
                return TimeZoneInfo.ConvertTimeToUtc(endTimeInProviderZone, providerTimeZone);
            }
        }

        /// <summary>
        /// Checks if the date/time is blocked
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <param name="currentUser"></param>
        /// <returns>true if date/time is blocked and user is not carwash admin</returns>
        private async Task<bool> IsBlocked(DateTime startTime, DateTime? endTime, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(startTime);
            ArgumentNullException.ThrowIfNull(endTime);

            if (currentUser.IsCarwashAdmin) return false;

            if (await context.Blocker.AnyAsync(b => b.StartDate < startTime && b.EndDate > endTime))
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Cannot reserve for blocked slots.",
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() ?? "" }
                    });

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether end time is later than start time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <param name="currentUser"></param>
        /// <returns>true if end time is later than start time</returns>
        private bool IsEndTimeLaterThanStartTime(DateTime startTime, DateTime? endTime, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(startTime);
            ArgumentNullException.ThrowIfNull(endTime);

            if (startTime < endTime) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation end time should be later than the start time.",
                SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() ?? "" }
                });

            return false;
        }

        /// <summary>
        /// Check if there is enough time in that slot
        /// </summary>
        /// <param name="dateTime">Date and time of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <param name="currentUser"></param>
        /// <param name="excludeReservationId"></param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private async Task<bool> IsEnoughTimeInSlotAsync(DateTime dateTime, int timeRequirement, User currentUser, string? excludeReservationId = null)
        {
            if (currentUser.IsCarwashAdmin) return true;

            var query = context.Reservation.Where(r => r.StartDate == dateTime);

            // Exclude the current reservation if this is an update
            if (!string.IsNullOrEmpty(excludeReservationId))
            {
                query = query.Where(r => r.Id != excludeReservationId);
            }

            var reservedTimeInSlot = await query.SumAsync(r => r.TimeRequirement);

            // Ensure dateTime is treated as UTC
            if (dateTime.Kind == DateTimeKind.Unspecified)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            TimeSpan timeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                // For UTC timezone, no conversion needed
                timeOfDay = dateTime.TimeOfDay;
            }
            else
            {
                // Convert UTC dateTime to provider's timezone to find matching slot
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var dateTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(dateTime, providerTimeZone);
                timeOfDay = dateTimeInProviderZone.TimeOfDay;
            }

            var slotCapacity = configuration.CurrentValue.Slots.Find(s => s.StartTime == timeOfDay)?.Capacity;
            if (reservedTimeInSlot + timeRequirement <= slotCapacity * configuration.CurrentValue.Reservation.TimeUnit) return true;

            telemetryClient.TrackTrace(
                "BadRequest: There is not enough time in that slot.",
                SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", dateTime.ToString() },
                    { "reservedTimeInSlot", reservedTimeInSlot.ToString() },
                    { "TimeRequirement", timeRequirement.ToString() },
                    { "slotCapacity", slotCapacity.ToString() ?? "" },
                });

            return false;
        }

        /// <summary>
        /// Checks if there is enough time on that day
        /// </summary>
        /// <param name="date">Date of reservation</param>
        /// <param name="timeRequirement">time requirement of the reservation in minutes</param>
        /// <param name="currentUser"></param>
        /// <param name="excludeReservationId"></param>
        /// <returns>true if there is enough time left or user is carwash admin</returns>
        private async Task<bool> IsEnoughTimeOnDateAsync(DateTime date, int timeRequirement, User currentUser, string? excludeReservationId = null)
        {
            if (currentUser.IsCarwashAdmin) return true;

            var userCompanyLimit = (await context.Company.SingleAsync(c => c.Name == currentUser.Company)).DailyLimit;

            // Do not validate against company limit after {HoursAfterCompanyLimitIsNotChecked} for today
            // or if company limit is 0 (meaning unlimited)
            if ((date.Date == DateTime.UtcNow.Date && DateTime.UtcNow.Hour >= configuration.CurrentValue.Reservation.HoursAfterCompanyLimitIsNotChecked)
                || userCompanyLimit == 0)
            {
                var allSlotCapacity = configuration.CurrentValue.Slots.Sum(s => s.Capacity);

                var query = context.Reservation.Where(r => r.StartDate.Date == date.Date);
                if (!string.IsNullOrEmpty(excludeReservationId))
                {
                    query = query.Where(r => r.Id != excludeReservationId);
                }
                var reservedTimeOnDate = await query.SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > allSlotCapacity * configuration.CurrentValue.Reservation.TimeUnit)
                {
                    telemetryClient.TrackTrace(
                        "BadRequest: There is not enough time on this day at all.",
                        SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", currentUser.Id },
                            { "StartDate", date.ToString() },
                            { "ReservedTimeOnDate", reservedTimeOnDate.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "AllSlotCapacity", allSlotCapacity.ToString() },
                        });

                    return false;
                }
            }
            else
            {
                var query = context.Reservation
                    .Where(r => r.StartDate.Date == date.Date && r.User.Company == currentUser.Company);
                if (!string.IsNullOrEmpty(excludeReservationId))
                {
                    query = query.Where(r => r.Id != excludeReservationId);
                }
                var reservedTimeOnDate = await query.SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > userCompanyLimit * configuration.CurrentValue.Reservation.TimeUnit)
                {
                    telemetryClient.TrackTrace(
                        "BadRequest: Company limit has been reached.",
                        SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", currentUser.Id },
                            { "StartDate", date.ToString() },
                            { "ReservedTimeOnDate", reservedTimeOnDate.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "UserCompanyLimit", userCompanyLimit.ToString() },
                        });

                    return false;
                }
            }

            if (date.Date == DateTime.UtcNow.Date)
            {
                var query = context.Reservation
                    .Where(r => r.StartDate >= DateTime.UtcNow && r.StartDate.Date == DateTime.UtcNow.Date);
                if (!string.IsNullOrEmpty(excludeReservationId))
                {
                    query = query.Where(r => r.Id != excludeReservationId);
                }
                var toBeDoneTodayTime = await query.SumAsync(r => r.TimeRequirement);

                var remainingSlotCapacityToday = RemainingSlotCapacityToday;
                if (toBeDoneTodayTime + timeRequirement > remainingSlotCapacityToday * configuration.CurrentValue.Reservation.TimeUnit)
                {
                    telemetryClient.TrackTrace(
                        "BadRequest: Company limit has been reached.",
                        SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", currentUser.Id },
                            { "StartDate", date.ToString() },
                            { "ToBeDoneTodayTime", toBeDoneTodayTime.ToString() },
                            { "TimeRequirement", timeRequirement.ToString() },
                            { "RemainingSlotCapacityToday", remainingSlotCapacityToday.ToString() },
                        });

                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether start or end times are before the earliest allowed time
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <param name="currentUser"></param>
        /// <returns>true if start and end times are both after the earliest allowed time</returns>
        private bool IsInPast(DateTime startTime, DateTime? endTime, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(startTime);
            ArgumentNullException.ThrowIfNull(endTime);

            if (currentUser.IsCarwashAdmin) return false;

            var earliestTimeAllowed = DateTime.UtcNow.AddMinutes(configuration.CurrentValue.Reservation.MinutesToAllowReserveInPast * -1);

            if (startTime < earliestTimeAllowed || endTime < earliestTimeAllowed)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Reservation time range should be located entirely after the earliest allowed time.",
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() ?? "" },
                        { "EarliestTimeAllowed", earliestTimeAllowed.ToString() }
                    });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether start and end times fit into a slot
        /// </summary>
        /// <param name="startTime">start date and time (UTC)</param>
        /// <param name="endTime">end date and time (UTC)</param>
        /// <param name="currentUser"></param>
        /// <returns>true if start and end times fit into a slot</returns>
        private bool IsInSlot(DateTime startTime, DateTime? endTime, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(startTime);
            ArgumentNullException.ThrowIfNull(endTime);

            // Ensure DateTime objects are treated as UTC
            if (startTime.Kind == DateTimeKind.Unspecified)
                startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);

            var endTimeValue = (DateTime)endTime;
            if (endTimeValue.Kind == DateTimeKind.Unspecified)
                endTimeValue = DateTime.SpecifyKind(endTimeValue, DateTimeKind.Utc);

            TimeSpan startTimeOfDay, endTimeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                // For UTC timezone, no conversion needed
                startTimeOfDay = startTime.TimeOfDay;
                endTimeOfDay = endTimeValue.TimeOfDay;
            }
            else
            {
                // Convert UTC times to provider's timezone for slot validation
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                var endTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(endTimeValue, providerTimeZone);

                startTimeOfDay = startTimeInProviderZone.TimeOfDay;
                endTimeOfDay = endTimeInProviderZone.TimeOfDay;
            }

            if (configuration.CurrentValue.Slots.Any(s => s.StartTime == startTimeOfDay && s.EndTime == endTimeOfDay)) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should fit into a slot.",
                SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() ?? "" },
                    { "StartTimeOfDay", startTimeOfDay.ToString() },
                    { "EndTimeOfDay", endTimeOfDay.ToString() }
                });

            return false;
        }

        /// <summary>
        /// Checks whether start and end times are on the same day
        /// </summary>
        /// <param name="startTime">start date and time</param>
        /// <param name="endTime">end date and time</param>
        /// <param name="currentUser"></param>
        /// <returns>true if start and end times are on the same day</returns>
        private bool IsStartAndEndTimeOnSameDay(DateTime startTime, DateTime? endTime, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(startTime);
            ArgumentNullException.ThrowIfNull(endTime);

            if (startTime.Date == ((DateTime)endTime).Date) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should be located entirely on the same day.",
                SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() ?? "" }
                });

            return false;
        }

        /// <summary>
        /// Checks whether user has met the active concurrent reservation limit: <see cref="CarWashConfiguration.ReservationSettings.UserConcurrentReservationLimit"/>
        /// </summary>
        /// <returns>true if user has met the limit and is not admin</returns>
        private async Task<bool> IsUserConcurrentReservationLimitMetAsync(User currentUser)
        {
            if (currentUser.IsAdmin || currentUser.IsCarwashAdmin) return false;

            var activeReservationCount = await context.Reservation.Where(r => r.UserId == currentUser.Id && r.State != State.Done).CountAsync();

            if (activeReservationCount >= configuration.CurrentValue.Reservation.UserConcurrentReservationLimit)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: User has met the active concurrent reservation limit.",
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "ActiveReservationCount", activeReservationCount.ToString() },
                        { "UserConcurrentReservationLimit", configuration.CurrentValue.Reservation.UserConcurrentReservationLimit.ToString() }
                    });

                return true;
            }

            return false;
        }
    }
}