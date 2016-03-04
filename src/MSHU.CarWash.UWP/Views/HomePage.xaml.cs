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
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : BasePage
    {
        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override void InitializePage()
        {
            ViewModel = new HomeViewModel();
            ((HomeViewModel)ViewModel).UserSignedOut += HomePage_UserSignedOut;
            base.InitializePage();
        }

        private void HomePage_UserSignedOut(object sender, EventArgs e)
        {
            Navigate(typeof(MainPage));
        }

    }
}
