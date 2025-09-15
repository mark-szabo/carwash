using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Models.ViewModels;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OfficeOpenXml;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Service for managing reservation business logic and operations
    /// </summary>
    public class ReservationService(
        ApplicationDbContext context,
        IOptionsMonitor<CarWashConfiguration> configuration,
        IEmailService emailService,
        ICalendarService calendarService,
        IPushService pushService,
        IBotService botService,
        TelemetryClient telemetryClient) : IReservationService
    {
        /// <inheritdoc />
        public async Task<ValidationResult> ValidateReservationAsync(Reservation reservation, bool isUpdate, User currentUser, string? excludeReservationId = null)
        {
            // Input validation
            if (reservation.Services == null || reservation.Services.Count == 0)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: No service chosen.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Services", reservation.ServicesJson }
                    });
                return new ValidationResult { IsValid = false, ErrorMessage = "No service chosen." };
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
                    return new ValidationResult { IsValid = false, ErrorMessage = "Reservation can be made to slots only." };
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
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", reservation.UserId },
                            { "IsAdmin", currentUser.IsAdmin.ToString() },
                            { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                            { "CreatedById", currentUser.Id }
                        });
                    throw new UnauthorizedAccessException("User cannot reserve in the name of others unless admin.");
                }

                if (reservation.UserId != null)
                {
                    var reservationUser = await context.Users.FindAsync(reservation.UserId);
                    if (currentUser.IsAdmin && reservationUser?.Company != currentUser.Company)
                    {
                        telemetryClient.TrackTrace(
                            "Forbid: Admin cannot reserve in the name of other companies' users.",
                            Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                            new Dictionary<string, string>
                            {
                                { "UserId", reservation.UserId },
                                { "IsAdmin", currentUser.IsAdmin.ToString() },
                                { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                                { "CreatedById", currentUser.Id }
                            });
                        throw new UnauthorizedAccessException("Admin cannot reserve in the name of other companies' users.");
                    }
                }
            }

            // Time and business validations
            if (!IsStartAndEndTimeOnSameDay(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "Reservation time range should be located entirely on the same day." };

            if (!IsEndTimeLaterThanStartTime(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "Reservation end time should be later than the start time." };

            if (IsInPast(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "Cannot reserve in the past." };

            if (!IsInSlot(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "Reservation can be made to slots only." };

            if (!isUpdate && await IsUserConcurrentReservationLimitMetAsync(currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = $"Cannot have more than {configuration.CurrentValue.Reservation.UserConcurrentReservationLimit} concurrent active reservations." };

            if (await IsBlocked(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "This time is blocked." };

            if (!await IsEnoughTimeOnDateAsync(reservation.StartDate, timeRequirement, currentUser, excludeReservationId))
                return new ValidationResult { IsValid = false, ErrorMessage = "Company limit has been met for this day or there is not enough time at all." };

            if (!await IsEnoughTimeInSlotAsync(reservation.StartDate, timeRequirement, currentUser, excludeReservationId))
                return new ValidationResult { IsValid = false, ErrorMessage = "There is not enough time in that slot." };

            return new ValidationResult { IsValid = true };
        }

        /// <inheritdoc />
        public async Task<Reservation> CreateReservationAsync(Reservation reservation, User currentUser, bool dropoffConfirmed = false)
        {
            // Set defaults
            reservation.UserId ??= currentUser.Id;
            reservation.State = State.SubmittedNotActual;
            reservation.Mpv = false;
            reservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty);
            reservation.CreatedById = currentUser.Id;
            reservation.CreatedOn = DateTime.UtcNow;

            if (reservation.Comments.Count == 1)
            {
                reservation.Comments[0].UserId = currentUser.Id;
                reservation.Comments[0].Timestamp = DateTime.UtcNow;
                reservation.Comments[0].Role = CommentRole.User;
            }
            if (reservation.Comments.Count > 1)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Only one comment can be added when creating a reservation.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Comments", reservation.CommentsJson }
                    });
                throw new ArgumentException("Only one comment can be added when creating a reservation.");
            }

            // EndDate should already be calculated during validation

            if (dropoffConfirmed)
            {
                if (reservation.Location == null)
                {
                    telemetryClient.TrackTrace(
                        "BadRequest: Location must be set if drop-off pre-confirmed.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", reservation.UserId },
                            { "Location", reservation.Location }
                        });
                    throw new ArgumentException("Location must be set if drop-off pre-confirmed.");
                }
                reservation.State = State.DropoffAndLocationConfirmed;
            }

            // Time requirement calculation
            reservation.TimeRequirement = reservation.Services.Contains(Constants.ServiceType.Carpet) ?
                configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * configuration.CurrentValue.Reservation.TimeUnit :
                configuration.CurrentValue.Reservation.TimeUnit;

            // Check if MPV
            reservation.Mpv = await IsMpvAsync(reservation.VehiclePlateNumber);

            // Create calendar event
            await CreateCalendarEventAsync(reservation, currentUser);

            context.Reservation.Add(reservation);
            await context.SaveChangesAsync();

            // Track event
            telemetryClient.TrackEvent(
                "New reservation was submitted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return reservation;
        }

        /// <inheritdoc />
        public async Task<Reservation> UpdateReservationAsync(Reservation reservation, User currentUser, bool dropoffConfirmed = false)
        {
            var dbReservation = await context.Reservation.FindAsync(reservation.Id);
            if (dbReservation == null)
                throw new InvalidOperationException("Reservation not found.");

            // Update basic properties
            dbReservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty);
            dbReservation.Location = reservation.Location;
            dbReservation.Services = reservation.Services;
            dbReservation.Private = reservation.Private;
            var oldStartDate = dbReservation.StartDate;
            var newStartDate = reservation.StartDate;
            dbReservation.StartDate = reservation.StartDate;
            if (reservation.EndDate != null) dbReservation.EndDate = (DateTime)reservation.EndDate;
            // EndDate should already be calculated during validation

            if (dropoffConfirmed)
            {
                if (dbReservation.Location == null)
                    throw new ArgumentException("Location must be set if drop-off pre-confirmed.");
                dbReservation.State = State.DropoffAndLocationConfirmed;
            }

            // Update user if changed
            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                    throw new UnauthorizedAccessException("Cannot update reservation for other users.");

                if (reservation.UserId != null)
                {
                    var reservationUser = await context.Users.FindAsync(reservation.UserId);
                    if (currentUser.IsAdmin && reservationUser?.Company != currentUser.Company)
                        throw new UnauthorizedAccessException("Cannot update reservation for users from other companies.");

                    dbReservation.UserId = reservation.UserId;
                }
            }

            // Time requirement calculation
            dbReservation.TimeRequirement = dbReservation.Services.Contains(Constants.ServiceType.Carpet) ?
                configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * configuration.CurrentValue.Reservation.TimeUnit :
                configuration.CurrentValue.Reservation.TimeUnit;

            // Check if MPV
            dbReservation.Mpv = dbReservation.Mpv || await IsMpvAsync(dbReservation.VehiclePlateNumber);

            // Update calendar event if date changed
            if (dbReservation.UserId == currentUser.Id && currentUser.CalendarIntegration && newStartDate != oldStartDate)
            {
                dbReservation.User = currentUser;
                dbReservation.OutlookEventId = await calendarService.UpdateEventAsync(dbReservation);
            }

            await context.SaveChangesAsync();

            // Track event
            telemetryClient.TrackEvent(
                "Reservation was updated.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", dbReservation.Id },
                    { "Reservation user ID", dbReservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return dbReservation;
        }

        /// <inheritdoc />
        public async Task<Reservation> DeleteReservationAsync(string reservationId, User currentUser)
        {
            var reservation = await context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id && !currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Cannot delete reservation for other users.");

            context.Reservation.Remove(reservation);
            await context.SaveChangesAsync();

            // Delete calendar event
            await calendarService.DeleteEventAsync(reservation);

            // Track event
            telemetryClient.TrackEvent(
                "Reservation was deleted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return reservation;
        }

        /// <inheritdoc />
        public async Task ConfirmDropoffAsync(string reservationId, string location, User currentUser)
        {
            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("Reservation location cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id && !currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Cannot confirm dropoff for other users.");

            reservation.State = State.DropoffAndLocationConfirmed;
            reservation.Location = location;

            await context.SaveChangesAsync();

            // Track event
            telemetryClient.TrackEvent(
                "Key dropoff was confirmed by user.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });
        }

        /// <inheritdoc />
        public async Task StartWashAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can start wash.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = State.WashInProgress;
            await context.SaveChangesAsync();

            // Send bot message
            await botService.SendWashStartedMessageAsync(reservation);
        }

        /// <inheritdoc />
        public async Task CompleteWashAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can complete wash.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .SingleOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = reservation.Private ? State.NotYetPaid : State.Done;
            await context.SaveChangesAsync();

            // Send notification
            await SendCompletionNotificationAsync(reservation);

            // Send bot message
            await botService.SendWashCompletedMessageAsync(reservation);
        }

        /// <inheritdoc />
        public async Task ConfirmPaymentAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can confirm payment.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.State != State.NotYetPaid)
                throw new InvalidOperationException("Reservation state is not 'Not yet paid'.");

            reservation.State = State.Done;
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task SetReservationStateAsync(string reservationId, State state, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can set reservation state.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = state;
            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task AddCommentAsync(string reservationId, string comment, User currentUser)
        {
            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");
            if (string.IsNullOrEmpty(comment))
                throw new ArgumentException("Comment cannot be null.");

            var reservation = await context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                    throw new UnauthorizedAccessException("Cannot add comment to other users' reservations.");

                if (currentUser.IsAdmin && reservation.User.Company != currentUser.Company)
                    throw new UnauthorizedAccessException("Cannot add comment to reservations from other companies.");
            }

            reservation.AddComment(new Comment
            {
                UserId = currentUser.Id,
                Role = reservation.UserId != currentUser.Id && currentUser.IsCarwashAdmin ? CommentRole.Carwash : CommentRole.User,
                Timestamp = DateTime.UtcNow,
                Message = comment
            });

            await context.SaveChangesAsync();

            // Send notification if CarWash admin adds comment
            if (currentUser.IsCarwashAdmin)
            {
                await SendCommentNotificationAsync(reservation, comment);
                await botService.SendCarWashCommentLeftMessageAsync(reservation);
            }
        }

        /// <inheritdoc />
        public async Task<bool> IsMpvAsync(string vehiclePlateNumber)
        {
            return await context.Reservation
                .OrderByDescending(r => r.StartDate)
                .Where(r => r.VehiclePlateNumber == vehiclePlateNumber)
                .Select(r => r.Mpv)
                .FirstOrDefaultAsync();
        }

        #region Private Helper Methods

        private DateTime CalculateEndTime(DateTime startTime, DateTime? endTime)
        {
            if (endTime != null) return (DateTime)endTime;

            startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);

            TimeSpan startTimeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                startTimeOfDay = startTime.TimeOfDay;
            }
            else
            {
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                startTimeOfDay = startTimeInProviderZone.TimeOfDay;
            }

            var slot = configuration.CurrentValue.Slots.Find(s => s.StartTime == startTimeOfDay);
            if (slot == null) throw new ArgumentOutOfRangeException(nameof(startTime), "Start time does not fit into any slot.");

            if (timeZoneId == "UTC")
            {
                return startTime.Date.Add(slot.EndTime);
            }
            else
            {
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                var endTimeInProviderZone = startTimeInProviderZone.Date.Add(slot.EndTime);
                return TimeZoneInfo.ConvertTimeToUtc(endTimeInProviderZone, providerTimeZone);
            }
        }

        private bool IsStartAndEndTimeOnSameDay(DateTime startTime, DateTime? endTime, User currentUser)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (startTime.Date == ((DateTime)endTime).Date) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should be located entirely on the same day.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() }
                });

            return false;
        }

        private bool IsEndTimeLaterThanStartTime(DateTime startTime, DateTime? endTime, User currentUser)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (startTime < endTime) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation end time should be later than the start time.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() }
                });

            return false;
        }

        private bool IsInPast(DateTime startTime, DateTime? endTime, User currentUser)
        {
            if (currentUser.IsCarwashAdmin) return false;

            if (endTime == null) throw new ArgumentNullException(nameof(endTime));
            var earliestTimeAllowed = DateTime.UtcNow.AddMinutes(configuration.CurrentValue.Reservation.MinutesToAllowReserveInPast * -1);

            if (startTime < earliestTimeAllowed || endTime < earliestTimeAllowed)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Reservation time range should be located entirely after the earliest allowed time.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() },
                        { "EarliestTimeAllowed", earliestTimeAllowed.ToString() }
                    });
                return true;
            }

            return false;
        }

        private bool IsInSlot(DateTime startTime, DateTime? endTime, User currentUser)
        {
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            if (startTime.Kind == DateTimeKind.Unspecified)
                startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);

            var endTimeValue = (DateTime)endTime;
            if (endTimeValue.Kind == DateTimeKind.Unspecified)
                endTimeValue = DateTime.SpecifyKind(endTimeValue, DateTimeKind.Utc);

            TimeSpan startTimeOfDay, endTimeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                startTimeOfDay = startTime.TimeOfDay;
                endTimeOfDay = endTimeValue.TimeOfDay;
            }
            else
            {
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var startTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(startTime, providerTimeZone);
                var endTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(endTimeValue, providerTimeZone);

                startTimeOfDay = startTimeInProviderZone.TimeOfDay;
                endTimeOfDay = endTimeInProviderZone.TimeOfDay;
            }

            if (configuration.CurrentValue.Slots.Any(s => s.StartTime == startTimeOfDay && s.EndTime == endTimeOfDay)) return true;

            telemetryClient.TrackTrace(
                "BadRequest: Reservation time range should fit into a slot.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", startTime.ToString() },
                    { "EndDate", endTime.ToString() },
                    { "StartTimeOfDay", startTimeOfDay.ToString() },
                    { "EndTimeOfDay", endTimeOfDay.ToString() }
                });

            return false;
        }

        private async Task<bool> IsUserConcurrentReservationLimitMetAsync(User currentUser)
        {
            if (currentUser.IsAdmin || currentUser.IsCarwashAdmin) return false;

            var activeReservationCount = await context.Reservation.Where(r => r.UserId == currentUser.Id && r.State != State.Done).CountAsync();

            if (activeReservationCount >= configuration.CurrentValue.Reservation.UserConcurrentReservationLimit)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: User has met the active concurrent reservation limit.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
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

        private async Task<bool> IsEnoughTimeOnDateAsync(DateTime date, int timeRequirement, User currentUser, string? excludeReservationId = null)
        {
            if (currentUser.IsCarwashAdmin) return true;

            var userCompanyLimit = (await context.Company.SingleAsync(c => c.Name == currentUser.Company)).DailyLimit;

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
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
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
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
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

                var remainingSlotCapacityToday = GetRemainingSlotCapacityToday();
                if (toBeDoneTodayTime + timeRequirement > remainingSlotCapacityToday * configuration.CurrentValue.Reservation.TimeUnit)
                {
                    telemetryClient.TrackTrace(
                        "BadRequest: Company limit has been reached.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
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

            if (dateTime.Kind == DateTimeKind.Unspecified)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            TimeSpan timeOfDay;
            var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;

            if (timeZoneId == "UTC")
            {
                timeOfDay = dateTime.TimeOfDay;
            }
            else
            {
                var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                var dateTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(dateTime, providerTimeZone);
                timeOfDay = dateTimeInProviderZone.TimeOfDay;
            }

            var slotCapacity = configuration.CurrentValue.Slots.Find(s => s.StartTime == timeOfDay)?.Capacity;
            if (reservedTimeInSlot + timeRequirement <= slotCapacity * configuration.CurrentValue.Reservation.TimeUnit) return true;

            telemetryClient.TrackTrace(
                "BadRequest: There is not enough time in that slot.",
                Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                new Dictionary<string, string>
                {
                    { "UserId", currentUser.Id },
                    { "StartDate", dateTime.ToString() },
                    { "reservedTimeInSlot", reservedTimeInSlot.ToString() },
                    { "TimeRequirement", timeRequirement.ToString() },
                    { "slotCapacity", slotCapacity.ToString() },
                });

            return false;
        }

        private async Task<bool> IsBlocked(DateTime startTime, DateTime? endTime, User currentUser)
        {
            if (currentUser.IsCarwashAdmin) return false;

            if (await context.Blocker.AnyAsync(b => b.StartDate < startTime && b.EndDate > endTime))
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Cannot reserve for blocked slots.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "StartDate", startTime.ToString() },
                        { "EndDate", endTime.ToString() }
                    });

                return true;
            }

            return false;
        }

        private int GetRemainingSlotCapacityToday()
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

        private async Task CreateCalendarEventAsync(Reservation reservation, User currentUser)
        {
            if (reservation.UserId == currentUser.Id)
            {
                if (currentUser.CalendarIntegration)
                {
                    reservation.User = currentUser;
                    reservation.OutlookEventId = await calendarService.CreateEventAsync(reservation);
                }
            }
            else
            {
                var user = await context.Users.FindAsync(reservation.UserId);
                if (user?.CalendarIntegration == true)
                {
                    var timer = DateTime.UtcNow;
                    try
                    {
                        reservation.User = user;
                        reservation.OutlookEventId = await calendarService.CreateEventAsync(reservation);
                        telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: true);
                    }
                    catch (Exception e)
                    {
                        telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: false);
                        telemetryClient.TrackException(e);
                    }
                }
            }
        }

        private async Task SendCompletionNotificationAsync(Reservation reservation)
        {
            switch (reservation.User.NotificationChannel)
            {
                case NotificationChannel.Disabled:
                    break;
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}" + (reservation.KeyLockerBox != null ? $" (Locker: {reservation.KeyLockerBox.Name})" : ""),
                        Tag = NotificationTag.Done
                    };
                    try
                    {
                        await pushService.Send(reservation.UserId, notification);
                        break;
                    }
                    catch (PushService.NoActivePushSubscriptionException)
                    {
                        reservation.User.NotificationChannel = NotificationChannel.Email;
                        await context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        telemetryClient.TrackException(e);
                    }
                    goto case NotificationChannel.Email;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email,
                        Subject = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}" + (reservation.KeyLockerBox != null ? $" (Locker: {reservation.KeyLockerBox.Name})" : ""),
                    };
                    await emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task SendCommentNotificationAsync(Reservation reservation, string comment)
        {
            switch (reservation.User.NotificationChannel)
            {
                case NotificationChannel.Disabled:
                    break;
                case NotificationChannel.Push:
                    var notification = new Notification
                    {
                        Title = "CarWash has left a comment on your reservation.",
                        Body = comment,
                        Tag = NotificationTag.Comment
                    };
                    try
                    {
                        await pushService.Send(reservation.UserId, notification);
                        break;
                    }
                    catch (PushService.NoActivePushSubscriptionException)
                    {
                        reservation.User.NotificationChannel = NotificationChannel.Email;
                        await context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        telemetryClient.TrackException(e);
                    }
                    goto case NotificationChannel.Email;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email,
                        Subject = "CarWash has left a comment on your reservation",
                        Body = comment + $"\n\n<a href=\"{configuration.CurrentValue.ConnectionStrings.BaseUrl}\">Reply</a>\n\nPlease do not reply to this email, messages to service providers can only be sent within the app. Kindly log in to your account and communicate directly through the in-app messaging feature.",
                    };
                    await emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        public async Task SetMpvAsync(string reservationId, bool mpv, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can set MPV flag");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found");

            reservation.Mpv = mpv;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Reservation.AnyAsync(e => e.Id == reservationId))
                    throw new InvalidOperationException("Reservation not found");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateServicesAsync(string reservationId, List<int> services, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can update services");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            if (services == null)
                throw new ArgumentException("Services param cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found");

            reservation.Services = services;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Reservation.AnyAsync(e => e.Id == reservationId))
                    throw new InvalidOperationException("Reservation not found");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateLocationAsync(string reservationId, string location, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can update location");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("New location cannot be null.");

            var reservation = await context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found");

            reservation.Location = location;

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Reservation.AnyAsync(e => e.Id == reservationId))
                    throw new InvalidOperationException("Reservation not found");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<NotAvailableDatesAndTimesViewModel> GetNotAvailableDatesAndTimesAsync(User currentUser, int daysAhead = 365)
        {
            if (currentUser.IsCarwashAdmin)
                return new NotAvailableDatesAndTimesViewModel([], []);

            #region Get not available dates
            var notAvailableDates = new List<DateTime>();
            var dailyCapacity = configuration.CurrentValue.Slots.Sum(s => s.Capacity);
            var userCompanyLimit = (await context.Company.SingleAsync(c => c.Name == currentUser.Company)).DailyLimit;

            notAvailableDates.AddRange(await context.Reservation
                .Where(r => r.EndDate >= DateTime.UtcNow && r.StartDate <= DateTime.UtcNow.AddDays(daysAhead))
                .GroupBy(r => r.StartDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .Where(d => d.TimeSum >= dailyCapacity * configuration.CurrentValue.Reservation.TimeUnit)
                .Select(d => d.Date)
                .ToListAsync());

            if (!notAvailableDates.Contains(DateTime.UtcNow.Date))
            {
                var toBeDoneTodayTime = await context.Reservation
                    .Where(r => r.StartDate >= DateTime.UtcNow && r.StartDate.Date == DateTime.UtcNow.Date)
                    .SumAsync(r => r.TimeRequirement);

                if (toBeDoneTodayTime >= GetRemainingSlotCapacityToday() * configuration.CurrentValue.Reservation.TimeUnit)
                    notAvailableDates.Add(DateTime.UtcNow.Date);
            }

            // If the company has set up limits.
            if (userCompanyLimit > 0)
            {
                notAvailableDates.AddRange(await context.Reservation
                    .Where(r => r.EndDate >= DateTime.UtcNow && r.StartDate <= DateTime.UtcNow.AddDays(daysAhead))
                    .Where(r => r.User.Company == currentUser.Company)
                    .GroupBy(r => r.StartDate.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        TimeSum = g.Sum(r => r.TimeRequirement)
                    })
                    .Where(d => d.TimeSum >= userCompanyLimit * configuration.CurrentValue.Reservation.TimeUnit)
                    .Select(d => d.Date)
                    .ToListAsync());
            }
            #endregion

            #region Get not available times
            var slotReservationAggregate = await context.Reservation
                .Where(r => r.EndDate >= DateTime.UtcNow && r.StartDate <= DateTime.UtcNow.AddDays(daysAhead))
                .GroupBy(r => r.StartDate)
                .Select(g => new
                {
                    DateTime = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .ToListAsync();

            var notAvailableTimes = slotReservationAggregate
                .Where(d =>
                {
                    // Convert UTC DateTime to provider's timezone to find matching slot
                    var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;
                    TimeSpan timeOfDay;

                    if (timeZoneId == "UTC")
                    {
                        timeOfDay = d.DateTime.TimeOfDay;
                    }
                    else
                    {
                        var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                        var dateTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(d.DateTime, providerTimeZone);
                        timeOfDay = dateTimeInProviderZone.TimeOfDay;
                    }

                    var slotCapacity = configuration.CurrentValue.Slots.Find(s => s.StartTime == timeOfDay)?.Capacity;
                    return slotCapacity.HasValue && d.TimeSum >= slotCapacity.Value * configuration.CurrentValue.Reservation.TimeUnit;
                })
                .Select(d => d.DateTime)
                .ToList();
            #endregion

            return new NotAvailableDatesAndTimesViewModel(
                Dates: notAvailableDates.Distinct().Select(DateOnly.FromDateTime),
                Times: notAvailableTimes);
        }

        /// <inheritdoc />
        public async Task<LastSettingsViewModel?> GetLastSettingsAsync(User currentUser)
        {
            var lastReservation = await context.Reservation
                .Where(r => r.UserId == currentUser.Id)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefaultAsync();

            if (lastReservation == null)
                return null;

            return new LastSettingsViewModel(
                VehiclePlateNumber: lastReservation.VehiclePlateNumber,
                Location: lastReservation.Location ?? "",
                Services: lastReservation.Services ?? []);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ReservationCapacityViewModel>> GetReservationCapacityAsync(DateTime date)
        {
            var slotReservationAggregate = await context.Reservation
                .Where(r => r.StartDate.Date == date.Date)
                .GroupBy(r => r.StartDate)
                .Select(g => new
                {
                    DateTime = g.Key,
                    TimeSum = g.Sum(r => r.TimeRequirement)
                })
                .ToListAsync();

            var slotFreeCapacity = new List<ReservationCapacityViewModel>();
            foreach (var a in slotReservationAggregate)
            {
                // Convert UTC DateTime to provider's timezone to find matching slot
                var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;
                TimeSpan timeOfDay;

                if (timeZoneId == "UTC")
                {
                    timeOfDay = a.DateTime.TimeOfDay;
                }
                else
                {
                    var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    var dateTimeInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(a.DateTime, providerTimeZone);
                    timeOfDay = dateTimeInProviderZone.TimeOfDay;
                }

                var slotCapacity = configuration.CurrentValue.Slots.Find(s => s.StartTime == timeOfDay)?.Capacity;
                if (slotCapacity == null) continue;
                var reservedCapacity = (int)Math.Ceiling((double)a.TimeSum / configuration.CurrentValue.Reservation.TimeUnit);
                slotFreeCapacity.Add(new ReservationCapacityViewModel(
                    StartTime: a.DateTime,
                    FreeCapacity: slotCapacity.Value - reservedCapacity));
            }

            // Add slots without any reservations
            foreach (var slot in configuration.CurrentValue.Slots)
            {
                DateTime slotDateTime;

                if (configuration.CurrentValue.Reservation.TimeZone == "UTC")
                {
                    slotDateTime = date.Date.Add(slot.StartTime);
                }
                else
                {
                    var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(configuration.CurrentValue.Reservation.TimeZone);
                    var dateInProviderZone = date.Date;
                    var slotDateTimeInProviderZone = dateInProviderZone.Add(slot.StartTime);
                    slotDateTime = TimeZoneInfo.ConvertTimeToUtc(slotDateTimeInProviderZone, providerTimeZone);
                }

                if (!slotFreeCapacity.Any(s => s.StartTime == slotDateTime))
                {
                    slotFreeCapacity.Add(new ReservationCapacityViewModel(
                        StartTime: slotDateTime,
                        FreeCapacity: slot.Capacity));
                }
            }

            return slotFreeCapacity.OrderBy(s => s.StartTime);
        }

        /// <inheritdoc />
        public async Task<byte[]> ExportReservationsAsync(User currentUser, DateTime? startDate = null, DateTime? endDate = null)
        {
            var startDateNonNull = startDate ?? DateTime.UtcNow.Date.AddMonths(-1);
            var endDateNonNull = endDate ?? DateTime.UtcNow.Date;

            List<Reservation> reservations;

            if (currentUser.IsCarwashAdmin)
                reservations = await context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.StartDate.Date >= startDateNonNull.Date && r.StartDate.Date <= endDateNonNull.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else if (currentUser.IsAdmin)
                reservations = await context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.User.Company == currentUser.Company && r.StartDate.Date >= startDateNonNull.Date &&
                                r.StartDate.Date <= endDateNonNull.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else
                throw new UnauthorizedAccessException("Only admins or carwash admins can export reservations");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            
            // Add a new worksheet to the empty workbook
            var worksheet = package.Workbook.Worksheets.Add($"{startDateNonNull.Year}-{startDateNonNull.Month}");

            // Add the headers
            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Start time";
            worksheet.Cells[1, 3].Value = "End time";
            worksheet.Cells[1, 4].Value = "Company";
            worksheet.Cells[1, 5].Value = "Name";
            worksheet.Cells[1, 6].Value = "Email";
            worksheet.Cells[1, 7].Value = "PhoneNumber";
            worksheet.Cells[1, 8].Value = "BillingName";
            worksheet.Cells[1, 9].Value = "BillingAddress";
            worksheet.Cells[1, 10].Value = "PaymentMethod";
            worksheet.Cells[1, 11].Value = "Vehicle plate number";
            worksheet.Cells[1, 12].Value = "MPV";
            worksheet.Cells[1, 13].Value = "Private";
            worksheet.Cells[1, 14].Value = "Services";
            worksheet.Cells[1, 15].Value = "Comment";
            worksheet.Cells[1, 16].Value = "Carwash comment";
            worksheet.Cells[1, 17].Value = "Price (computed)";

            // Add the data
            for (var i = 0; i < reservations.Count; i++)
            {
                var reservation = reservations[i];
                worksheet.Cells[i + 2, 1].Value = reservation.StartDate.ToString("yyyy-MM-dd");
                worksheet.Cells[i + 2, 2].Value = reservation.StartDate.ToString("HH:mm");
                worksheet.Cells[i + 2, 3].Value = reservation.EndDate?.ToString("HH:mm");
                worksheet.Cells[i + 2, 4].Value = reservation.User?.Company;
                worksheet.Cells[i + 2, 5].Value = reservation.User?.FullName;
                worksheet.Cells[i + 2, 6].Value = reservation.Private ? reservation.User?.Email : "";
                worksheet.Cells[i + 2, 7].Value = reservation.Private ? reservation.User?.PhoneNumber : "";
                worksheet.Cells[i + 2, 8].Value = reservation.Private ? reservation.User?.BillingName : "";
                worksheet.Cells[i + 2, 9].Value = reservation.Private ? reservation.User?.BillingAddress : "";
                worksheet.Cells[i + 2, 10].Value = reservation.Private ? reservation.User?.PaymentMethod.ToString() : "";
                worksheet.Cells[i + 2, 11].Value = reservation.VehiclePlateNumber;
                worksheet.Cells[i + 2, 12].Value = reservation.Mpv.ToString();
                worksheet.Cells[i + 2, 13].Value = reservation.Private.ToString();
                worksheet.Cells[i + 2, 14].Value = reservation.GetServiceNames(configuration.CurrentValue);
                worksheet.Cells[i + 2, 15].Value = reservation.CommentsJson;
                worksheet.Cells[i + 2, 16].Value = ""; // CarwashComment is not available - leaving empty
                worksheet.Cells[i + 2, 17].Value = reservation.GetPrice(configuration.CurrentValue);
            }

            return package.GetAsByteArray();
        }

        #endregion
    }
}