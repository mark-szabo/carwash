using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MSHU.CarWash.DomainModel;
using System;
using System.Text;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class HomeViewModel : Bindable
    {
        private string _givenName;
        private string _familyName;
        private string _displayableID;
        private string m_RegistrationInfo;

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
        /// Default constructor initializes instance state.
        /// </summary>
        public HomeViewModel()
        {
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
                foreach(ReservationDayDetailsViewModel s in result.ReservationsByDayActive)
                {
                    if (s.Reservations != null && s.Reservations.Count > 0)
                    {
                        string plateNumber = s.Reservations[0].VehiclePlateNumber;
                        builder.AppendFormat("Reservation for {0} on {1} {2}, {3}.", plateNumber, s.MonthName, s.DayNumber, s.DayName);
                    }
                }
                string generated = builder.ToString();
                //if there is no reservation
                if (String.IsNullOrEmpty(generated) == true)
                {
                    this.RegistrationInfo = "There is currently no reservation for car wash";
                }
                else
                {
                    this.RegistrationInfo = builder.ToString();
                }
            }

        }
    }
}
