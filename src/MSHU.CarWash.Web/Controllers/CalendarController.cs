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

namespace MSHU.CarWash.Controllers
{
    [Authorize]
    [RequireHttps]
    public class CalendarController : ApiController
    {
        #region private fields
        private readonly MSHUCarWashContext _db;
        private readonly int _slotsPerDay;
        private readonly Dictionary<DateTime, int> _exceptionalDays;
        private readonly int _monthlyLimitPerPerson;
        #endregion private fields

        public CalendarController()
        {
            _db = new MSHUCarWashContext();
            _slotsPerDay = Convert.ToInt32(ConfigurationManager.AppSettings["SlotsPerDay"]);
            _exceptionalDays = new Dictionary<DateTime, int>();
            var exceptionalDays = ConfigurationManager.AppSettings["ExceptionalDays"].Split(';');
            foreach (var item in exceptionalDays)
            {
                if (!string.IsNullOrWhiteSpace(item) && item.Contains(':'))
                {
                    var splitted = item.Split(':');
                    _exceptionalDays.Add(Convert.ToDateTime(splitted[0]), Convert.ToInt32(splitted[1]));
                }
            }
            _monthlyLimitPerPerson = Convert.ToInt32(ConfigurationManager.AppSettings["MontlySlotLimitPerPerson"]);
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
                ret.DateInterval = string.Format("{0} {1} - {2} {3}", ret.StartOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("hu-HU")), ret.StartOfWeek.Day, endOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("hu-HU")), endOfWeek.Day).ToUpper();
            }
            else
            {
                ret.DateInterval = string.Format("{0} {1} - {2}", ret.StartOfWeek.ToString("MMMM", CultureInfo.CreateSpecificCulture("hu-HU")), ret.StartOfWeek.Day, endOfWeek.Day).ToUpper();
            }
            ret.Days = await GetDaysDetailsAsync(ret.StartOfWeek);

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


            ret.AvailableSlots = day < DateTime.Today ? 0 : GetAvailableSlots(queryResult, day);

            ret.Reservations = queryResult
                                .Select(s => new ReservationDetailViewModel
                                {
                                    Date = s.Date,
                                    EmployeeId = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.EmployeeId : "",
                                    EmployeeName = (s.EmployeeId == currentUser.Id  || currentUser.IsAdmin) ? s.Employee.Name : "",
                                    ReservationId = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.ReservationId : 0,
                                    SelectedServiceName = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ?  ((ServiceEnum)s.SelectedServiceId).GetDescription() : "",
                                    VehiclePlateNumber = s.VehiclePlateNumber,
                                    Comment = (s.EmployeeId == currentUser.Id || currentUser.IsAdmin) ? s.Comment : "",
                                    IsDeletable = (s.EmployeeId == currentUser.Id && s.Date > DateTime.Today) || currentUser.IsAdmin
                                })
                                .ToList<ReservationDetailViewModel>();
                       
            ret.ReservationIsAllowed = (ret.AvailableSlots > 0);

            if (ret.ReservationIsAllowed && !currentUser.IsAdmin)
            {
                query = from b in _db.Reservations
                        where b.EmployeeId == currentUser.Id
                        orderby b.Date >= DateTime.Today
                        select b;
                queryResult = await query.ToListAsync();

                ret.ReservationIsAllowed = !queryResult.Any();
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
                //csak egy aktív foglása lehet személyenként a rendszerben
                var query = from b in _db.Reservations
                            where b.Date >= DateTime.Today && b.EmployeeId == newReservationViewModel.EmployeeId
                            orderby b.Date
                            select b;
                var queryResult = await query.ToListAsync();
                if (queryResult.Count > 0)
                {
                    ModelState.AddModelError("", string.Format("Van már foglalt időpontod {0}-n!", queryResult.FirstOrDefault().Date));
                    return BadRequest(ModelState);
                }

                //havi limit ellenőrzés személyenként
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                query = from b in _db.Reservations
                        where b.Date >= startOfMonth && b.EmployeeId == newReservationViewModel.EmployeeId
                        orderby b.Date
                        select b;
                queryResult = await query.ToListAsync();
                int sum = 0;
                foreach (var item in queryResult)
                {
                    if (item.SelectedServiceId == (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas)
                        sum += 2;
                    else
                        sum += 1;
                }
                if (newReservationViewModel.SelectedServiceId == (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas)
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
            var availableSlots = GetAvailableSlots(queryReservationResult, newReservationViewModel.Date);
            if (newReservationViewModel.SelectedServiceId == (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas)
            {
                availableSlots = availableSlots - 2;
            }
            else
            {
                availableSlots = availableSlots - 1;
            }
            if (availableSlots < 0)
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

            return StatusCode(HttpStatusCode.NoContent);
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

                if (reservation.Date <= DateTime.Today)
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
                            IsDeletable = (resItem.EmployeeId == currentUser.Id && resItem.Date > DateTime.Today) || currentUser.IsAdmin
                        });
                    }
                    temp.Add(reservationDayDetailsViewModel);
                }
            }

            ret.ReservationsByDayActive = temp.FindAll(t => t.Day >= DateTime.Today);
            ret.ReservationsByDayHistory = temp.FindAll(t => t.Day < DateTime.Today);

            return Ok(ret);
        }

        /// <summary>
        /// Retrieves date of next free slot (excluding today).
        /// </summary>
        /// <returns>Date of next free slot or null if none is available</returns>
        public async Task<DateTime?> GetNextFreeSlotDate()
        {
            var tomorrow = DateTime.Now.Date.AddDays(1);
            var until = tomorrow.AddDays(14);

            var nextReservations = _db.Reservations.Where(r => r.Date >= tomorrow && r.Date < until);

            for (var day = tomorrow; day < until; day = day.AddDays(1))
            {
                if(GetAvailableSlots(await nextReservations.ToListAsync<Reservation>(), day) >= 2)
                {
                    return day;
                }
            }

            return null;
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
            int availableSlots = day < DateTime.Today ? 0 : GetAvailableSlots(queryResult, day);
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
                availableSlotsNr = current < DateTime.Today ? 0 : GetAvailableSlots(queryResult, current);
                availableSlots.Add(availableSlotsNr);
                current = current.AddDays(1);
            }

            // Return the number of available slots.
            return Json(availableSlots);
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
        private async Task<List<DayViewModel>> GetDaysDetailsAsync(DateTime startOfWeek)
        {
            DateTime endOfWeek = startOfWeek.AddDays(7);
            var query = from b in _db.Reservations
                        where b.Date >= startOfWeek && b.Date < endOfWeek
                        select b;
            var queryResult = await query.ToListAsync();

            List<DayViewModel> result = new List<DayViewModel>();
            for (DateTime date = startOfWeek; date <= startOfWeek.AddDays(6); date = date.AddDays(1))
            {
                var dayViewModel = new DayViewModel
                {
                    Day = date,
                    AvailableSlots = date < DateTime.Today ? 0 : GetAvailableSlots(queryResult, date),
                    IsToday = (date == DateTime.Today)
                };
                int maxAvailableSlotsOnDate = 0;
                if (date >= DateTime.Today)
                {
                    maxAvailableSlotsOnDate = GetConfiguredAvailableSlotsOnDate(date);
                }
                dayViewModel.AvailableSlotCount = new List<string>();
                for(var i=0; i< dayViewModel.AvailableSlots; i++)
                {
                    dayViewModel.AvailableSlotCount.Add(i.ToString());
                }
                dayViewModel.ReservedSlotCount = new List<string>();
                for (var i = 0; i < maxAvailableSlotsOnDate - dayViewModel.AvailableSlots; i++)
                {
                    dayViewModel.ReservedSlotCount.Add(i.ToString());
                }

                result.Add(dayViewModel);
            }

            return result;
        }

        private int GetAvailableSlots(List<Reservation> queryResult, DateTime date)
        {
            var availableSlots = GetConfiguredAvailableSlotsOnDate(date);
            var reservationsOnDate = queryResult.FindAll(q => q.Date.Month == date.Month && q.Date.Day == date.Day);

            foreach (var item in reservationsOnDate)
            {
                if (item.SelectedServiceId == (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas)
                {
                    availableSlots = availableSlots - 2;
                }
                else
                {
                    availableSlots = availableSlots - 1;
                }
            }

            return availableSlots;
        }

        private int GetConfiguredAvailableSlotsOnDate(DateTime date)
        {
            int result = 0;

            if (_exceptionalDays.ContainsKey(date))
            {
                result = _exceptionalDays[date];
            }
            else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                result = 0;
            }
            else
            {
                result = _slotsPerDay;
            }

            return result;
        }

        #endregion Private methods

    }
}