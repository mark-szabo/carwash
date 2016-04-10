using MSHU.CarWash.DomainModel;
using MSHU.CarWash.UWP.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// View model for user settings
    /// </summary>
    class SettingsViewModel : BaseViewModel
    {

        /// <summary>
        /// Is app launched first time (new user)?
        /// </summary>
        public bool FirstLaunch
        {
            get { return firstUse; }
            set
            {
                firstUse = value;
                OnPropertyChanged(nameof(FirstLaunch));
            }
        }
        private bool firstUse;

        /// <summary>
        /// Default number plate
        /// </summary>
        public string DefaultNumberPlate
        {
            get { return defaultNumberPlate; }
            set
            {
                defaultNumberPlate = value;
                OnPropertyChanged(nameof(DefaultNumberPlate));
                SaveCommand.RaiseCanExecuteChanged();
            }
        }
        private string defaultNumberPlate;

        public RelayCommand SaveCommand { get; set; }

        public SettingsViewModel()
        {
            SaveCommand = new RelayCommand(async o => await HandleSaveCommand(), () => !String.IsNullOrWhiteSpace(DefaultNumberPlate));
        }

        private async Task HandleSaveCommand()
        {
            var result = await ServiceClient.ServiceClient.SaveSettings(new Settings { DefaultNumberPlate = this.DefaultNumberPlate },
                App.AuthenticationManager.BearerAccessToken);

            if(result)
            {
                AppShell.Current.IsMenuEnabled = true;
                App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber = DefaultNumberPlate;
                AppShell.Current.AppFrame.Navigate(typeof(HomePage));
            }
        }
    }
}