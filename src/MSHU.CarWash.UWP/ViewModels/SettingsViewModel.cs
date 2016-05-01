using MSHU.CarWash.DomainModel;
using MSHU.CarWash.UWP.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// View model for user settings
    /// </summary>
    class SettingsViewModel : BaseViewModel
    {
        Regex _validLPNumberFormat = new Regex("^[a-zA-Z]{3}-[0-9]{3}$", RegexOptions.Compiled);
        private string INVALIDLPNUMBER = "Invalid license plate number. Please use the format 'ABC-123'!";

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
            }
        }
        private string defaultNumberPlate;

        public RelayCommand SaveCommand { get; set; }

        public SettingsViewModel()
        {
            SaveCommand = new RelayCommand(
                async o => await HandleSaveCommand(), 
                (Func<object, bool>)CanExecuteSaveCommand);
            // Subscribe to the PropertyChanged event. If the DefaultNumberPlate
            // property changes we must fire the CanExecuteChanged event on the
            // SaveCommand otherwise the UWP's CommandManager doesn't
            // reevaluate if the SaveCommand can be executed.
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(DefaultNumberPlate))
                {
                    SaveCommand.RaiseCanExecuteChanged();
                }
            };
        }

        private async Task HandleSaveCommand()
        {
            var result = await ServiceClient.ServiceClient.SaveSettings(new Settings { DefaultNumberPlate = this.DefaultNumberPlate },
                App.AuthenticationManager.BearerAccessToken);

            if (result)
            {
                AppShell.Current.IsMenuEnabled = true;
                App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber = DefaultNumberPlate;
                AppShell.Current.AppFrame.Navigate(typeof(HomePage));
            }
        }

        private bool CanExecuteSaveCommand(object param)
        {
            bool result = false;
            // If DefaultNumberPlate property has a string a value and it has the correct format
            // then the value can be saved. 
            if (!string.IsNullOrEmpty(DefaultNumberPlate) && _validLPNumberFormat.IsMatch(DefaultNumberPlate))
            {
                result = true;
            }
            return result;
        }
    }
}