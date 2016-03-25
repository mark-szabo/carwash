using System;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public event EventHandler UserAuthenticated;

        /// <summary>
        /// Gets or sets the command that handles the app registrations.
        /// </summary>
        public RelayCommand LoginWithAADCommand { get; set; }

        /// <summary>
        /// Default constructor initializes basic business logic.
        /// </summary>
        public MainViewModel()
        {
            LoginWithAADCommand = new RelayCommand(this.ExecuteLoginWithAADCommand);
        }

        /// <summary>
        /// Event handler for the Executed event of the RegisterCommand.
        /// </summary>
        private async void ExecuteLoginWithAADCommand(object param)
        {
            bool authenticated = await App.AuthenticationManager.LoginWithAAD();
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
