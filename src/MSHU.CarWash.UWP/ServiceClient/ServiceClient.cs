using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.MobileServices;
using MSHU.CarWash.DomainModel;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ServiceClient
{
    public class ServiceClient
    {
        private const string s_BaseUrl = "https://vadkertitestwebapp.azurewebsites.net";

        // This MobileServiceClient has been configured to communicate with the Azure Mobile App.
        // You're all set to start working with your Mobile App!
        public static MobileServiceClient MobileService = new MobileServiceClient(
            s_BaseUrl);

        public static async Task<ReservationViewModel> GetReservations(string token)
        {
            ReservationViewModel returnValue = null;
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + "/api/Calendar/GetReservations");
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                ReservationViewModel reservation =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<ReservationViewModel>(jSonResult);
                returnValue = reservation;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return returnValue;
        }

        public static async Task<bool> SaveReservation(NewReservationViewModel newReservation,
            string token)
        {
            bool result = false;

            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + "/api/Calendar/SaveReservation");
            //HttpContent content = new HttpContent();
            string reservationJSON = Newtonsoft.Json.JsonConvert.SerializeObject(newReservation);
            HttpContent contentPost = new StringContent(reservationJSON, Encoding.UTF8, "application/json");


            HttpResponseMessage httpResponse = await httpClient.PostAsync(requestURI, contentPost);
            if (httpResponse.IsSuccessStatusCode)
            {
                result = true;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }

            return result;
        }

        public static async Task<Employee> GetCurrentUser(string token)
        {
            Employee returnValue = null;
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + "/api/Employees/GetCurrentUser");
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                Employee employee =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<Employee>(jSonResult);
                returnValue = employee;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return returnValue;
        }
    }
}
