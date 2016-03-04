using MSHU.CarWash.DomainModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class RegistrationsViewModel : Bindable
    {
        /// <summary>
        /// Holds a reference to the ReservationViewModel. It is used for looking
        /// up the number of reservations for a given date.
        /// </summary>
        private ReservationViewModel _rmv;

        private object _freeSlots;

        private bool _useDetailsView;

        private ReservationDayDetailsViewModel _selectedDayDetails;
        private string _selectedDate;

        /// <summary>
        /// Private fields holds a reference to the reservation instance.
        /// The field has a value - not null - if the current user has
        /// a reservation for the currently selected day (SelectedDayDetails.Day).
        /// The field is null if the user has no reservation.
        /// 
        /// If the user doesn't have any reservation and he/she creates a new one
        /// then this field is assigned with the new instance of Reservation.
        /// </summary>
        private ReservationDayDetailViewModel _currentReservation;

        /// <summary>
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

        /// <summary>
        /// Gets or sets the CreateReservationsCommand.
        /// </summary>
        public RelayCommand CreateReservationCommand { get; set; }

        /// <summary>
        /// Gets or sets the CancelReservationChangesCommand.
        /// </summary>
        public RelayCommand CancelReservationChangesCommand { get; set; }


        /// <summary>
        /// Gets or sets the SaveReservationChangesCommand.
        /// </summary>
        public RelayCommand SaveReservationChangesCommand { get; set; }

        public object FreeSlots => this;

        public bool UseDetailsView
        {
            get
            {
                return _useDetailsView;
            }

            set
            {
                _useDetailsView = value;
                OnPropertyChanged("UseDetailsView");
                OnPropertyChanged("UseMasterView");
                if (!value)
                {
                    // The user navigated back to the master view so clear the selected
                    // date relating properties!
                    SelectedDayDetails = null;
                    CurrentReservation = null;
                }
            }
        }

        public bool UseMasterView => !_useDetailsView;

        public ReservationDayDetailsViewModel SelectedDayDetails
        {
            get
            {
                return _selectedDayDetails;
            }
            private set
            {
                _selectedDayDetails = value;
            }
        }

        /// <summary>
        /// The command that activates
        /// </summary>
        public RelayCommand ActivateDetailsCommand { get; set; }

        public string SelectedDate
        {
            get
            {
                return _selectedDate;
            }
            private set
            {
                _selectedDate = value;
                OnPropertyChanged("SelectedDate");
            }
        }

        /// <summary>
        /// Private fields holds a reference to the reservation instance.
        /// The field has a value - not null - if the current user has
        /// a reservation for the currently selected day (SelectedDayDetails.Day).
        /// The field is null if the user has no reservation.
        /// 
        /// If the user doesn't have any reservation and he/she creates a new one
        /// then this field is assigned with the new instance of Reservation.
        /// </summary>
        public ReservationDayDetailViewModel CurrentReservation
        {
            get
            {
                return _currentReservation;
            }

            set
            {
                _currentReservation = value;
                OnPropertyChanged("CurrentReservation");
            }
        }

        public RegistrationsViewModel()
        {
            UseDetailsView = false;

            // Create and execute the RequestService command.
            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);

            // Create the ActivateDetailsCommand.
            ActivateDetailsCommand = new RelayCommand(ExecuteActivateDetailsCommand);

            // Create the CreateReservationCommand.
            CreateReservationCommand = new RelayCommand(ExecuteCreateReservationCommand);

            // Create the CancelReservationChangesCommand.
            CancelReservationChangesCommand = new RelayCommand(ExecuteCancelReservationChangesCommand);

            // Create the SaveReservationChangesCommand.
            SaveReservationChangesCommand = new RelayCommand(ExecuteSaveReservationChangesCommand);
        }


        /// <summary>
        /// Retrieves the number of reservations for the given date.
        /// </summary>
        /// <param name="date">The date the number of reservations requested for.</param>
        /// <returns>Number of reservations.</returns>
        public string GetReservationCountByDay(DateTimeOffset date)
        {
            string result = "?";
            if (_rmv != null)
            {
                int number = -1;
                if (date.CompareTo(DateTimeOffset.Now) < 0)
                {
                    number = GetReservationCountFromList(date, _rmv.ReservationsByDayHistory);
                }
                else
                {
                    number = GetReservationCountFromList(date, _rmv.ReservationsByDayActive);
                }
                result = $"{App.MAX_RESERVATIONS_PER_DAY - number}";

            }
            return result;
        }

        private int GetReservationCountFromList(DateTimeOffset date, List<ReservationDayDetailsViewModel> list)
        {
            int number;
            // Let's check if there are reservations for the specified day at all!
            var day = list.Find(x => x.Day.Equals(date.Date));
            if (day == null)
            {
                // No reservations.
                number = 0;
            }
            else
            {
                // Get the number of reservations.
                var reservations = day.Reservations.Count;
                number = reservations;
            }

            return number;
        }

        /// <summary>
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteRequestServiceCommand(object param)
        {
            _rmv = await ServiceClient.ServiceClient.GetReservations(
                App.AuthenticationManager.BearerAccessToken);
            if (_rmv != null)
            {
                OnPropertyChanged("FreeSlots");
            }
        }

        /// <summary>
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// The handler sets the UseDetailsView property to true and also sets
        /// the SelectedDayDetails property, correspondingly. The latter is used
        /// for data binding UI elements.
        /// </summary>
        /// <param name="param"></param>
        private void ExecuteActivateDetailsCommand(object param)
        {
            DateTimeOffset selectedDate = (DateTimeOffset)param;
            if (selectedDate.CompareTo(DateTimeOffset.Now) < 0)
            {
                SelectedDayDetails =
                    _rmv.ReservationsByDayHistory.Find(x => x.Day.Equals(selectedDate.Date));
            }
            else
            {
                SelectedDayDetails =
                    _rmv.ReservationsByDayActive.Find(x => x.Day.Equals(selectedDate.Date));
            }
            SelectedDate = selectedDate.ToString("D");

            if (SelectedDayDetails != null)
            {
                // Check if the user has reservation for the selected date.
                CurrentReservation = SelectedDayDetails.Reservations.Find(
                    x => x.EmployeeId.Equals(App.AuthenticationManager.UserData.DisplayableId));
            }

            UseDetailsView = true;
        }

        private void ExecuteCreateReservationCommand(object param)
        {
            CurrentReservation = new ReservationDayDetailViewModel();
        }

        private void ExecuteCancelReservationChangesCommand(object param)
        {

        }

        private void ExecuteSaveReservationChangesCommand(object param)
        {

        }

    }
}
