using MSHU.CarWash.UWP.Common.UI;
using MSHU.CarWash.UWP.Converters;
using MSHU.CarWash.UWP.ViewModels;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MSHU.CarWash.UWP.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ReservationsPage : BasePage
    {
        private static CalendarViewDayItemConverter _converter;
        private static StatusConverter _statusConverter;

        /// <summary>
        /// True if user caused switch from master to detail view.
        /// </summary>
        private bool comingFromMasterView;

        public ReservationsPage()
        {
            _converter = new CalendarViewDayItemConverter();
            _statusConverter = new StatusConverter();
            this.InitializeComponent();
        }

        protected override void InitializePage()
        {
            ViewModel = new RegistrationsViewModel();

            base.InitializePage();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            (ViewModel as RegistrationsViewModel).SmartGoBackRequested += ReservationsPage_SmartGoBackRequested;
            if (e.Parameter is DateTime)
            {
                (ViewModel as RegistrationsViewModel).CreateReservationCommand.Execute(e.Parameter);
                (ViewModel as RegistrationsViewModel).UseDetailsView = true;
            }
        }

        /// <summary>
        /// Handles smart go back reqeusts from view
        /// </summary>
        private void ReservationsPage_SmartGoBackRequested(object sender, EventArgs e)
        {
            if(comingFromMasterView)
            {
                // switch to master view
                (ViewModel as RegistrationsViewModel).UseDetailsView = false;
            }
            else
            {
                // go back to previous page
                AppShell.Current.GoBack();
            }
        }

        private void CalendarView_CalendarViewDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            CalendarViewDayItem item = args.Item;
            DateTimeOffset date = item.Date;

            if (item.DataContext == null)
            {
                item.DataContext = this.ViewModel;
            }

            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                item.IsBlackout = true;
            }

            TextBlock tb = UIHelper.FindVisualChild<TextBlock, string>(
                item, NameProperty, "FreeSlotsTextBox");

            if (tb != null)
            {
                Binding textBinding = new Binding();
                if (tb.DataContext is CalendarViewDayItem)
                {
                    textBinding.Path = new PropertyPath("DataContext.FreeSlots");
                }
                else if (tb.DataContext is RegistrationsViewModel)
                {
                    textBinding.Path = new PropertyPath("FreeSlots");
                }
                else
                {
                    throw new InvalidOperationException("Inadequate control hierarchy in CalendarViewDayItem's template!");
                }
                textBinding.Converter = _converter;
                textBinding.ConverterParameter = item.Date;
                tb.SetBinding(TextBlock.TextProperty, textBinding);
            }

            Ellipse statusControl = UIHelper.FindVisualChild<Ellipse, string>(
                item, NameProperty, "StatusControl");
            if (statusControl != null)
            {
                Binding fillBinding = new Binding();
                if (statusControl.DataContext is CalendarViewDayItem)
                {
                    fillBinding.Path = new PropertyPath("DataContext.DayStatus");
                }
                else if (statusControl.DataContext is RegistrationsViewModel)
                {
                    fillBinding.Path = new PropertyPath("DayStatus");
                }
                else
                {
                    throw new InvalidOperationException("Inadequate control hierarchy in CalendarViewDayItem's template!");
                }
                fillBinding.Converter = _statusConverter;
                fillBinding.ConverterParameter = item.Date;
                statusControl.SetBinding(Ellipse.FillProperty, fillBinding);
            }

            //var tb = item.FindName("FreeSlotsTextBox");
            //tb = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(item, 0), 0), 0);

            // Render basic day items.
            if (args.Phase == 0)
            {
                // Register callback for next phase.
                args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
            }
            // Set blackout dates.
            else if (args.Phase == 1)
            {
                // Blackout dates in the past, Sundays, and dates that are fully booked.
                //if (args.Item.Date < DateTimeOffset.Now ||
                //    args.Item.Date.DayOfWeek == DayOfWeek.Sunday ||
                //    Bookings.HasOpenings(args.Item.Date) == false)
                //{
                //    args.Item.IsBlackout = true;
                //}

                //// Register callback for next phase.
                //args.RegisterUpdateCallback(CalendarView_CalendarViewDayItemChanging);
            }
            // Set density bars.
            else if (args.Phase == 2)
            {
                // Avoid unnecessary processing.
                // You don't need to set bars on past dates or Sundays.
                //if (args.Item.Date > DateTimeOffset.Now &&
                //    args.Item.Date.DayOfWeek != DayOfWeek.Sunday)
                //{
                //    // Get bookings for the date being rendered.
                //    var currentBookings = Bookings.GetBookings(args.Item.Date);

                //    List<Color> densityColors = new List<Color>();
                //    // Set a density bar color for each of the days bookings.
                //    // It's assumed that there can't be more than 10 bookings in a day. Otherwise,
                //    // further processing is needed to fit within the max of 10 density bars.
                //    foreach (booking in currentBookings)
                //    {
                //        if (booking.IsConfirmed == true)
                //        {
                //            densityColors.Add(Colors.Green);
                //        }
                //        else
                //        {
                //            densityColors.Add(Colors.Blue);
                //        }
                //    }
                //    args.Item.SetDensityColors(densityColors);
                //}
            }

        }

        /// <summary>
        /// The event handler send the ActivateDetailsCommand to the Viewmodel (transmitting
        /// the selected date as command parameter).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CalendarView_SelectedDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            comingFromMasterView = true;
            if (args.AddedDates.Count == 1)
            {
                DateTimeOffset selectedDate = args.AddedDates[0];

                var viewModel = (RegistrationsViewModel)ViewModel;
                if (viewModel.ActivateDetailsCommand.CanExecute(selectedDate))
                {
                    viewModel.ActivateDetailsCommand.Execute(selectedDate);
                }
            }
        }

        private void GoToMasterViewButton_Click(object sender, RoutedEventArgs e)
        {
            ReservationsPage_SmartGoBackRequested(this, null);
        }
    }
}
