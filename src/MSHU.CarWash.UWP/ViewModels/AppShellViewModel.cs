using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    class AppShellViewModel : BaseViewModel
    {
        public bool InternetAvailable
        {
            get { return internetAvailable; }
            set
            {
                internetAvailable = value;
                OnPropertyChanged(nameof(InternetAvailable));
            }
        }
        private bool internetAvailable;

        public AppShellViewModel()
        {
            if(DesignMode.DesignModeEnabled)
            {
                InternetAvailable = false;
            }
        }
    }
}
