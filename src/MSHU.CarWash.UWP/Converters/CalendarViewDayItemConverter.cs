using MSHU.CarWash.UWP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace MSHU.CarWash.UWP.Converters
{
    public class CalendarViewDayItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType.Equals(typeof(string)))
            {
                RegistrationsViewModel vm = value as RegistrationsViewModel;
                DateTimeOffset dt = (DateTimeOffset)parameter;
                if (vm != null)
                {
                    if (dt.Date < DateTime.Now.Date)
                    {
                        return String.Empty;
                    }
                    var freeCount = vm.GetReservationCountByDay(dt);
                    value = freeCount > 0 ? $"{freeCount} free" : "Taken";
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
