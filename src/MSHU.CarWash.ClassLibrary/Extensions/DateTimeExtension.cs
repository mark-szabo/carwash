using System;

namespace MSHU.CarWash.ClassLibrary.Extensions
{
    /// <summary>
    /// Extension class for DateTime
    /// </summary>
    public static class DateTimeExtension
    {
        /// <summary>
        /// Indicates whether this instance of <see cref="DateTime"/> is on a weekend.
        /// </summary>
        /// <param name="dateTime">the DateTime object</param>
        /// <returns>
        /// True if the value of the <see cref="DayOfWeek"/> property is <see cref="DayOfWeek.Saturday"/> or <see cref="DayOfWeek.Saturday"/>.
        /// </returns>
        public static bool IsWeekend(this DateTime dateTime)
        {
            return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
        }
    }
}
