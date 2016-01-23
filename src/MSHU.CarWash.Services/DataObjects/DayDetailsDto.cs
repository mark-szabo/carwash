using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class DayDetailsDto
    {
        public int Offset { get; set; }
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
        public int AvailableSlots { get; set; }

        public bool ReservationIsAllowed { get; set; }

        public List<ReservationDetailDto> Reservations { get; set; }


        public NewReservationDto NewReservation { get; set; }

    }
}