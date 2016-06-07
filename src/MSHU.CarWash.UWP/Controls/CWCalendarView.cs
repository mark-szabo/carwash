using MSHU.CarWash.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace MSHU.CarWash.UWP.Controls
{
    public sealed class CWCalendarView : CalendarView
    {
        public CWCalendarView()
        {
            this.DefaultStyleKey = typeof(CWCalendarView);
        }
    }
}
