using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class UserViewModel
    {
        public string Username
        {
            get
            {
                var givenName = "guest";
                givenName = App.AuthenticationManager?.UserData?.GivenName;
                return $"Signed in as {givenName}.";
            }
        }

        public RelayCommand SignOutUser { get;}
        public event EventHandler UserSignedOut;

        public UserViewModel()
        {
            SignOutUser = new RelayCommand(async o =>
            {
                var succeeded = await App.AuthenticationManager.SignOutWithAAD();
                if(succeeded && UserSignedOut != null)
                {
                    UserSignedOut(this, new EventArgs());
                }
            });
        }
    }
}
