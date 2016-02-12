using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class HomeViewModel : Bindable
    {
        private string _givenName;
        private string _familyName;
        private string _displayableID;

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
        /// Gets or sets the SignOutWithAADCommand.
        /// </summary>
        public RelayCommand SignOutWithAADCommand { get; set; }

        /// <summary>
        /// Gets or sets the SignOutWithAADCommand.
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
        /// Event handler for the Executed event of the SignOutWithAADCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteRequestServiceCommand(object param)
        {
            //Newtonsoft.Json.Linq.JObject payload = new Newtonsoft.Json.Linq.JObject();
            //payload["access_token"] = App.AuthenticationManager.BearerAccessToken;
            //Microsoft.WindowsAzure.MobileServices.MobileServiceUser user = 
            //    await ServiceClient.ServiceClient.MobileService.LoginAsync(
            //        Microsoft.WindowsAzure.MobileServices.MobileServiceAuthenticationProvider.WindowsAzureActiveDirectory,payload);
            bool success = await 
                App.AuthenticationManager.ReadValues(App.AuthenticationManager.BearerAccessToken);

        }
    }
}
