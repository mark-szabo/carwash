using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class DayDto
    {
        public DateTime Day { get; set; }
        public string DayName
        {
            get
            {
                return this.Day == DateTime.Today ? "MA" : this.Day.ToString("dddd", CultureInfo.CreateSpecificCulture("hu-HU")).ToUpper();
            }
        }
        public int DayNumber
        {
            get
            {
                return this.Day.Day;
            }
        }

        public bool IsToday { get; set; }
        public int AvailableSlots { get; set; }
        public List<string> AvailableSlotCount { get; set; }
        public List<string> ReservedSlotCount { get; set; }
    }

}