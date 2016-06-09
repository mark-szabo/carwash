using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using MSHU.CarWash.Models;
using MSHU.CarWash.DAL;
using System.Globalization;
using System.Configuration;
using System.Security.Claims;
using MSHU.CarWash.Helpers;
using System.Diagnostics;

namespace MSHU.CarWash.Controllers
{
    [Authorize]
    [RequireHttps]
    public class CalendarController : ApiController
    {
        #region private fields
        private readonly MSHUCarWashContext _db;
        private readonly int _slotsPerDay;
        private readonly Dictionary<DateTime, Tuple<int, int>> _exceptionalDays;
        private readonly int _monthlyLimitPerPerson;
        private readonly int _reservationLimitPerPerson;
        private int _steamSlotsOnTuesday;
        #endregion private fields

        public CalendarController()
        {
            _db = new MSHUCarWashContext();
            _slotsPerDay = Convert.ToInt32(ConfigurationManager.AppSettings["SlotsPerDay"]);
            _steamSlotsOnTuesday = Convert.ToInt32(ConfigurationManager.AppSettings["SteamSlotsOnTuesday"]);
            _exceptionalDays = new Dictionary<DateTime, Tuple<int, int>>();
            var exceptionalDays = ConfigurationManager.AppSettings["ExceptionalDays"].Split(';');
            foreach (var item in exceptionalDays)
            {
                if (!string.IsNullOrWhiteSpace(item) && item.Contains(':'))
                {
                    var splitted = item.Split(':');
                    var numbers = splitted[1].Split(',');
                    _exceptionalDays.Add(Convert.ToDateTime(splitted[0]), new Tuple<int, int>(Convert.ToInt32(numbers[0]), Convert.ToInt32(numbers[1])));
                }
            }
            _monthlyLimitPerPerson = Convert.ToInt32(ConfigurationManager.AppSettings["MontlySlotLimitPerPerson"]);
            _reservationLimitPerPerson = 2;
        }

        [HttpGet]
        [ResponseType(typeof(WeekViewModel))]
        public async Task<IHttpActionResult> GetWeek(int offset)
        {
            if (offset > 50 || offset < -50)
            {
                return BadRequest();
            }

            WeekViewModel ret = new WeekViewModel();
            ret.Offset = offset;
            ret.NextWeekOffset = offset + 1;
            ret.PreviousWeekOffset = offset - 1;
            DateTime startOfCurrentWeek = DateTime.Today.GetFirstDayOfWeek();
            ret.StartOfWeek = startOfCurrentWeek.AddDays(7 * offset);
            DateTime endOfWeek = ret.StartOfWeek.AddDays(6);
            if (ret.StartOfWeek.Month != endOfWeek.Month)
            {
                ret.DateInterval = string.Format("{0} {1} - {2} {3}", ret.StartOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("en-US")), ret.StartOfWeek.Day, endOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("en-US")), endOfWeek.Day).ToUpper();
            }
            else
            {
                ret.DateInterval = string.Format("{0} {1} - {2}", ret.StartOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("en-US")), ret.StartOfWeek.Day, endOfWeek.Day).ToUpper();
            }
            ret.Days = await GetDaysDetailsAsync(ret.StartOfWeek, endOfWeek);

            return Ok(ret);
        }

        [HttpGet]
        [ResponseType(typeof(DayDetailsViewModel))]
        public async Task<IHttpActionResult> GetDay(DateTime day, int offset)
        {
            if (day > DateTime.Today.AddDays(350) || day < DateTime.Today.AddDays(-350))
            {
                return BadRequest();
            }
            if (offset > 50 || offset < -50)
            {
                return BadRequest();
            }

            var currentUser = UserHelper.GetCurrentUser();

            DayDetailsViewModel ret = new DayDetailsViewModel();

            ret.Offset = offset;

            ret.Day = day;

            var query = from b in _db.Reservations.Include(i => i.Employee)
                        where b.Date == day
                        select b;
            var queryResult = await query.ToListAsync();


            ret.AvailableNormalSlots = day < DateTime.Today ? 0 : GetAvailableSlots(queryResult, day, false).Item1;
            ret.AvailableSteamSlots = day < DateTime.Today ? 0 : GetAvailableSlots(queryResult, day, false).Item2;

            ret.Reservations = queryResult
                                .Select(s => new ReservationDetailViewModel
                                {
                                    Date = s.Date,
                                    EmployeeId = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.EmployeeId : "",
                                    EmployeeName = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.Employee.Name : "",
                                    ReservationId = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.ReservationId : 0,
                                    SelectedServiceName = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? ((ServiceEnum)s.SelectedServiceId).GetDescription() : "",
                                    VehiclePlateNumber = s.VehiclePlateNumber,
                                    Comment = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.Comment : "",
                                    IsDeletable = (s.EmployeeId == currentUser.Id && s.Date >= DateTime.Today) || currentUser.IsAdmin
                                })
                                .ToList<ReservationDetailViewModel>();

            ret.ReservationIsAllowed = (ret.AvailableNormalSlots > 0 || ret.AvailableSteamSlots > 0);

            if (ret.ReservationIsAllowed && !currentUser.IsAdmin)
            {
                query = from b in _db.Reservations
                        where b.EmployeeId == currentUser.Id && b.Date >= DateTime.Today
                        select b;
                queryResult = await query.ToListAsync();
                //csak akkor foglalhat, ha nincs aktív foglalása
                ret.ReservationIsAllowed = CanUserReserve(currentUser);
            }

            if (ret.ReservationIsAllowed)
            {
                Employee employee = await _db.Employees.FindAsync(currentUser.Id);
                ret.NewReservation = new NewReservationViewModel();
                ret.NewReservation.Date = day;
                ret.NewReservation.EmployeeId = currentUser.Id;
                ret.NewReservation.EmployeeName = currentUser.FullName;
                ret.NewReservation.VehiclePlateNumber = employee != null ? employee.VehiclePlateNumber : "";
                ret.NewReservation.IsAdmin = currentUser.IsAdmin;
            }

            return Ok(ret);
        }
               

        [HttpGet]
        [ResponseType(typeof(HomeViewModel))]
        public async Task<IHttpActionResult> GetFirstAvailableDay()
        {
            var currentUser = UserHelper.GetCurrentUser();
            DateTime day = DateTime.Today;

            if (!currentUser.IsAdmin)
            {
                #region  ha van aktív foglalása a szimpla usernek, akkor nem foglalhat
                var query = from b in _db.Reservations.Include(i => i.Employee)
                            where b.Date >= day && b.EmployeeId == currentUser.Id
                            orderby b.Date
                            select b;
                var queryResult = await query.ToListAsync();
                var activeReservation = queryResult.FirstOrDefault();
                if (activeReservation != null)
                {
                    HomeViewModel ret = new HomeViewModel();

                    ret.HasActiveReservation = true;
                    ret.ActiveReservation =
                        new ReservationDetailViewModel
                        {
                            Date = activeReservation.Date,
                            EmployeeId = activeReservation.EmployeeId,
                            EmployeeName = activeReservation.Employee.Name,
                            ReservationId = activeReservation.ReservationId,
                            SelectedServiceName =  ((ServiceEnum)activeReservation.SelectedServiceId).GetDescription(),
                            VehiclePlateNumber = activeReservation.VehiclePlateNumber,
                            Comment = activeReservation.Comment,
                            IsDeletable = activeReservation.Date >= DateTime.Today
                        };

                    return Ok(ret);
                }
                #endregion

                #region  havi limit ellenőrzés személyenként, azaz melyik naptól kezdődően foglalhat
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                query = from b in _db.Reservations
                        where b.Date >= startOfMonth && b.EmployeeId == currentUser.Id
                        orderby b.Date
                        select b;
                queryResult = await query.ToListAsync();
                int sum = 0;
                foreach (var item in queryResult)
                {
                    if (item.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                        sum += 2;
                    else
                        sum += 1;
                }               
                if (sum >= _monthlyLimitPerPerson)
                {
                    //csak a következő hónapban foglalhat
                    day = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
                }
                #endregion
            }

            var q = from b in _db.Reservations
                    where b.Date >= day
                    orderby b.Date
                    select b;
            var qResult = await q.ToListAsync();

            // consider only normal washing service
            int availableSlots = GetAvailableSlots(qResult, day, false).Item1;
            while (availableSlots == 0)
            {
                day = day.AddDays(1);

                // consider only normal washing service
                availableSlots = GetAvailableSlots(qResult, day, false).Item1;
            }

            HomeViewModel retVal = await CreateHomeViewModelAsync(day, currentUser);

            return Ok(retVal);
        }
       

        [HttpPost]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> SaveReservation(NewReservationViewModel newReservationViewModel)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = UserHelper.GetCurrentUser();
            DateTime now = DateTime.UtcNow;

            #region Check business rules
            if (!currentUser.IsAdmin)
            {
                if (!CanUserReserve(currentUser))
                {
                    ModelState.AddModelError("", string.Format("Van már foglalt időpontod!"));
                    return BadRequest(ModelState);
                }

                //havi limit ellenőrzés személyenként
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var query = from b in _db.Reservations
                        where b.Date >= startOfMonth && b.EmployeeId == newReservationViewModel.EmployeeId
                        orderby b.Date
                        select b;
                var queryResult = await query.ToListAsync();
                int sum = 0;
                foreach (var item in queryResult)
                {
                    if (item.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                        sum += 2;
                    else
                        sum += 1;
                }
                if (newReservationViewModel.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                    sum += 2;
                else
                    sum += 1;
                if (sum > _monthlyLimitPerPerson && newReservationViewModel.Date.Month == DateTime.Today.Month)
                {
                    ModelState.AddModelError("", "Ebben hónapban már nem foglalhaszt időpontot!");
                    return BadRequest(ModelState);
                }
            }
            var queryReservation = from b in _db.Reservations
                                   where b.Date == newReservationViewModel.Date
                                   select b;
            var queryReservationResult = await queryReservation.ToListAsync();
            // In case of saving a new reservation, it is necessary to check if the user already has a reservation
            // for that day.
            var availableSlots = GetAvailableSlots(queryReservationResult, newReservationViewModel.Date, true);
            if (newReservationViewModel.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
            {
                availableSlots = new Tuple<int, int>(availableSlots.Item1-2, availableSlots.Item2);
            }
            else if(newReservationViewModel.SelectedServiceId == (int)ServiceEnum.InteriorSteam ||
                newReservationViewModel.SelectedServiceId == (int)ServiceEnum.ExteriorSteam||
                newReservationViewModel.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorSteam)
            {
                availableSlots = new Tuple<int, int>(availableSlots.Item1, availableSlots.Item2 - 1);
            }
            else
            {
                availableSlots = new Tuple<int, int>(availableSlots.Item1 - 1, availableSlots.Item2);
            }
            if (availableSlots.Item1 < 0 || availableSlots.Item2 < 0)
            {
                ModelState.AddModelError("", "Ezen a napon foglalás már nem lehetséges!");
                return BadRequest(ModelState);
            }
            #endregion

            #region Update the VehiclePlateNumber to the current given value
            Employee employee = await _db.Employees.FindAsync(newReservationViewModel.EmployeeId);
            if (employee == null)
            {
                return NotFound();
            }
            if (employee.VehiclePlateNumber != newReservationViewModel.VehiclePlateNumber)
            {
                employee.VehiclePlateNumber = newReservationViewModel.VehiclePlateNumber.ToUpper();
                employee.ModifiedBy = currentUser.Id;
                employee.ModifiedOn = now;

                _db.Entry(employee).State = EntityState.Modified;

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            #endregion

            #region Save reservation
            Reservation reservation = new Reservation
            {
                EmployeeId = newReservationViewModel.EmployeeId,
                SelectedServiceId = newReservationViewModel.SelectedServiceId.Value,
                VehiclePlateNumber = newReservationViewModel.VehiclePlateNumber.ToUpper(),
                Date = newReservationViewModel.Date,
                Comment = newReservationViewModel.Comment,
                CreatedBy = currentUser.Id,
                CreatedOn = now,
            };

            _db.Reservations.Add(reservation);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
            #endregion

            return Ok(reservation.ReservationId);
        }

        private bool CanUserReserve(User currentUser)
        {
            //csak két aktív foglása lehet személyenként a rendszerben
            var query = from b in _db.Reservations
                        where b.Date >= DateTime.Today && b.EmployeeId == currentUser.Id
                        orderby b.Date
                        select b;

            return query.Count() < 2;
        }

        [HttpGet]
        [ResponseType(typeof(Reservation))]
        public async Task<IHttpActionResult> DeleteReservation(int reservationId)
        {
            Reservation reservation = await _db.Reservations.FindAsync(reservationId);
            if (reservation == null)
            {
                return NotFound();
            }
            var currentUser = UserHelper.GetCurrentUser();

            if (!currentUser.IsAdmin)
            {
                if (reservation.EmployeeId != currentUser.Id)
                {
                    return NotFound();
                }

                if (reservation.Date < DateTime.Today)
                {
                    return NotFound();
                }
            }

            _db.Reservations.Remove(reservation);
            await _db.SaveChangesAsync();

            return Ok(reservation);
        }

        [HttpGet]
        [ResponseType(typeof(ReservationViewModel))]
        public async Task<IHttpActionResult> GetReservations()
        {
            ReservationViewModel ret = new ReservationViewModel();

            var currentUser = UserHelper.GetCurrentUser();

            IOrderedQueryable<Reservation> query;
            if (currentUser.IsAdmin)
            {
                var dateFrom = DateTime.Today.AddDays(-90);
                query = from b in _db.Reservations.Include(i => i.Employee)
                        where b.Date >= dateFrom
                        orderby b.Date descending, b.CreatedOn ascending
                        select b;
            }
            else
            {
                query = from b in _db.Reservations.Include(i => i.Employee)
                        where b.EmployeeId == currentUser.Id
                        orderby b.Date descending, b.CreatedOn ascending
                        select b;
            }
            var queryResult = await query.ToListAsync();

            var temp = new List<ReservationDayDetailsViewModel>();
            DateTime day = DateTime.MaxValue;
            foreach (var item in queryResult)
            {
                if (item.Date != day)
                {
                    day = item.Date;
                    ReservationDayDetailsViewModel reservationDayDetailsViewModel = new ReservationDayDetailsViewModel();
                    reservationDayDetailsViewModel.Day = item.Date;
                    reservationDayDetailsViewModel.Reservations = new List<ReservationDayDetailViewModel>();
                    foreach (var resItem in queryResult.FindAll(q => q.Date == item.Date))
                    {
                        reservationDayDetailsViewModel.Reservations.Add(new ReservationDayDetailViewModel
                        {
                            EmployeeId = resItem.EmployeeId,
                            EmployeeName = resItem.Employee.Name,
                            ReservationId = resItem.ReservationId,
                            SelectedServiceName = ((ServiceEnum)resItem.SelectedServiceId).GetDescription(),
                            VehiclePlateNumber = resItem.VehiclePlateNumber,
                            Comment = resItem.Comment,
                            IsDeletable = (resItem.EmployeeId == currentUser.Id && resItem.Date >= DateTime.Today) || currentUser.IsAdmin
                        });
                    }
                    temp.Add(reservationDayDetailsViewModel);
                }
            }

            ret.ReservationsByDayActive = temp.FindAll(t => t.Day >= DateTime.Today).OrderBy(t => t.Day).ToList();
            ret.ReservationsByDayHistory = temp.FindAll(t => t.Day < DateTime.Today).OrderByDescending(t => t.Day).ToList();

            return Ok(ret);
        }

        /// <summary>
        /// Retrieves date of next free, normal slot (excluding today and carpet + steam slots).
        /// </summary>
        /// <returns>Date of next free, normal slot or null if none is available</returns>
        public async Task<DateTime?> GetNextFreeSlotDate()
        {
            var now = DateTime.Now;

            var from = now.Date;
            var until = from.AddDays(14);

            var currentUser = UserHelper.GetCurrentUser();
            var nextReservations = await _db.Reservations.Where(r => r.Date >= from && r.Date < until).ToListAsync<Reservation>();

            for (var day = from; day < until; day = day.AddDays(1))
            {
                // If we are looking the for next free slot, we have to consider if the user already has a reservation for the day.
                if(GetAvailableSlots(nextReservations, day, true).Item1 >= 1)
                {
                    return day;
                }
            }

            return null;
        }

        private bool IsItTooLateForToday()
        {
            var now = DateTime.Now;
            // server time might be different than Budapest time
            now = TimeZoneInfo.ConvertTime(now, TimeZoneInfo.Local, TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time"));

            return now.Hour >= 14;
        }

        /// <summary>
        /// Retrieves the number of available slots from the database.
        /// </summary>
        /// <param name="day">The date that the number of free slots is calculated for.</param>
        /// <returns>Number of available slots.</returns>
        [HttpGet]
        [ResponseType(typeof(int))]
        public async Task<IHttpActionResult> CapacityByDay(DateTime day)
        {
            if (day > DateTime.Today.AddDays(350) || day < DateTime.Today.AddDays(-350))
            {
                return BadRequest();
            }

            // Query the list of reservations on the given day.
            var query = from b in _db.Reservations
                        where b.Date == day
                        select b;
            var queryResult = await query.ToListAsync();
            // Get the number of available slots.
            var slots = GetAvailableSlots(queryResult, day, false);
            int availableSlots = day < DateTime.Today ? 0 : slots.Item1 + slots.Item2;
            // Return the number of available slots.
            return Json(availableSlots);
        }

        /// <summary>
        /// Retrieves the number of available slots from the database.
        /// </summary>
        /// <param name="day">The date that the number of free slots is calculated for.</param>
        /// <returns>Number of available slots.</returns>
        [HttpGet]
        //[ResponseType(typeof(DayDetailsViewModel))]
        public async Task<IHttpActionResult> CapacityForTimeInterval(DateTime startDate, DateTime endDate)
        {
            if (startDate > DateTime.Today.AddDays(350) || 
                startDate < DateTime.Today.AddDays(-350) ||
                endDate > DateTime.Today.AddDays(350) ||
                endDate < DateTime.Today.AddDays(-350) ||
                startDate > endDate)
            {
                return BadRequest();
            }

            DateTime current = startDate;
            List<int> availableSlots = new List<int>();
            int availableSlotsNr;
            while (current <= endDate)
            {
                // Query the list of reservations on the given day.
                var query = from b in _db.Reservations
                            where b.Date == current
                            select b;
                var queryResult = await query.ToListAsync();
                // Get the number of available slots.
                var slots = GetAvailableSlots(queryResult, current, false);
                availableSlotsNr = current < DateTime.Today ? 0 : slots.Item1 + slots.Item2;
                availableSlots.Add(availableSlotsNr);
                current = current.AddDays(1);
            }

            // Return the number of available slots.
            return Json(availableSlots);
        }

        /// <summary>
        /// Checks if the current user can create new reservation (limit has not been exceeded).
        /// </summary>
        /// <returns>True, if the user allowed to create a new reservation.</returns>
        [HttpGet]
        [ResponseType(typeof(bool))]
        public async Task<IHttpActionResult> NewReservationAvailable()
        {
            bool result = false;

            var currentUser = UserHelper.GetCurrentUser();
            DateTime day = DateTime.Today;

            if (!currentUser.IsAdmin)
            {
                #region  Check the number of open reservations
                var query = from b in _db.Reservations.Include(i => i.Employee)
                            where b.Date >= day && b.EmployeeId == currentUser.Id
                            orderby b.Date
                            select b;
                var queryResult = await query.ToListAsync();
                if (queryResult.Count < _reservationLimitPerPerson)
                {
                    result = true;
                }
                #endregion
            }

            // Return the number of available slots.
            return Json(result);
        }

        [HttpPost]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> UpdateReservation(Reservation reservationToUpdate)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUser = UserHelper.GetCurrentUser();
            DateTime now = DateTime.UtcNow;

            #region Find the reservation that must be updated.

            var query = from b in _db.Reservations
                        where b.ReservationId == reservationToUpdate.ReservationId && b.EmployeeId == reservationToUpdate.EmployeeId
                        select b;
            var queryResult = await query.ToListAsync();
            if (queryResult.Count == 0)
            {
                ModelState.AddModelError("", "The requested operation could not be executed (Reservation not found)!");
                return BadRequest(ModelState);
            }
            Reservation reservationInDb = queryResult[0];

            #endregion

            #region Check business rules
            // Check if the selected service has been changed. In that case we need to ensure
            // that there are enough slots available.
            if (reservationToUpdate.SelectedServiceId != reservationInDb.SelectedServiceId)
            {
                var queryReservation = from b in _db.Reservations
                                       where b.Date == reservationToUpdate.Date
                                       select b;
                var queryReservationResult = await queryReservation.ToListAsync();
                var availableSlots = GetAvailableSlots(
                    queryReservationResult, 
                    reservationToUpdate.Date,
                    false);

                // Increase slot numbers according to the previously selected service
                if (reservationInDb.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1 + 2, availableSlots.Item2);
                }
                else if (reservationInDb.SelectedServiceId == (int)ServiceEnum.InteriorSteam ||
                    reservationInDb.SelectedServiceId == (int)ServiceEnum.ExteriorSteam ||
                    reservationInDb.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorSteam)
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1, availableSlots.Item2 + 1);
                }
                else
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1 + 1, availableSlots.Item2);
                }

                // Decrease slot numbers according to the newly selected service
                if (reservationToUpdate.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1 - 2, availableSlots.Item2);
                }
                else if (reservationToUpdate.SelectedServiceId == (int)ServiceEnum.InteriorSteam ||
                    reservationToUpdate.SelectedServiceId == (int)ServiceEnum.ExteriorSteam ||
                    reservationToUpdate.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorSteam)
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1, availableSlots.Item2 - 1);
                }
                else
                {
                    availableSlots = new Tuple<int, int>(availableSlots.Item1 - 1, availableSlots.Item2);
                }
                if (availableSlots.Item1 < 0 || availableSlots.Item2 < 0)
                {
                    ModelState.AddModelError("", "The newly selected service is not available on the selected day! Please select another service or another day!");
                    return BadRequest(ModelState);
                }
            }
            #endregion

            #region Update the VehiclePlateNumber to the current given value
            Employee employee = await _db.Employees.FindAsync(currentUser.Id);
            if (employee == null)
            {
                return NotFound();
            }
            if (employee.VehiclePlateNumber != reservationToUpdate.VehiclePlateNumber)
            {
                employee.VehiclePlateNumber = reservationToUpdate.VehiclePlateNumber.ToUpper();
                employee.ModifiedBy = currentUser.Id;
                employee.ModifiedOn = now;

                _db.Entry(employee).State = EntityState.Modified;

                try
                {
                    await _db.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            #endregion

            #region Update reservation

            reservationInDb.SelectedServiceId = reservationToUpdate.SelectedServiceId;
            reservationInDb.VehiclePlateNumber = reservationToUpdate.VehiclePlateNumber.ToUpper();
            reservationInDb.Comment = reservationToUpdate.Comment;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw;
            }
            #endregion

            return StatusCode(HttpStatusCode.NoContent);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
        
        #region Private methods
        private async Task<List<DayViewModel>> GetDaysDetailsAsync(DateTime startDate, DateTime endDate)
        {
            var query = from b in _db.Reservations
                        where b.Date >= startDate && b.Date <= endDate
                        select b;
            var queryResult = await query.ToListAsync();

            List<DayViewModel> result = new List<DayViewModel>();
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayViewModel = new DayViewModel
                {
                    Day = date,
                    AvailableNormalSlots = date < DateTime.Today ? 0 : GetAvailableSlots(queryResult, date, false).Item1,
                    AvailableSteamSlots = date < DateTime.Today ? 0 : GetAvailableSlots(queryResult, date, false).Item2,
                    IsToday = (date == DateTime.Today)
                };
                var maxAvailableSlotsOnDate = new Tuple<int, int>(0, 0);

                // exclude past
                if (date >= DateTime.Today)
                {
                    maxAvailableSlotsOnDate = GetConfiguredAvailableSlotsOnDate(date);
                }

                dayViewModel.AvailableSlotCount = new List<string>();
                for(var i=0; i< dayViewModel.AvailableNormalSlots + dayViewModel.AvailableSteamSlots; i++)
                {
                    dayViewModel.AvailableSlotCount.Add(i.ToString());
                }

                dayViewModel.ReservedSlotCount = new List<string>();
                for (var i = 0; i < maxAvailableSlotsOnDate.Item1 - dayViewModel.AvailableNormalSlots + maxAvailableSlotsOnDate.Item2 - dayViewModel.AvailableSteamSlots; i++)
                {
                    dayViewModel.ReservedSlotCount.Add(i.ToString());
                }

                result.Add(dayViewModel);
            }

            return result;
        }

        /// <summary>
        /// Calculates the number of available slots for the specified date based on
        /// the given reservation list.
        /// Specify false for the checkDuplicate parameter if the method doesn't have to
        /// check if the user already has a reservation on the specified date.
        /// </summary>
        /// <param name="queryResult"></param>
        /// <param name="date"></param>
        /// <param name="checkDuplicate"></param>
        /// <returns></returns>
        private Tuple<int, int> GetAvailableSlots(
            List<Reservation> queryResult, 
            DateTime date,
            bool checkDuplicate = true)
        {
            if(date.Date == DateTime.Now.Date && IsItTooLateForToday())
            {
                return new Tuple<int, int>(0, 0);
            }

            var currentUser = UserHelper.GetCurrentUser();
            var availableSlots = GetConfiguredAvailableSlotsOnDate(date);
            var availableNormalSlots = availableSlots.Item1;
            var availableSteamSlots = availableSlots.Item2;

            if (availableNormalSlots > 0 || availableSteamSlots > 0)
            {
                var reservationsOnDate = queryResult.FindAll(q => q.Date.Month == date.Month && q.Date.Day == date.Day);

                foreach (var item in reservationsOnDate)
                {
                    if (item.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorCarpet)
                    {
                        availableNormalSlots -= 2;
                    }
                    else if(item.SelectedServiceId == (int)ServiceEnum.ExteriorInteriorSteam ||
                        item.SelectedServiceId == (int)ServiceEnum.ExteriorSteam ||
                        item.SelectedServiceId == (int)ServiceEnum.InteriorSteam)
                    {
                        availableSteamSlots -= 1;
                    }
                    else
                    {
                        availableNormalSlots -= 1;
                    }

                    // ...make sure we don't allow 2 reservations for a single day (unless user is admin)
                    if (item.EmployeeId == currentUser.Id && !currentUser.IsAdmin && checkDuplicate)
                    {
                        availableNormalSlots = availableSteamSlots = 0;
                        break;
                    }
                }
            }

            return new Tuple<int, int>(availableNormalSlots, availableSteamSlots);
        }

        private Tuple<int, int> GetConfiguredAvailableSlotsOnDate(DateTime date)
        {
            var normalSlots = 0;
            var steamSlots = 0;

            if (_exceptionalDays.ContainsKey(date))
            {
                normalSlots = _exceptionalDays[date].Item1;
                steamSlots = _exceptionalDays[date].Item2;
            }
            else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                // leave default zeros
            }
            else
            {
                normalSlots = _slotsPerDay;
                steamSlots = date.DayOfWeek == DayOfWeek.Tuesday ? _steamSlotsOnTuesday : 0;
            }
            
            return new Tuple<int, int>(normalSlots, steamSlots);
        }

        private async Task<HomeViewModel> CreateHomeViewModelAsync(DateTime day, User user)
        {
            HomeViewModel ret = new HomeViewModel();

            ret.Day = day;

            Employee employee = await _db.Employees.FindAsync(user.Id);
            ret.NewReservation = new NewReservationViewModel();
            ret.NewReservation.Date = day;
            ret.NewReservation.EmployeeId = user.Id;
            ret.NewReservation.EmployeeName = user.FullName;
            ret.NewReservation.VehiclePlateNumber = employee != null ? employee.VehiclePlateNumber : "";
            ret.NewReservation.IsAdmin = user.IsAdmin;

            return ret;
        }

        #endregion Private methods

    }
}