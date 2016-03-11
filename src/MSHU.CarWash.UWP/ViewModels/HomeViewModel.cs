using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MSHU.CarWash.DomainModel;
using System;
using System.Globalization;
using System.Text;
using Windows.ApplicationModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class HomeViewModel : Bindable
    {
        private string _givenName;
        private string _familyName;
        private string _displayableID;
        private string m_RegistrationInfo;

        /// <summary>
        /// Indicates if user has a resevartion
        /// </summary>
        public bool ReservationAvailable { get
            {
                return reservationAvailable;
            }
            set
            {
                reservationAvailable = value;
                OnPropertyChanged(nameof(ReservationAvailable));
            }
        }
        private bool reservationAvailable;

        /// <summary>
        /// Holds car's numberplate (of first reservation)
        /// </summary>
        public string NumberPlate { get
            {
                return numberPlate;
            }
            set
            {
                numberPlate = value;
                OnPropertyChanged(nameof(NumberPlate));
            }
        }
        private string numberPlate;

        /// <summary>
        /// Holds date of first reservation
        /// </summary>
        public string ReservationDateString
        {
            get
            {
                return reservationDateString;

            }
            set
            {
                reservationDateString = value;
                OnPropertyChanged(nameof(ReservationDateString));
            }
        }
        private string reservationDateString;

        public event EventHandler UserSignedOut;

        /// <summary>
        /// Gets the given name of the user.
        /// </summary>
        public string GivenName
        {
            get
            {
                return _givenName;
            }
            set
            {
                _givenName = value;
                OnPropertyChanged("GivenName");
            }
        }

        /// <summary>
        /// Gets the family name of the user.
        /// </summary>
        public string FamilyName
        {
            get
            {
                return _familyName;
            }
            private set
            {
                _familyName = value;
                OnPropertyChanged("FamilyName");
            }
        }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public string Email
        {
            get
            {
                return _displayableID;
            }
            private set
            {
                _displayableID = value;
                OnPropertyChanged("Email");
            }
        }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public string RegistrationInfo
        {
            get
            {
                return this.m_RegistrationInfo;
            }
            private set
            {
                this.m_RegistrationInfo = value;
                OnPropertyChanged("RegistrationInfo");
            }
        }

        /// <summary>
        /// Gets or sets the SignOutWithAADCommand.
        /// </summary>
        public RelayCommand SignOutWithAADCommand { get; set; }

        /// <summary>
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

        /// <summary>
        /// Gets or sets the DeleteReservationCommand.
        /// </summary>
        public RelayCommand DeleteReservationCommand { get; set; }

        /// <summary>
        /// Default constructor initializes instance state.
        /// </summary>
        public HomeViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                GivenName = "Béla";
                FamilyName = "Példa";
                Email = "bpelda@microsoft.com";

                ReservationAvailable = true;
                reservationDateString = (DateTime.Now + TimeSpan.FromDays(1)).ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
                NumberPlate = "MS-0001";

                return;
            }
            if (App.AuthenticationManager.IsUserAuthenticated)
            {
                UserInfo info = App.AuthenticationManager.UserData;
                GivenName = info.GivenName;
                FamilyName = info.FamilyName;
                Email = info.DisplayableId;

            }
            // Initialize the SignOutWithAADCommand.
            SignOutWithAADCommand = new RelayCommand(ExecuteSignOutWithAADCommand);
            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);            
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
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteRequestServiceCommand(object param)
        {
            ReservationViewModel result = await
               ServiceClient.ServiceClient.GetReservations(App.AuthenticationManager.BearerAccessToken);

            if (result != null)
            {
                StringBuilder builder = new StringBuilder();
                if (result.ReservationsByDayActive.Count > 0)
                {
                    ReservationAvailable = true;
                    NumberPlate = result.ReservationsByDayActive[0].Reservations[0].VehiclePlateNumber;
                    ReservationDateString = result.ReservationsByDayActive[0].Day
                        .ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
                }
            }

        }
    }
}
