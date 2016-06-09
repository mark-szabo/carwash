using MSHU.CarWash.DomainModel;
using MSHU.CarWash.UWP.Common;
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
        private const string s_BaseUrl = AuthenticationManager.s_BackendAddress;

        /// <summary>
        /// Get all reservations
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<ReservationViewModel> GetReservations(string token)
        {
            return await GetRestApiCall<ReservationViewModel>(token, "/api/Calendar/GetReservations");
        }

        /// <summary>
        /// Save Reservation
        /// </summary>
        /// <param name="newReservation">Details of reservation</param>
        /// <param name="token">Auth token</param>
        /// <returns>ID of the new reservation</returns>
        public static async Task<int?> SaveReservation(NewReservationViewModel newReservation, string token)
        {
            // we need the ID returned
            var result = new int?();
            var success = await PostRestApi<NewReservationViewModel>(newReservation, token, "/api/Calendar/SaveReservation", 
                o => result = new int?(Convert.ToInt32(o)));    // result comes back as long due to JSonConvert...

            return result;
        }

        /// <summary>
        /// Get current user
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<Employee> GetCurrentUser(string token)
        {
            return await GetRestApiCall<Employee>(token, "/api/Employees/GetCurrentUser"); 
        }

        /// <summary>
        /// Retrieves date of next free slot.
        /// </summary>
        /// <param name="token">Token used for authentication</param>
        /// <returns>Date of next free slot</returns>
        public static async Task<DateTime?> GetNextFreeSlotDate(string token)
        {
            return await GetRestApiCall<DateTime?>(token, "/api/Calendar/GetNextFreeSlotDate");
        }

        /// <summary>
        /// Deletes a reservation.
        /// </summary>
        /// <param name="reservationId">ID of the reservation</param>
        /// <param name="token">Token used for authentication</param>
        /// <returns>True if removal has succeeded, false otherwise</returns>
        public static async Task<bool> DeleteReservation(int reservationId, string token)
        {
            //Delete returns the removed Reservation unfortunately
            Reservation reservation = await GetRestApiCall<Reservation>(token, "/api/Calendar/DeleteReservation?reservationId=" + reservationId);
            return (reservation!=null);
        }

        /// <summary>
        /// Retrieves the number of available slots for the given day.
        /// </summary>
        /// <param name="day">The day that the number of available slots is calculated for.</param>
        /// <param name="token">Token used for authentication</param>
        /// <returns>The number of available slots.</returns>
        public static async Task<int?> GetCapacityByDay(DateTime day, string token)
        {
            return await GetRestApiCall<int?>(token, $"/api/Calendar/CapacityByDay?day={day.ToString("D", CultureInfo.InvariantCulture)}");
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
            string url = String.Format("/api/Calendar/CapacityForTimeInterval?startDate={0}&endDate={1}",
               startDay.ToString("D", CultureInfo.InvariantCulture),
               endDay.ToString("D", CultureInfo.InvariantCulture));

            return await GetRestApiCall<List<int>>(token, url);
        }

        /// <summary>
        /// Saves settings for the current user.
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>True if succeeded</returns>
        public static async Task<bool> SaveSettings(Settings setting, string token)
        {
            return await PostRestApi<Settings>(setting, token, "/api/Employees/SaveSettings");
        }
        
        /// <summary>
        /// </summary>
        /// <param name="token">Token used for authentication</param>
        /// <returns>
        /// </returns>
        public static async Task<bool?> NewReservationAvailable(string token)
        {
            return await GetRestApiCall<bool?>(token, "/api/Calendar/NewReservationAvailable");
        }

        /// <summary>
        /// Save Reservation
        /// </summary>
        /// <param name="newReservation"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateReservation(Reservation reservation, string token)
        {
            return await PostRestApi<Reservation>(reservation, token, "/api/Calendar/UpdateReservation");
        }

        /// <summary>
        /// General Get REST Api call to Service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <param name="relativeApiUrl"></param>
        /// <returns></returns>
        private static async Task<T> GetRestApiCall<T>(string token, string relativeApiUrl)
        {
            T returnValue = default(T);
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Call the Web API to get the values
            Uri requestURI = new Uri(s_BaseUrl + relativeApiUrl);
            HttpResponseMessage httpResponse = await httpClient.GetAsync(requestURI);
            if (httpResponse.IsSuccessStatusCode)
            {
                string jSonResult = await httpResponse.Content.ReadAsStringAsync();

                T resultObject =
                    Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jSonResult);
                returnValue = resultObject;
            }
            else
            {
                //if unauthorized, then try to login again
                if (httpResponse.StatusCode.ToString().ToLower().Contains("unauthorized"))
                {
                    bool authorized = await App.AuthenticationManager.TryAutoSignInWithAadAsync();
                    //if login is successful
                    if (authorized == true)
                    {
                        //call again the original method
                        return await GetRestApiCall<T>(token, relativeApiUrl);
                    }
                }
                else
                {
                    var message = string.Format("{0}", httpResponse.StatusCode.ToString());
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(message);
                    Diagnostics.ReportError(message);
                    await dialog.ShowAsync();
                }
            }

            return returnValue;
        }

        /// <summary>
        /// General Post REST Api call to service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="postObject"></param>
        /// <param name="token"></param>
        /// <param name="relativeApiUrl"></param>
        /// <param name="saveResponse">Delegate that can save the response if needed</param>
        /// <returns></returns>
        private static async Task<bool> PostRestApi<T>(T postObject, string token, string relativeApiUrl, Action<object> saveResponse = null)
        {
            // Create an HTTP client and add the token to the Authorization header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            Uri requestURI = new Uri(s_BaseUrl + relativeApiUrl);
            string reservationJSON = Newtonsoft.Json.JsonConvert.SerializeObject(postObject);

            HttpContent content = new StringContent(reservationJSON, Encoding.UTF8, "application/json");
            HttpResponseMessage httpResponse = await httpClient.PostAsync(requestURI, content);
            if (httpResponse.IsSuccessStatusCode)
            {
                // save the response if requested
                saveResponse?.Invoke(Newtonsoft.Json.JsonConvert.DeserializeObject(await httpResponse.Content.ReadAsStringAsync()));
                return true;
            }
            else
            {
                //if unauthorized, then try to login again
                if (httpResponse.StatusCode.ToString().ToLower().Contains("unauthorized"))
                {
                    bool authorized = await App.AuthenticationManager.TryAutoSignInWithAadAsync();
                    //if login is successful
                    if (authorized == true)
                    {
                        //call again the original method
                        return await PostRestApi<T>(postObject, token, relativeApiUrl, saveResponse);
                    }
                }
                else
                {
                    var message = string.Format("{0}\n{1}", httpResponse.StatusCode.ToString(),
                    await httpResponse.Content.ReadAsStringAsync());
                    Diagnostics.ReportError(message);
                    Windows.UI.Popups.MessageDialog dialog = new Windows.UI.Popups.MessageDialog(message);
                    await dialog.ShowAsync();
                }
            }
            return false;
        }
    }
}
