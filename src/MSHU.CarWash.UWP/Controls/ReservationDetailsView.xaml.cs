using MSHU.CarWash.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MSHU.CarWash.UWP.Controls
{
    public sealed partial class ReservationDetailsView : UserControl
    {
        public RelayCommand CancelCommand
        {
            get { return (RelayCommand)GetValue(CancelCommandProperty); }
            set { SetValue(CancelCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CancelCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register(
            "CancelCommand",
            typeof(RelayCommand), 
            typeof(ReservationDetailsView), 
            new PropertyMetadata(null));


        public RelayCommand SaveCommand
        {
            get { return (RelayCommand)GetValue(SaveCommandProperty); }
            set { SetValue(SaveCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SaveCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SaveCommandProperty = DependencyProperty.Register(
            "SaveCommand", 
            typeof(RelayCommand), 
            typeof(ReservationDetailsView), 
            new PropertyMetadata(null));

        public ReservationDetailsView()
        {
            this.InitializeComponent();
        }
    }
}
