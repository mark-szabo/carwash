using MSHU.CarWash.UWP.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace MSHU.CarWash.UWP.Views
{
    /// <summary>
    /// A page for user settings and preferences.
    /// </summary>
    public sealed partial class SettingsPage : BasePage
    {
        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void InitializePage()
        {
            ViewModel = new SettingsViewModel()
            {
                DefaultNumberPlate = App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber
            };

            ((SettingsViewModel)ViewModel).UserSignedOut += HomePage_UserSignedOut;
            base.InitializePage();
        }

        private void HomePage_UserSignedOut(object sender, EventArgs e)
        {
            Navigate(typeof(MainPage));
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // is this the first launch ever?
            if(e.Parameter is bool && (bool)e.Parameter == true)
            {
                ((SettingsViewModel)ViewModel).FirstLaunch = true;
            }
        }

    }
}
