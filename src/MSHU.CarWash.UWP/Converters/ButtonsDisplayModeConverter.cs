using MSHU.CarWash.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MSHU.CarWash.UWP.Converters
{
    public class ButtonsDisplayModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ReservationDayDetailViewModel vm = value as ReservationDayDetailViewModel;
            // If the reservation hasn't got any id yet then this is a newly created one so
            // display the 'Save' and 'Cancel' buttons.
            if (vm?.ReservationId == 0)
            {
                return Controls.ReservationDetailsView.ButtonsDisplayMode.SaveCancel;
            }
            else
            {
                return Controls.ReservationDetailsView.ButtonsDisplayMode.Delete;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
