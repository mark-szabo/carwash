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
            CalendarViewDayItem item = value as CalendarViewDayItem;
            if (item != null)
            {
                if (item.DataContext == null)
                {
                    FrameworkElement parent = VisualTreeHelper.GetParent(item) as FrameworkElement;
                    RegistrationsViewModel vm = parent?.DataContext as RegistrationsViewModel;
                    if (vm != null)
                    {
                        item.DataContext = vm;
                        value = vm.GetReservationCountByDay(item.Date);
                    }

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
