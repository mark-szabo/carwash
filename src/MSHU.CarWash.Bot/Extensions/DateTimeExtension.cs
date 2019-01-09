using System;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace MSHU.CarWash.ClassLibrary.Extensions
{
    /// <summary>
    /// Extension class for DateTime.
    /// </summary>
    public static class DateTimeExtension
    {
        /// <summary>
        /// Converts the DateTime object to natural languge with the given reference point.
        /// </summary>
        /// <param name="dateTime">the DateTime object.</param>
        /// <param name="referenceDate">(Optional) Reference point. Defaults to DateTime.Now.</param>
        /// <returns>
        /// Natural languge string of the DateTime.
        /// </returns>
        public static string ToNaturalLanguage(this DateTime dateTime, DateTime? referenceDate = null)
        {
            if (referenceDate == null) referenceDate = DateTime.Now;
            var timex = TimexProperty.FromDateTime(dateTime);
            return timex.ToNaturalLanguage(referenceDate.Value);
        }
    }
}
