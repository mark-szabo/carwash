using Microsoft.WindowsAzure.MobileServices;

namespace MSHU.CarWash.UWP.ServiceClient
{
    public class ServiceClient
    {
        // This MobileServiceClient has been configured to communicate with the Azure Mobile App.
        // You're all set to start working with your Mobile App!
        public static MobileServiceClient MobileService = new MobileServiceClient(
            "https://vadkertitestmobile.azurewebsites.net"
            );
    }
}
