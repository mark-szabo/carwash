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

    }
}
