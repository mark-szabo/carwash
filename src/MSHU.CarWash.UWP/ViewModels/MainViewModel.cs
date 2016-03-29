using System;
using Windows.ApplicationModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public event EventHandler UserAuthenticated;

        /// <summary>
        /// Gets or sets the command that handles the app registrations.
        /// </summary>
        public RelayCommand LoginWithAADCommand { get; set; }

        public bool ShowSignInUI
        {
            // show UI only if Internet is avail.
            get { return showSignInUI && InternetAvailable && !SignInInProgress; }
            set
            {
                showSignInUI = value;
                OnPropertyChanged(nameof(ShowSignInUI));
            }
        }
        private bool showSignInUI;

        public bool InternetAvailable
        {
            get { return internetAvailable; }
            set
            {
                internetAvailable = value;
                OnPropertyChanged(nameof(InternetAvailable));
                OnPropertyChanged(nameof(ShowSignInUI));
            }
        }
        private bool internetAvailable;

        private bool signInInProgress;

        public bool SignInInProgress
        {
            get { return signInInProgress; }
            set
            {
                signInInProgress = value;
                OnPropertyChanged(nameof(SignInInProgress));
                OnPropertyChanged(nameof(ShowSignInUI));
            }
        }

        /// <summary>
        /// Default constructor initializes basic business logic.
        /// </summary>
        public MainViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                ShowSignInUI = true;
                InternetAvailable = true;
                return;
            }
            ShowSignInUI = true;
            LoginWithAADCommand = new RelayCommand(this.ExecuteLoginWithAADCommand);
        }

        /// <summary>
        /// Event handler for the Executed event of the RegisterCommand.
        /// </summary>
        private async void ExecuteLoginWithAADCommand(object param)
        {
            SignInInProgress = true;
            bool authenticated = await App.AuthenticationManager.LoginWithAAD();
            SignInInProgress = false;
            if (authenticated)
            {
                if (UserAuthenticated != null)
                {
                    UserAuthenticated(this, new EventArgs());
                }
            }
        }

    }

}
