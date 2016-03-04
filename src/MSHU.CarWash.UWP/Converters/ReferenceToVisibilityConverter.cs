using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace MSHU.CarWash.UWP.Converters
{
    /// <summary>
    /// ReferenceToVisibilityConverter is an implementation of IValueConverter that
    /// converter a reference to Visibility.
    /// If the reference is null then it returns Visibility.Collapsed; otherwise Visibility.Visible.
    /// </summary>
    public class ReferenceToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (targetType.Equals(typeof(Visibility)))
            {
                if (value == null)
                {
                    return Visibility.Collapsed;
                }
                else
                {
                    return Visibility.Visible;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
