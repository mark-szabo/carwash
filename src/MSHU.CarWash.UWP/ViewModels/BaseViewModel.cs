using MSHU.CarWash.DomainModel;
using MSHU.CarWash.UWP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// Base class for the viewmodels.
    /// </summary>
    public abstract class BaseViewModel : Bindable
    {
        // The event indicates if the user has signed out.
        public event EventHandler UserSignedOut;

        /// <summary>
        /// Gets or sets the SignOutWithAADCommand.
        /// </summary>
        public RelayCommand SignOutWithAADCommand { get; set; }

        // Appointment management is disabled due to current platform issues with the built-in Calendar App
        protected IAppointmentService appointmentService = new DummyAppointmentService();

        public BaseViewModel()
        {
            // Initialize the SignOutWithAADCommand.
            SignOutWithAADCommand = new RelayCommand(ExecuteSignOutWithAADCommand);
        }

        /// <summary>
        /// Event handler for the Executed event of the SignOutWithAADCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteSignOutWithAADCommand(object param)
        {
            bool success = await App.AuthenticationManager.SignOutWithAAD();
            if (success)
            {
                if (UserSignedOut != null)
                {
                    UserSignedOut(this, new EventArgs());
                }
            }
        }

        /// <summary>
        /// Helper method until we get rid of NewReservationViewModel used as model
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="reservationID"></param>
        /// <returns></returns>
        protected static Reservation CreateReservationFromViewModel(NewReservationViewModel reservation, int reservationID)
        {
            return new Reservation()
            {
                ReservationId = reservationID,
                Comment = reservation.Comment,
                Date = reservation.Date,
                SelectedServiceId = reservation.SelectedServiceId.Value,
                VehiclePlateNumber = reservation.VehiclePlateNumber
            };
        }
    }
}
