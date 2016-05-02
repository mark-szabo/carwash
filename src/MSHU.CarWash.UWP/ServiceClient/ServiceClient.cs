using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.MobileServices;
using MSHU.CarWash.DomainModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ServiceClient
{
    // TODO: refactor this class to remove duplicates
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
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}\n{1}", httpResponse.StatusCode.ToString(),
                       await httpResponse.Content.ReadAsStringAsync()));
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

        /// <summary>
        /// Retrieves date of next free slot.
        /// </summary>
        /// <param name="token">Token used for authentication</param>
        /// <returns>Date of next free slot</returns>
        public static async Task<DateTime?> GetNextFreeSlotDate(string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + "/api/Calendar/GetNextFreeSlotDate");
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                var date =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime?>(jSonResult);

                return date;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return null;
        }

        /// <summary>
        /// Deletes a reservation.
        /// </summary>
        /// <param name="reservationId">ID of the reservation</param>
        /// <param name="token">Token used for authentication</param>
        /// <returns>True if removal has succeeded, false otherwise</returns>
        public static async Task<bool> DeleteReservation(int reservationId, string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + "/api/Calendar/DeleteReservation?reservationId=" + reservationId);
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                    new Windows.UI.Popups.MessageDialog(string.Format("{0}\n{1}", httpResponse.StatusCode.ToString(),
                    await httpResponse.Content.ReadAsStringAsync()));

                await dialog.ShowAsync();
            }
            return false;
        }

        /// <summary>
        /// Retrieves the number of available slots for the given day.
        /// </summary>
        /// <param name="day">The day that the number of available slots is calculated for.</param>
        /// <param name="token">Token used for authentication</param>
        /// <returns>The number of available slots.</returns>
        public static async Task<int?> GetCapacityByDay(DateTime day, string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + $"/api/Calendar/CapacityByDay?day={day.ToString("D", CultureInfo.InvariantCulture)}");
            string reservationJSON = Newtonsoft.Json.JsonConvert.SerializeObject(day);
            //HttpContent contentPost = new StringContent(reservationJSON, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                //var availableSlots =
                //    Newtonsoft.Json.JsonConvert.DeserializeObject<int?>(jSonResult);

                int availableSlots = 0;
                int.TryParse(jSonResult, out availableSlots);

                return availableSlots;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return null;
        }

        /// <summary>
        /// Retrieves the number of available slots for every day between the specified
        /// start and end date.
        /// </summary>
        /// <param name="startDay">The start date of the desired time interval.</param>
        /// <param name="endDay">The end date of the desired time interval.</param>
        /// <param name="token">Token used for authentication</param>
        /// <returns>
        /// The number of available slots corresponding to the day index in the time interval.
        /// </returns>
        public static async Task<List<int>> GetCapacityForTimeInterval(DateTime startDay, DateTime endDay, 
            string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + 
                String.Format("/api/Calendar/CapacityForTimeInterval?startDate={0}&endDate={1}",
                startDay.ToString("D", CultureInfo.InvariantCulture),
                endDay.ToString("D", CultureInfo.InvariantCulture)));
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                var availableSlots =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>> (jSonResult);

                return availableSlots;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return null;
        }

        /// <summary>
        /// Saves settings for the current user.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>True if succeeded</returns>
        public static async Task<bool> SaveSettings(Settings setting, string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            Uri requestURI = new Uri(s_BaseUrl + "/api/Employees/SaveSettings");
            string reservationJSON = Newtonsoft.Json.JsonConvert.SerializeObject(setting);

            HttpContent content = new StringContent(reservationJSON, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PostAsync(requestURI, content);
            if (httpResponse.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                    new Windows.UI.Popups.MessageDialog(string.Format("{0}\n{1}", httpResponse.StatusCode.ToString(),
                    await httpResponse.Content.ReadAsStringAsync()));

                await dialog.ShowAsync();
            }
            return false;

        }

        /// <summary>
        /// </summary>
        /// <param name="token">Token used for authentication</param>
        /// <returns>
        /// </returns>
        public static async Task<bool?> NewReservationAvailable(string token)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl +
                String.Format("/api/Calendar/NewReservationAvailable"));
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                var availableSlots =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<bool>(jSonResult);

                return availableSlots;
            }
            else
            {
                Windows.UI.Popups.MessageDialog dialog =
                       new Windows.UI.Popups.MessageDialog(string.Format("{0}", httpResponse.StatusCode.ToString()));
                await dialog.ShowAsync();
            }
            return false;
        }

    }
}
