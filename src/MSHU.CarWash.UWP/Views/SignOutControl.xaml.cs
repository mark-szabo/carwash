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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace MSHU.CarWash.UWP.Views
{
    public sealed partial class SignOutControl : UserControl
    {

        public UserViewModel ViewModel { get; set; }


        public SignOutControl()
        {
            ViewModel = new UserViewModel();
            ViewModel.UserSignedOut += ViewModel_UserSignedOut;
            this.InitializeComponent();
        }

        private void ViewModel_UserSignedOut(object sender, EventArgs e)
        {
            Window.Current.Content = new MainPage();
            Window.Current.Activate();
        }
    }
}
