using MSHU.CarWashService.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWash.Services.DataObjects
{
    public class WeekDto
    {
        public DateTime StartOfWeek { get; set; }
        public string DateInterval { get; set; }
        public List<DayDto> Days { get; set; }
        public int Offset { get; set; }
        public int NextWeekOffset { get; set; }
        public int PreviousWeekOffset { get; set; }
    }

}