using MSHU.CarWash.DomainModel;
using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

        public object FreeSlots => this;

        public RegistrationsViewModel()
        {
            // Create and execute the RequestService command.
            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);
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
        /// Event handler for the Executed event of the SignOutWithAADCommand.
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

    }
}
