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
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

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
                OnPropertyChanged("SelectedDate");
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

        public string SelectedDate => _selectedDate;

        public RegistrationsViewModel()
        {
            UseDetailsView = false;

            // Create and execute the RequestService command.
            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);

            // Create the ActivateDetailsCommand
            ActivateDetailsCommand = new RelayCommand(ExecuteActivateDetailsCommand);
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
        private async void ExecuteActivateDetailsCommand(object param)
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
            _selectedDate = selectedDate.ToString("D");

            UseDetailsView = true;

        }

    }
}
