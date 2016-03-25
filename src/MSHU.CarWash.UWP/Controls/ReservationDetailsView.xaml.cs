using MSHU.CarWash.UWP.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MSHU.CarWash.UWP.Controls
{
    public sealed partial class ReservationDetailsView : UserControl
    {
        public enum ButtonsDisplayMode
        {
            SaveCancel,
            Delete
        }

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

        public ButtonsDisplayMode DisplayMode
        {
            get { return (ButtonsDisplayMode)GetValue(DisplayModeProperty); }
            set { SetValue(DisplayModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DisplayMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisplayModeProperty = DependencyProperty.Register(
            "DisplayMode", 
            typeof(ButtonsDisplayMode), 
            typeof(ReservationDetailsView), 
            new PropertyMetadata(ButtonsDisplayMode.Delete, OnDisplayModeChanged));

        public RelayCommand DeleteCommand
        {
            get { return (RelayCommand)GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DeleteCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register("DeleteCommand", typeof(RelayCommand), typeof(ReservationDetailsView), new PropertyMetadata(null));

        public ReservationDetailsView()
        {
            this.InitializeComponent();
        }

        private static void OnDisplayModeChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ((ReservationDetailsView)sender).OnDisplayModeChanged(args);
        }

        private void OnDisplayModeChanged(DependencyPropertyChangedEventArgs args)
        {
            ButtonsDisplayMode mode = (ButtonsDisplayMode)args.NewValue;
            if (mode == ButtonsDisplayMode.Delete)
            {
                DeleteButton.Visibility = Visibility.Visible;
                SaveButton.Visibility = Visibility.Collapsed;
                CancelButton.Visibility = Visibility.Collapsed;
            }
            else if (mode == ButtonsDisplayMode.SaveCancel)
            {
                DeleteButton.Visibility = Visibility.Collapsed;
                SaveButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Visible;
            }
        }
    }
}
