using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWash.Services.Helpers
{
    public static class DateTimeExtension
    {
        public static DateTime GetFirstDayOfWeek(this DateTime date)
        {
            while (date.DayOfWeek != DayOfWeek.Monday)
            {
                date = date.AddDays(-1);
            }

            return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
        }
    }
}