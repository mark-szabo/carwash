using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
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
        private readonly ReservationValidator reservationValidator = new(context, configuration, telemetryClient);

        /// <inheritdoc />
        public async Task<Reservation?> GetReservationByIdAsync(string reservationId, User currentUser)
        {
            var reservation = await context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .SingleOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null) return null;

            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                    throw new UnauthorizedAccessException("Cannot view reservation of other users.");

                if (currentUser.IsAdmin && reservation.User.Company != currentUser.Company)
                    throw new UnauthorizedAccessException("Cannot view reservation of users from other companies.");
            }

            return reservation;
        }

        /// <inheritdoc />
        public async Task<List<Reservation>> GetReservationsOfUserAsync(User currentUser)
        {
            return await context.Reservation
                .Include(r => r.KeyLockerBox)
                .Where(r => r.UserId == currentUser.Id)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Reservation>> GetReservationsOfCompanyAsync(User currentUser)
        {
            if (!currentUser.IsAdmin) throw new UnauthorizedAccessException();

            return await context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .Where(r => r.User!.Company == currentUser.Company && r.UserId != currentUser.Id)
                .OrderByDescending(r => r.StartDate)
                .Take(100)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<Reservation>> GetReservationsOnBacklog(User currentUser)
        {
            if (!currentUser.IsCarwashAdmin) throw new UnauthorizedAccessException();

            return await context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .Where(r => r.StartDate.Date >= DateTime.UtcNow.Date.AddDays(-3) || r.State != State.Done)
                .OrderBy(r => r.StartDate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<Reservation> CreateReservationAsync(Reservation reservation, User currentUser)
        {
            // Set defaults
            reservation.UserId ??= currentUser.Id;
            reservation.State = State.SubmittedNotActual;
            reservation.Mpv = false;
            reservation.VehiclePlateNumber = reservation.VehiclePlateNumber.ToUpper().Replace("-", string.Empty).Replace(" ", string.Empty);
            reservation.CreatedById = currentUser.Id;
            reservation.CreatedOn = DateTime.UtcNow;

            // Validate the reservation
            var validationResult = await reservationValidator.ValidateReservationAsync(reservation, isUpdate: false, currentUser);
            if (!validationResult.IsValid) throw new ReservationValidationExeption(validationResult.ErrorMessage);

            if (reservation.Comments?.Count == 1)
            {
                reservation.Comments[0].UserId = currentUser.Id;
                reservation.Comments[0].Timestamp = DateTime.UtcNow;
                reservation.Comments[0].Role = CommentRole.User;
            }
            if (reservation.Comments?.Count > 1)
            {
                telemetryClient.TrackTrace(
                    "BadRequest: Only one comment can be added when creating a reservation.",
                    SeverityLevel.Error,
                    new Dictionary<string, string>
                    {
                        { "UserId", reservation.UserId },
                        { "Comments", reservation.CommentsJson ?? "" }
                    });
                throw new ArgumentException("Only one comment can be added when creating a reservation.");
            }

            // EndDate should already be calculated during validation

            // TODO: Simplify Time requirement calculation
            reservation.TimeRequirement = reservation.Services.Contains(Constants.ServiceType.Carpet) ?
                configuration.CurrentValue.Reservation.CarpetCleaningMultiplier * configuration.CurrentValue.Reservation.TimeUnit :
                configuration.CurrentValue.Reservation.TimeUnit;

            // Check if MPV
            reservation.Mpv = await IsMpvAsync(reservation.VehiclePlateNumber);

            // Create calendar event
            reservation.OutlookEventId = await CreateCalendarEventAsync(reservation, currentUser);

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
        public async Task<Reservation> UpdateReservationAsync(Reservation reservation, User currentUser)
        {
            // Validate the reservation
            var validationResult = await reservationValidator.ValidateReservationAsync(reservation, isUpdate: true, currentUser, reservation.Id);
            if (!validationResult.IsValid) throw new ReservationValidationExeption(validationResult.ErrorMessage);

            var dbReservation = await context.Reservation.FindAsync(reservation.Id) ?? throw new InvalidOperationException("Reservation not found.");

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

            await SaveReservationChangesAsync(reservation);

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
            var reservation = await context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

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
            ArgumentNullException.ThrowIfNull(reservationId);
            ArgumentNullException.ThrowIfNull(location);

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id && !currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Cannot confirm dropoff for other users.");

            reservation.State = State.DropoffAndLocationConfirmed;
            reservation.Location = location;
            await SaveReservationChangesAsync(reservation);

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
        public async Task<Reservation> ConfirmDropoffByEmail(string email, string location, string? vehiclePlateNumber)
        {
            ArgumentNullException.ThrowIfNull(email);
            ArgumentNullException.ThrowIfNull(location);

            var user = await context.Users.SingleOrDefaultAsync(u => u.Email == email)
                ?? throw new InvalidOperationException($"No user found with email address '{email}'.");

            var reservations = await context.Reservation
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.User!.Email == user.Email && (r.State == State.SubmittedNotActual || r.State == State.ReminderSentWaitingForKey))
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            if (reservations.Count == 0) throw new InvalidOperationException("No reservations found.");

            Reservation reservation;
            string reservationResolution;
            // Only one active - straightforward
            if (reservations.Count == 1)
            {
                reservation = reservations[0];
                reservationResolution = "Only one active reservation.";
            }
            // Vehicle plate number was specified and only one reservation for that car - easy
            else if (vehiclePlateNumber?.ToUpper() != null && reservations.Count(r => r.VehiclePlateNumber == vehiclePlateNumber) == 1)
            {
                reservation = reservations.Single(r => r.VehiclePlateNumber == vehiclePlateNumber.ToUpper());
                reservationResolution = "Vehicle plate number was specified and only one reservation exists for that car.";
            }
            // Only one where we are waiting for the key - still pretty straightforward
            else if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey) == 1)
            {
                reservation = reservations.Single(r => r.State == State.ReminderSentWaitingForKey);
                reservationResolution = "Only one reservation is waiting for the key.";
            }
            // One where we are waiting for the key and is today - eg. there's another one in the past where the key was not dropped off and nobody deleted it
            else if (reservations.Count(r => r.State == State.ReminderSentWaitingForKey && r.StartDate.Date == DateTime.UtcNow.Date) == 1)
            {
                reservation = reservations.Single(r => r.State == State.ReminderSentWaitingForKey && r.StartDate.Date == DateTime.UtcNow.Date);
                reservationResolution = "Only one reservation TODAY is waiting for the key.";
            }
            // Only one active reservation today - eg. user has two reservations, one today, one in the future and on the morning drops off the keys before the reminder
            else if (reservations.Count(r => r.StartDate.Date == DateTime.UtcNow.Date) == 1)
            {
                reservation = reservations.Single(r => r.StartDate.Date == DateTime.UtcNow.Date);
                reservationResolution = "Only one reservation today.";
            }
            else if (vehiclePlateNumber == null)
            {
                throw new InvalidDataException("More than one reservation found where the reservation state is submitted or waiting for key. Please specify vehicle plate number!");
            }
            else
            {
                throw new InvalidDataException("More than one reservation found where the reservation state is submitted or waiting for key.");
            }

            await ConfirmDropoffAsync(reservation.Id, location, reservation.User!);

            telemetryClient.TrackEvent(
                "Key dropoff was confirmed by 3rd party service.",
                new Dictionary<string, string>
                {
                    { "Reservation ID", reservation.Id },
                    { "Reservation user ID", reservation.UserId },
                    { "Reservation resolution method", reservationResolution },
                    { "Number of active reservation of the user", reservations.Count.ToString() },
                },
                new Dictionary<string, double>
                {
                    { "Service dropoff", 1 },
                });

            return reservation;
        }

        /// <inheritdoc />
        public async Task StartWashAsync(string reservationId, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can start wash.");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            reservation.State = State.WashInProgress;
            await SaveReservationChangesAsync(reservation);

            // Send bot message
            await botService.SendWashStartedMessageAsync(reservation);
        }

        /// <inheritdoc />
        public async Task CompleteWashAsync(string reservationId, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can complete wash.");

            var reservation = await context.Reservation
                .Include(r => r.User)
                .Include(r => r.KeyLockerBox)
                .SingleOrDefaultAsync(r => r.Id == reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            reservation.State = reservation.Private ? State.NotYetPaid : State.Done;
            await SaveReservationChangesAsync(reservation);

            // Send notification
            await SendCompletionNotificationAsync(reservation);

            // Send bot message
            await botService.SendWashCompletedMessageAsync(reservation);
        }

        /// <inheritdoc />
        public async Task ConfirmPaymentAsync(string reservationId, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can confirm payment.");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            if (reservation.State != State.NotYetPaid)
                throw new InvalidOperationException("Reservation state is not 'Not yet paid'.");

            reservation.State = State.Done;
            await SaveReservationChangesAsync(reservation);
        }

        /// <inheritdoc />
        public async Task SetReservationStateAsync(string reservationId, State state, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can set reservation state.");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            reservation.State = state;
            await SaveReservationChangesAsync(reservation);
        }

        /// <inheritdoc />
        public async Task AddCommentAsync(string reservationId, string comment, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);
            ArgumentNullException.ThrowIfNull(comment);

            var reservation = await context.Reservation.Include(r => r.User).SingleOrDefaultAsync(r => r.Id == reservationId)
                ?? throw new InvalidOperationException("Reservation not found.");

            if (reservation.UserId != currentUser.Id)
            {
                if (!currentUser.IsAdmin && !currentUser.IsCarwashAdmin)
                    throw new UnauthorizedAccessException("Cannot add comment to other users' reservations.");

                if (currentUser.IsAdmin && reservation.User!.Company != currentUser.Company)
                    throw new UnauthorizedAccessException("Cannot add comment to reservations from other companies.");
            }

            reservation.AddComment(new Comment
            {
                UserId = currentUser.Id,
                Role = reservation.UserId != currentUser.Id && currentUser.IsCarwashAdmin ? CommentRole.Carwash : CommentRole.User,
                Timestamp = DateTime.UtcNow,
                Message = comment
            });

            await SaveReservationChangesAsync(reservation);

            // Send notification if CarWash admin adds comment
            if (currentUser.IsCarwashAdmin)
            {
                await SendCommentNotificationAsync(reservation, comment);

                await botService.SendCarWashCommentLeftMessageAsync(reservation);
            }

            //TODO: Send push notification to carwash admins if user adds comment
        }

        /// <inheritdoc />
        public async Task SetMpvAsync(string reservationId, bool mpv, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can set MPV flag");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found");

            reservation.Mpv = mpv;
            await SaveReservationChangesAsync(reservation);
        }

        /// <inheritdoc />
        public async Task UpdateServicesAsync(string reservationId, List<int> services, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);
            ArgumentNullException.ThrowIfNull(services);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can update services");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found");

            reservation.Services = services;
            await SaveReservationChangesAsync(reservation);
        }

        /// <inheritdoc />
        public async Task UpdateLocationAsync(string reservationId, string location, User currentUser)
        {
            ArgumentNullException.ThrowIfNull(reservationId);
            ArgumentNullException.ThrowIfNull(location);

            if (!currentUser.IsCarwashAdmin)
                throw new UnauthorizedAccessException("Only carwash admins can update location");

            var reservation = await context.Reservation.FindAsync(reservationId)
                ?? throw new InvalidOperationException("Reservation not found");

            reservation.Location = location;
            await SaveReservationChangesAsync(reservation);
        }

        /// <inheritdoc />
        public async Task<NotAvailableDatesAndTimes> GetNotAvailableDatesAndTimesAsync(User currentUser, int daysAhead = 365)
        {
            if (currentUser.IsCarwashAdmin)
                return new NotAvailableDatesAndTimes([], []);

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

                if (toBeDoneTodayTime >= reservationValidator.RemainingSlotCapacityToday * configuration.CurrentValue.Reservation.TimeUnit)
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

            // Check if a slot has already started today
            foreach (var slot in configuration.CurrentValue.Slots)
            {
                var now = DateTime.UtcNow;
                var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;
                DateTime slotStartTimeUtc;

                if (timeZoneId == "UTC")
                {
                    // For UTC timezone, create time directly
                    slotStartTimeUtc = now.Date.Add(slot.StartTime);
                }
                else
                {
                    // Convert current UTC time to provider's timezone
                    var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    var nowInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(now, providerTimeZone);
                    var todayInProviderZone = nowInProviderZone.Date;
                    var slotStartTimeInProviderZone = todayInProviderZone.Add(slot.StartTime);
                    slotStartTimeUtc = TimeZoneInfo.ConvertTimeToUtc(slotStartTimeInProviderZone, providerTimeZone);
                }

                if (!notAvailableTimes.Contains(slotStartTimeUtc) && slotStartTimeUtc.AddMinutes(configuration.CurrentValue.Reservation.MinutesToAllowReserveInPast) < DateTime.UtcNow)
                {
                    notAvailableTimes.Add(slotStartTimeUtc);
                }
            }
            #endregion

            #region Check blockers
            var blockers = await context.Blocker
                .Where(b => b.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            foreach (var blocker in blockers)
            {
                Debug.Assert(blocker.EndDate != null, "blocker.EndDate != null");
                if (blocker.EndDate == null) continue;

                var dateIterator = blocker.StartDate.Date;
                while (dateIterator <= ((DateTime)blocker.EndDate).Date)
                {
                    // Don't bother with the past part of the blocker
                    if (dateIterator < DateTime.UtcNow.Date)
                    {
                        dateIterator = dateIterator.AddDays(1);
                        continue;
                    }

                    var dateBlocked = true;

                    foreach (var slot in configuration.CurrentValue.Slots)
                    {
                        // Convert slot times from provider timezone to UTC for the iteration date
                        var timeZoneId = configuration.CurrentValue.Reservation.TimeZone;
                        DateTime slotStart, slotEnd;

                        if (timeZoneId == "UTC")
                        {
                            // For UTC timezone, create times directly
                            slotStart = dateIterator.Date.Add(slot.StartTime);
                            slotEnd = dateIterator.Date.Add(slot.EndTime);
                        }
                        else
                        {
                            var providerTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                            var dateInProviderZone = TimeZoneInfo.ConvertTimeFromUtc(dateIterator, providerTimeZone).Date;
                            var slotStartInProviderZone = dateInProviderZone.Add(slot.StartTime);
                            var slotEndInProviderZone = dateInProviderZone.Add(slot.EndTime);
                            slotStart = TimeZoneInfo.ConvertTimeToUtc(slotStartInProviderZone, providerTimeZone);
                            slotEnd = TimeZoneInfo.ConvertTimeToUtc(slotEndInProviderZone, providerTimeZone);
                        }

                        if (slotStart > blocker.StartDate && slotEnd < blocker.EndDate && !notAvailableTimes.Contains(slotStart))
                        {
                            notAvailableTimes.Add(slotStart);
                        }
                        else
                        {
                            dateBlocked = false;
                        }
                    }

                    if (dateBlocked && !notAvailableDates.Contains(dateIterator)) notAvailableDates.Add(dateIterator);

                    dateIterator = dateIterator.AddDays(1);
                }
            }
            #endregion

            return new NotAvailableDatesAndTimes(
                Dates: notAvailableDates.Distinct().Select(DateOnly.FromDateTime),
                Times: notAvailableTimes);
        }

        /// <inheritdoc />
        public async Task<LastSettings?> GetLastSettingsAsync(User currentUser)
        {
            var lastReservation = await context.Reservation
                .Where(r => r.UserId == currentUser.Id)
                .OrderByDescending(r => r.CreatedOn)
                .FirstOrDefaultAsync();

            if (lastReservation == null)
                return null;

            return new LastSettings(
                VehiclePlateNumber: lastReservation.VehiclePlateNumber,
                Location: lastReservation.Location ?? "",
                Services: lastReservation.Services ?? []);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ReservationCapacity>> GetReservationCapacityAsync(DateTime date)
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

            var slotFreeCapacity = new List<ReservationCapacity>();
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
                slotFreeCapacity.Add(new ReservationCapacity(
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
                    slotFreeCapacity.Add(new ReservationCapacity(
                        StartTime: slotDateTime,
                        FreeCapacity: slot.Capacity));
                }
            }

            return slotFreeCapacity.OrderBy(s => s.StartTime);
        }

        /// <inheritdoc />
        public async Task<byte[]> ExportReservationsAsync(User currentUser, DateTime startDate, DateTime endDate)
        {
            List<Reservation> reservations;

            if (currentUser.IsCarwashAdmin)
                reservations = await context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.StartDate.Date >= startDate.Date && r.StartDate.Date <= endDate.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else if (currentUser.IsAdmin)
                reservations = await context.Reservation
                    .Include(r => r.User)
                    .Where(r => r.User.Company == currentUser.Company && r.StartDate.Date >= startDate.Date &&
                                r.StartDate.Date <= endDate.Date)
                    .OrderBy(r => r.StartDate)
                    .ToListAsync();
            else throw new UnauthorizedAccessException("Only admins or carwash admins can export reservations");

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            // Add a new worksheet to the empty workbook
            var worksheet = package.Workbook.Worksheets.Add($"{startDate.Year}-{startDate.Month}");

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

            // Add values
            var i = 2;
            foreach (var reservation in reservations)
            {
                worksheet.Cells[i, 1].Style.Numberformat.Format = "yyyy-mm-dd";
                worksheet.Cells[i, 1].Value = reservation.StartDate.Date;

                worksheet.Cells[i, 2].Style.Numberformat.Format = "hh:mm";
                worksheet.Cells[i, 2].Value = reservation.StartDate.TimeOfDay;

                worksheet.Cells[i, 3].Style.Numberformat.Format = "hh:mm";
                worksheet.Cells[i, 3].Value = reservation.EndDate?.TimeOfDay;

                worksheet.Cells[i, 4].Value = reservation.User.Company;
                worksheet.Cells[i, 5].Value = reservation.User.FullName;

                worksheet.Cells[i, 6].Value = reservation.Private ? reservation.User.Email : "";
                worksheet.Cells[i, 7].Value = reservation.Private ? reservation.User.PhoneNumber : "";
                worksheet.Cells[i, 8].Value = reservation.Private ? reservation.User.BillingName : "";
                worksheet.Cells[i, 9].Value = reservation.Private ? reservation.User.BillingAddress : "";
                worksheet.Cells[i, 10].Value = reservation.Private ? reservation.User.PaymentMethod : "";

                worksheet.Cells[i, 11].Value = reservation.VehiclePlateNumber;
                worksheet.Cells[i, 12].Value = reservation.Mpv;
                worksheet.Cells[i, 13].Value = reservation.Private;
                worksheet.Cells[i, 14].Value = reservation.GetServiceNames(configuration.CurrentValue);
                worksheet.Cells[i, 15].Value = reservation.CommentsJson;
                worksheet.Cells[i, 17].Value = reservation.GetPrice(configuration.CurrentValue);

                i++;
            }

            // Format as table
            var dataRange = worksheet.Cells[1, 1, i == 2 ? i : i - 1, 17]; //cannot create table with only one row
            var table = worksheet.Tables.Add(dataRange, $"reservations_{startDate.Year}_{startDate.Month}");
            table.ShowTotal = false;
            table.ShowHeader = true;
            table.ShowFilter = true;

            // Column auto-width
            worksheet.Column(1).AutoFit();
            worksheet.Column(2).AutoFit();
            worksheet.Column(3).AutoFit();
            worksheet.Column(4).AutoFit();
            worksheet.Column(5).AutoFit();
            worksheet.Column(6).AutoFit();
            worksheet.Column(7).AutoFit();
            worksheet.Column(8).AutoFit();
            worksheet.Column(9).AutoFit();
            worksheet.Column(10).AutoFit();
            worksheet.Column(11).AutoFit();
            worksheet.Column(12).AutoFit();
            worksheet.Column(13).AutoFit();
            // worksheet.Column(14).AutoFit(); //services
            // don't do it for comment fields
            worksheet.Column(17).AutoFit();

            // Pivot table
            var pivotSheet = package.Workbook.Worksheets.Add($"{startDate.Year}-{startDate.Month} pivot");
            var pivot = pivotSheet.PivotTables.Add(pivotSheet.Cells[1, 1], dataRange, "Employee pivot");
            if (currentUser.IsCarwashAdmin) pivot.RowFields.Add(pivot.Fields["Company"]);
            pivot.RowFields.Add(pivot.Fields["Name"]);
            pivot.DataFields.Add(pivot.Fields["Price (computed)"]);
            pivot.DataOnRows = true;

            return await package.GetAsByteArrayAsync();
        }

        #region Private Helper Methods

        private async Task<bool> IsMpvAsync(string vehiclePlateNumber)
        {
            return await context.Reservation
                .OrderByDescending(r => r.StartDate)
                .Where(r => r.VehiclePlateNumber == vehiclePlateNumber)
                .Select(r => r.Mpv)
                .FirstOrDefaultAsync();
        }

        private async Task<string?> CreateCalendarEventAsync(Reservation reservation, User currentUser)
        {
            var user = (reservation.UserId == currentUser.Id) ? currentUser : await context.Users.FindAsync(reservation.UserId);

            if (user == null) return null;

            if (user.CalendarIntegration)
            {
                var timer = DateTime.UtcNow;
                try
                {
                    reservation.User = user;
                    var outlookEventId = await calendarService.CreateEventAsync(reservation);
                    telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: true);

                    return outlookEventId;
                }
                catch (Exception e)
                {
                    telemetryClient.TrackDependency("CalendarService", "CreateEvent", new { ReservationId = reservation.Id, UserId = user.Id }.ToString(), timer, DateTime.UtcNow - timer, success: false);
                    telemetryClient.TrackException(e);
                }
            }

            return null;
        }

        private async Task SendCompletionNotificationAsync(Reservation reservation)
        {
            ArgumentNullException.ThrowIfNull(reservation.User);

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
                        await pushService.Send(reservation.UserId!, notification);
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
                    // If push fails, fallback to email
                    goto case NotificationChannel.Email;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email!,
                        Subject = reservation.Private ? "Your car is ready! Don't forget to pay!" : "Your car is ready!",
                        Body = $"You can find it here: {reservation.Location}" + (reservation.KeyLockerBox != null ? $" (Locker: {reservation.KeyLockerBox.Name})" : ""),
                    };
                    await emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reservation.User.NotificationChannel));
            }
        }

        private async Task SendCommentNotificationAsync(Reservation reservation, string comment)
        {
            ArgumentNullException.ThrowIfNull(reservation.User);

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
                        await pushService.Send(reservation.UserId!, notification);
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
                    // If push fails, fallback to email
                    goto case NotificationChannel.Email;
                case NotificationChannel.NotSet:
                case NotificationChannel.Email:
                    var email = new Email
                    {
                        To = reservation.User.Email!,
                        Subject = "CarWash has left a comment on your reservation",
                        Body = comment + $"\n\n<a href=\"{configuration.CurrentValue.ConnectionStrings.BaseUrl}\">Reply</a>\n\nPlease do not reply to this email, messages to service providers can only be sent within the app. Kindly log in to your account and communicate directly through the in-app messaging feature.",
                    };
                    await emailService.Send(email, TimeSpan.FromMinutes(1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reservation.User.NotificationChannel));
            }
        }

        private async Task SaveReservationChangesAsync(Reservation reservation)
        {
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await context.Reservation.AnyAsync(e => e.Id == reservation.Id))
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    throw;
                }
            }
        }

        #endregion
    }
}