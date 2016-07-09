using MSHU.CarWash.UWP.ViewModels;
using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static MSHU.CarWash.UWP.ViewModels.RegistrationsViewModel;

namespace MSHU.CarWash.UWP.Converters
{
    public class StatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType.Equals(typeof(Brush)))
            {
                RegistrationsViewModel vm = value as RegistrationsViewModel;
                DateTimeOffset dt = (DateTimeOffset)parameter;
                if (vm != null)
                {
                    StatusValue status = vm.GetStatus(dt);
                    switch (status)
                    {
                        case StatusValue.NotAvailable:
                            {
                                value = new SolidColorBrush(Colors.LightGray);
                            }
                            break;
                        case StatusValue.Available:
                            {
                                value = new SolidColorBrush(Colors.Green);
                            }
                            break;
                        case StatusValue.Reserved:
                            {
                                value = new SolidColorBrush(Colors.Yellow);
                            }
                            break;
                        case StatusValue.BookedUp:
                            {
                                value = new SolidColorBrush(Colors.Red);
                            }
                            break;
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
