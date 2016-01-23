using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class ReservationDayDetailsDto
    {
        public DateTime Day { get; set; }
        public string DayName
        {
            get
            {
                return this.Day.ToString("dddd", CultureInfo.CreateSpecificCulture("hu-HU")).ToUpper();
            }
        }
        public string MonthName
        {
            get
            {
                return this.Day.ToString("MMMM", CultureInfo.CreateSpecificCulture("hu-HU")).ToUpper();
            }
        }
        public int DayNumber
        {
            get
            {
                return this.Day.Day;
            }
        }
        public List<ReservationDayDetailDto> Reservations { get; set; }
    }
}