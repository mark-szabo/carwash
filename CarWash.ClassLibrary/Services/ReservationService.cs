using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Service for managing reservation business logic and operations
    /// </summary>
    public class ReservationService : IReservationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IOptionsMonitor<CarWashConfiguration> _configuration;
        private readonly IEmailService _emailService;
        private readonly ICalendarService _calendarService;
        private readonly IPushService _pushService;
        private readonly IBotService _botService;
        private readonly TelemetryClient _telemetryClient;

        public ReservationService(
            ApplicationDbContext context,
            IOptionsMonitor<CarWashConfiguration> configuration,
            IEmailService emailService,
            ICalendarService calendarService,
            IPushService pushService,
            IBotService botService,
            TelemetryClient telemetryClient)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _calendarService = calendarService;
            _pushService = pushService;
            _botService = botService;
            _telemetryClient = telemetryClient;
        }

        public async Task<ValidationResult> ValidateReservationAsync(Reservation reservation, bool isUpdate, User currentUser)
        {
            // Input validation
            if (reservation.Services == null || reservation.Services.Count == 0)
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: No service chosen.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Services", reservation.ServicesJson }
                    });
                return new ValidationResult { IsValid = false, ErrorMessage = "No service chosen." };
            }

            // Authorization validation
            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                {
                    _telemetryClient.TrackTrace(
                        "Forbid: User cannot reserve in the name of others unless admin.",
                        Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                        new Dictionary<string, string>
                        {
                            { "UserId", reservation.UserId },
                            { "IsAdmin", currentUser.IsAdmin.ToString() },
                            { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                            { "CreatedById", currentUser.Id }
                        });
                    return new ValidationResult { IsValid = false, ErrorMessage = "Cannot reserve for others without admin privileges." };
                }

                if (reservation.UserId != null)
                {
                    var reservationUser = await _context.Users.FindAsync(reservation.UserId);
                    if (currentUser.IsAdmin && reservationUser?.Company != currentUser.Company)
                    {
                        _telemetryClient.TrackTrace(
                            "Forbid: Admin cannot reserve in the name of other companies' users.",
                            Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                            new Dictionary<string, string>
                            {
                                { "UserId", reservation.UserId },
                                { "IsAdmin", currentUser.IsAdmin.ToString() },
                                { "IsCarwashAdmin", currentUser.IsCarwashAdmin.ToString() },
                                { "CreatedById", currentUser.Id }
                            });
                        return new ValidationResult { IsValid = false, ErrorMessage = "Cannot reserve for users from other companies." };
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
                return new ValidationResult { IsValid = false, ErrorMessage = $"Cannot have more than {_configuration.CurrentValue.Reservation.UserConcurrentReservationLimit} concurrent active reservations." };

            if (await IsBlocked(reservation.StartDate, reservation.EndDate, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "This time is blocked." };

            if (!await IsEnoughTimeOnDateAsync(reservation.StartDate, reservation.TimeRequirement, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "Company limit has been met for this day or there is not enough time at all." };

            if (!await IsEnoughTimeInSlotAsync(reservation.StartDate, reservation.TimeRequirement, currentUser))
                return new ValidationResult { IsValid = false, ErrorMessage = "There is not enough time in that slot." };

            return new ValidationResult { IsValid = true };
        }

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
                _telemetryClient.TrackTrace(
                    "BadRequest: Only one comment can be added when creating a reservation.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Comments", reservation.CommentsJson }
                    });
                throw new ArgumentException("Only one comment can be added when creating a reservation.");
            }

            try
            {
                reservation.EndDate = CalculateEndTime(reservation.StartDate, reservation.EndDate);
            }
            catch (ArgumentOutOfRangeException e)
            {
                _telemetryClient.TrackException(e, new Dictionary<string, string>
                {
                    { "CreatedById", reservation.CreatedById },
                    { "StartDate", reservation.StartDate.ToString() }
                });
                throw new ArgumentException("Reservation can be made to slots only.");
            }

            if (dropoffConfirmed)
            {
                if (reservation.Location == null)
                {
                    _telemetryClient.TrackTrace(
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
                _configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * _configuration.CurrentValue.Reservation.TimeUnit :
                _configuration.CurrentValue.Reservation.TimeUnit;

            // Check if MPV
            reservation.Mpv = await IsMpvAsync(reservation.VehiclePlateNumber);

            // Create calendar event
            await CreateCalendarEventAsync(reservation, currentUser);

            _context.Reservation.Add(reservation);
            await _context.SaveChangesAsync();

            // Track event
            _telemetryClient.TrackEvent(
                "New reservation was submitted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return reservation;
        }

        public async Task<Reservation> UpdateReservationAsync(Reservation reservation, User currentUser, bool dropoffConfirmed = false)
        {
            var dbReservation = await _context.Reservation.FindAsync(reservation.Id);
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
            else dbReservation.EndDate = null;

            try
            {
                dbReservation.EndDate = CalculateEndTime(dbReservation.StartDate, dbReservation.EndDate);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new ArgumentException("Reservation can be made to slots only.");
            }

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
                    var reservationUser = await _context.Users.FindAsync(reservation.UserId);
                    if (currentUser.IsAdmin && reservationUser?.Company != currentUser.Company)
                        throw new UnauthorizedAccessException("Cannot update reservation for users from other companies.");

                    dbReservation.UserId = reservation.UserId;
                }
            }

            // Time requirement calculation
            dbReservation.TimeRequirement = dbReservation.Services.Contains(Constants.ServiceType.Carpet) ?
                _configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * _configuration.CurrentValue.Reservation.TimeUnit :
                _configuration.CurrentValue.Reservation.TimeUnit;

            // Check if MPV
            dbReservation.Mpv = dbReservation.Mpv || await IsMpvAsync(dbReservation.VehiclePlateNumber);

            // Update calendar event if date changed
            if (dbReservation.UserId == currentUser.Id && currentUser.CalendarIntegration && newStartDate != oldStartDate)
            {
                dbReservation.User = currentUser;
                dbReservation.OutlookEventId = await _calendarService.UpdateEventAsync(dbReservation);
            }

            await _context.SaveChangesAsync();

            // Track event
            _telemetryClient.TrackEvent(
                "Reservation was updated.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", dbReservation.Id },
                    { "Reservation user ID", dbReservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return dbReservation;
        }

        public async Task<Reservation> DeleteReservationAsync(string reservationId, User currentUser)
        {
            var reservation = await _context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id && !currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Cannot delete reservation for other users.");

            _context.Reservation.Remove(reservation);
            await _context.SaveChangesAsync();

            // Delete calendar event
            await _calendarService.DeleteEventAsync(reservation);

            // Track event
            _telemetryClient.TrackEvent(
                "Reservation was deleted.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });

            return reservation;
        }

        public async Task ConfirmDropoffAsync(string reservationId, string location, User currentUser)
        {
            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");
            if (string.IsNullOrEmpty(location))
                throw new ArgumentException("Reservation location cannot be null.");

            var reservation = await _context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id && !currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Cannot confirm dropoff for other users.");

            reservation.State = State.DropoffAndLocationConfirmed;
            reservation.Location = location;

            await _context.SaveChangesAsync();

            // Track event
            _telemetryClient.TrackEvent(
                "Key dropoff was confirmed by user.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Action user ID", currentUser.Id },
                });
        }

        public async Task StartWashAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can start wash.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = State.WashInProgress;
            await _context.SaveChangesAsync();

            // Send bot message
            await _botService.SendWashStartedMessageAsync(reservation);
        }

        public async Task CompleteWashAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can complete wash.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await _context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .SingleOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = reservation.Private ? State.NotYetPaid : State.Done;
            await _context.SaveChangesAsync();

            // Send notification
            await SendCompletionNotificationAsync(reservation);

            // Send bot message
            await _botService.SendWashCompletedMessageAsync(reservation);
        }

        public async Task ConfirmPaymentAsync(string reservationId, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can confirm payment.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            if (reservation.State != State.NotYetPaid)
                throw new InvalidOperationException("Reservation state is not 'Not yet paid'.");

            reservation.State = State.Done;
            await _context.SaveChangesAsync();
        }

        public async Task SetReservationStateAsync(string reservationId, State state, User currentUser)
        {
            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can set reservation state.");

            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");

            var reservation = await _context.Reservation.FindAsync(reservationId);
            if (reservation == null)
                throw new InvalidOperationException("Reservation not found.");

            reservation.State = state;
            await _context.SaveChangesAsync();
        }

        public async Task AddCommentAsync(string reservationId, string comment, User currentUser)
        {
            if (string.IsNullOrEmpty(reservationId))
                throw new ArgumentException("Reservation id cannot be null.");
            if (string.IsNullOrEmpty(comment))
                throw new ArgumentException("Comment cannot be null.");

            var reservation = await _context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId);
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

            await _context.SaveChangesAsync();

            // Send notification if CarWash admin adds comment
            if (currentUser.IsCarwashAdmin)
            {
                await SendCommentNotificationAsync(reservation, comment);
                await _botService.SendCarWashCommentLeftMessageAsync(reservation);
            }
        }

        public async Task<bool> IsMpvAsync(string vehiclePlateNumber)
        {
            return await _context.Reservation
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
            var timeZoneId = _configuration.CurrentValue.Reservation.TimeZone;

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

            var slot = _configuration.CurrentValue.Slots.Find(s => s.StartTime == startTimeOfDay);
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

            _telemetryClient.TrackTrace(
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

            _telemetryClient.TrackTrace(
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
            var earliestTimeAllowed = DateTime.UtcNow.AddMinutes(_configuration.CurrentValue.Reservation.MinutesToAllowReserveInPast * -1);

            if (startTime < earliestTimeAllowed || endTime < earliestTimeAllowed)
            {
                _telemetryClient.TrackTrace(
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
            var timeZoneId = _configuration.CurrentValue.Reservation.TimeZone;

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

            if (_configuration.CurrentValue.Slots.Any(s => s.StartTime == startTimeOfDay && s.EndTime == endTimeOfDay)) return true;

            _telemetryClient.TrackTrace(
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

            var activeReservationCount = await _context.Reservation.Where(r => r.UserId == currentUser.Id && r.State != State.Done).CountAsync();

            if (activeReservationCount >= _configuration.CurrentValue.Reservation.UserConcurrentReservationLimit)
            {
                _telemetryClient.TrackTrace(
                    "BadRequest: User has met the active concurrent reservation limit.",
                    Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", currentUser.Id },
                        { "ActiveReservationCount", activeReservationCount.ToString() },
                        { "UserConcurrentReservationLimit", _configuration.CurrentValue.Reservation.UserConcurrentReservationLimit.ToString() }
                    });

                return true;
            }

            return false;
        }

        private async Task<bool> IsEnoughTimeOnDateAsync(DateTime date, int timeRequirement, User currentUser)
        {
            if (currentUser.IsCarwashAdmin) return true;

            var userCompanyLimit = (await _context.Company.SingleAsync(c => c.Name == currentUser.Company)).DailyLimit;

            if ((date.Date == DateTime.UtcNow.Date && DateTime.UtcNow.Hour >= _configuration.CurrentValue.Reservation.HoursAfterCompanyLimitIsNotChecked)
                || userCompanyLimit == 0)
            {
                var allSlotCapacity = _configuration.CurrentValue.Slots.Sum(s => s.Capacity);

                var reservedTimeOnDate = await _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date)
                    .SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > allSlotCapacity * _configuration.CurrentValue.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
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
                var reservedTimeOnDate = await _context.Reservation
                    .Where(r => r.StartDate.Date == date.Date && r.User.Company == currentUser.Company)
                    .SumAsync(r => r.TimeRequirement);

                if (reservedTimeOnDate + timeRequirement > userCompanyLimit * _configuration.CurrentValue.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
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
                var toBeDoneTodayTime = await _context.Reservation
                    .Where(r => r.StartDate >= DateTime.UtcNow && r.StartDate.Date == DateTime.UtcNow.Date)
                    .SumAsync(r => r.TimeRequirement);

                var remainingSlotCapacityToday = GetRemainingSlotCapacityToday();
                if (toBeDoneTodayTime + timeRequirement > remainingSlotCapacityToday * _configuration.CurrentValue.Reservation.TimeUnit)
                {
                    _telemetryClient.TrackTrace(
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

        private async Task<bool> IsEnoughTimeInSlotAsync(DateTime dateTime, int timeRequirement, User currentUser)
        {
            if (currentUser.IsCarwashAdmin) return true;

            var reservedTimeInSlot = await _context.Reservation
                .Where(r => r.StartDate == dateTime)
                .SumAsync(r => r.TimeRequirement);

            if (dateTime.Kind == DateTimeKind.Unspecified)
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

            TimeSpan timeOfDay;
            var timeZoneId = _configuration.CurrentValue.Reservation.TimeZone;

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

            var slotCapacity = _configuration.CurrentValue.Slots.Find(s => s.StartTime == timeOfDay)?.Capacity;
            if (reservedTimeInSlot + timeRequirement <= slotCapacity * _configuration.CurrentValue.Reservation.TimeUnit) return true;

            _telemetryClient.TrackTrace(
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

            if (await _context.Blocker.AnyAsync(b => b.StartDate < startTime && b.EndDate > endTime))
            {
                _telemetryClient.TrackTrace(
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
            var timeZoneId = _configuration.CurrentValue.Reservation.TimeZone;
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

            foreach (var slot in _configuration.CurrentValue.Slots)
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
                    reservation.OutlookEventId = await _calendarService.CreateEventAsync(reservation);
                }
            }
            else
            {
                var user = await _context.Users.FindAsync(reservation.UserId);
                if (user?.CalendarIntegration == true)
                {
                    var timer = DateTime.UtcNow;
                    try
                    {
                        reservation.User = user;
                        reservation.OutlookEventId = await _calendarService.CreateEventAsync(reservation);
                        _telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: true);
                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: false);
                        _telemetryClient.TrackException(e);
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
                        await _pushService.Send(reservation.UserId, notification);
                        break;
                    }
                    catch (PushService.NoActivePushSubscriptionException)
                    {
                        reservation.User.NotificationChannel = NotificationChannel.Email;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackException(e);
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
                    await _emailService.Send(email, TimeSpan.FromMinutes(1));
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
                        await _pushService.Send(reservation.UserId, notification);
                        break;
                    }
                    catch (PushService.NoActivePushSubscriptionException)
                    {
                        reservation.User.NotificationChannel = NotificationChannel.Email;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception e)
                    {
                        _telemetryClient.TrackException(e);
                    }
                    goto case NotificationChannel.Email;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email,
                        Subject = "CarWash has left a comment on your reservation",
                        Body = comment + $"\n\n<a href=\"{_configuration.CurrentValue.ConnectionStrings.BaseUrl}\">Reply</a>\n\nPlease do not reply to this email, messages to service providers can only be sent within the app. Kindly log in to your account and communicate directly through the in-app messaging feature.",
                    };
                    await _emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}