using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class MainViewModel : Bindable
    {
        /// <summary>
        /// Gets or sets the command that handles the app registrations.
        /// </summary>
        public RelayCommand RegisterCommand { get; set; }

        /// <summary>
        /// Default constructor initializes basic business logic.
        /// </summary>
        public MainViewModel()
        {
            RegisterCommand = new RelayCommand(this.ExecuteRegisterCommand);
        }

        /// <summary>
        /// Event handler for the Executed event of the RegisterCommand.
        /// </summary>
        private void ExecuteRegisterCommand(object param)
        {

        }

    }
}
