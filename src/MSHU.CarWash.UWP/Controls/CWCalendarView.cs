using MSHU.CarWash.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace MSHU.CarWash.UWP.Controls
{
    public sealed class CWCalendarView : CalendarView
    {
        // Using a DependencyProperty as the backing store for PreviousButtonCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviousButtonCommandProperty =
            DependencyProperty.Register("PreviousButtonCommand", typeof(RelayCommand), typeof(CWCalendarView), new PropertyMetadata(null));

        // Using a DependencyProperty as the backing store for NextButtonCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty NextButtonCommandProperty =
            DependencyProperty.Register("NextButtonCommand", typeof(RelayCommand), typeof(CWCalendarView), new PropertyMetadata(null));

        public RelayCommand PreviousButtonCommand
        {
            get { return (RelayCommand)GetValue(PreviousButtonCommandProperty); }
            set { SetValue(PreviousButtonCommandProperty, value); }
        }

        public RelayCommand NextButtonCommand
        {
            get { return (RelayCommand)GetValue(NextButtonCommandProperty); }
            set { SetValue(NextButtonCommandProperty, value); }
        }

        public CWCalendarView()
        {
            this.DefaultStyleKey = typeof(CWCalendarView);
        }
    }
}
