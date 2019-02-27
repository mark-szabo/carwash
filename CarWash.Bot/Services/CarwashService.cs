using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CarWash.Bot.Dialogs;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Extensions;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using Constants = CarWash.ClassLibrary.Constants;

namespace CarWash.Bot.Services
{
    /// <summary>
    /// CarWash Service for accessing the CarWash API.
    /// </summary>
    public class CarwashService
    {
        private readonly HttpClient _client;
        private readonly TelemetryClient _telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CarwashService"/> class.
        /// CarWash Service for accessing the CarWash API.
        /// </summary>
        /// <param name="token">Authentication token.</param>
        internal CarwashService(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new AuthenticationException("Not authenticated.");

            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://carwashu.azurewebsites.net/");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _telemetryClient = new TelemetryClient();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CarwashService"/> class.
        /// CarWash Service for accessing the CarWash API.
        /// </summary>
        /// <param name="dc">Dialog context.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        internal CarwashService(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = AuthDialog.GetToken(dc, cancellationToken).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(token)) throw new AuthenticationException("Not authenticated.");

            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://carwashu.azurewebsites.net/");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            _telemetryClient = new TelemetryClient();
        }

        /// <summary>
        /// Get the signed in user's object.
        /// </summary>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>The currently signed in user.</returns>
        internal async Task<User> GetMe(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<User>("/api/users/me", cancellationToken);
        }

        /// <summary>
        /// Get the user's active (not done) reservations.
        /// </summary>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>List of active reservations.</returns>
        internal async Task<List<Reservation>> GetMyActiveReservationsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var reservations = await GetAsync<List<Reservation>>("/api/reservations", cancellationToken);

            return reservations
                .Where(r => r.State < State.Done)
                .OrderBy(r => r.StartDate)
                .ToList();
        }

        /// <summary>
        /// Get reservation by id.
        /// </summary>
        /// <param name="id">Reservation id.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Reservation object.</returns>
        internal async Task<Reservation> GetReservationAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<Reservation>($"/api/reservations/{id}", cancellationToken);
        }

        /// <summary>
        /// Add a new reservation.
        /// </summary>
        /// <param name="reservation"><see cref="Reservation"/>.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>The newly created <see cref="Reservation"/>.</returns>
        internal async Task<Reservation> SubmitReservationAsync(Reservation reservation, CancellationToken cancellationToken = default(CancellationToken))
        {
            reservation.StartDate = reservation.StartDate.ToUniversalTime();
            var jsonContent = JsonConvert.SerializeObject(
                reservation,
                new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            return await PostAsync<Reservation>($"/api/reservations", content, cancellationToken);
        }

        /// <summary>
        /// Cancels a reservation by id.
        /// </summary>
        /// <param name="id">Reservation id.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        internal async Task CancelReservationAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DeleteAsync<object>($"/api/reservations/{id}", cancellationToken);
        }

        /// <summary>
        /// Confirm key drop-off and location for a reservation.
        /// </summary>
        /// <param name="id">Reservation id.</param>
        /// <param name="location">Reservation location.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        internal async Task ConfirmDropoffAsync(string id, string location, CancellationToken cancellationToken = default(CancellationToken))
        {
            var content = new StringContent(JsonConvert.SerializeObject(location), Encoding.UTF8, "application/json");
            await PostAsync<object>($"/api/reservations/{id}/confirmdropoff", content, cancellationToken);
        }

        /// <summary>
        /// Get some settings from the last reservation made by the user to be used as defaults for a new reservation.
        /// </summary>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>LastSettings object.</returns>
        internal async Task<LastSettings> GetLastSettingsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<LastSettings>("/api/reservations/lastsettings", cancellationToken);
        }

        /// <summary>
        /// Get the list of future dates that are not available.
        /// </summary>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>NotAvailableDatesAndTimes object.</returns>
        internal async Task<NotAvailableDatesAndTimes> GetNotAvailableDatesAndTimesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<NotAvailableDatesAndTimes>("/api/reservations/notavailabledates", cancellationToken);
        }

        /// <summary>
        /// Get the list of next available, free slots.
        /// </summary>
        /// <param name="numberOfSlots">Number of next free slot to return. Defaults to 3.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A list of free slots.</returns>
        internal async Task<IEnumerable<string>> GetNextFreeSlotsAsync(int numberOfSlots = 3, CancellationToken cancellationToken = default(CancellationToken))
        {
            var notAvailable = await GetAsync<NotAvailableDatesAndTimes>("/api/reservations/notavailabledates", cancellationToken);

            var freeslots = new List<string>();
            var dateIterator = DateTime.Today;
            while (freeslots.Count < numberOfSlots && dateIterator < DateTime.Today.AddYears(1))
            {
                foreach (var slot in Constants.Slots)
                {
                    var slotStartTime = new DateTime(dateIterator.Year, dateIterator.Month, dateIterator.Day, slot.StartTime, 0, 0);
                    var slotEndTime = new DateTime(dateIterator.Year, dateIterator.Month, dateIterator.Day, slot.EndTime, 0, 0);

                    if (!notAvailable.Times.Contains(slotStartTime) && freeslots.Count <= numberOfSlots && slotStartTime >= DateTime.Now)
                    {
                        var timex = TimexProperty.FromDateTime(slotStartTime);
                        freeslots.Add($"{timex.ToNaturalLanguage(DateTime.Now)}-{slotEndTime.ToString("htt")}");
                    }
                }

                dateIterator = dateIterator.AddDays(1);
            }

            return freeslots;
        }

        /// <summary>
        /// Gets a list of slots and their free reservation capacity on a given date.
        /// </summary>
        /// <param name="date">The date to filter on.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>List of <see cref="ReservationCapacity"/>.</returns>
        internal async Task<IEnumerable<ReservationCapacity>> GetReservationCapacityAsync(DateTime date, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<IEnumerable<ReservationCapacity>>($"/api/reservations/reservationcapacity?date={date.ToString("s")}", cancellationToken);
        }

        /// <summary>
        /// Makes a GET request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Type the API response should be parsed to.</typeparam>
        /// <param name="endpoint">API endpoint (eg. "/api/reservations").</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Parsed response.</returns>
        private Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeApiRequestAsync<T>(() => _client.GetAsync(endpoint, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Makes a POST request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Type the API response should be parsed to.</typeparam>
        /// <param name="endpoint">API endpoint (eg. "/api/reservations").</param>
        /// <param name="content">HTTP request content.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Parsed response.</returns>
        private Task<T> PostAsync<T>(string endpoint, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeApiRequestAsync<T>(() => _client.PostAsync(endpoint, content, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Makes a DELETE request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Type the API response should be parsed to.</typeparam>
        /// <param name="endpoint">API endpoint (eg. "/api/reservations").</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Parsed response.</returns>
        private Task<T> DeleteAsync<T>(string endpoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            return MakeApiRequestAsync<T>(() => _client.DeleteAsync(endpoint, cancellationToken));
        }

        /// <summary>
        /// Makes a request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Type the API response should be parsed to.</typeparam>
        /// <param name="func">One of <see cref="HttpClient"/>'s HTTP request func. (GetAsync, PostAsync, etc).</param>
        /// <returns>Parsed response.</returns>
        private async Task<T> MakeApiRequestAsync<T>(Func<Task<HttpResponseMessage>> func)
        {
            try
            {
                var response = await func();
                var responseContent = response.Content is HttpContent c ? await c.ReadAsStringAsync() : null;

                switch (response.StatusCode)
                {
                    case System.Net.HttpStatusCode.BadRequest:
                        throw new CarwashApiException(responseContent?.Trim('"'), response);
                    case System.Net.HttpStatusCode.Forbidden:
                        throw new CarwashApiException("Sorry, but you don't have permission to do this.", response);
                    case System.Net.HttpStatusCode.InternalServerError:
                        throw new CarwashApiException("I'm not able to access the CarWash app right now.", response);
                    case System.Net.HttpStatusCode.NotFound:
                        throw new CarwashApiException("Sorry, I haven't found this one.", response);
                    case System.Net.HttpStatusCode.Unauthorized:
                        throw new CarwashApiException("You are not authenticated. Log in by typing 'login'.", response);
                }

                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<T>(responseContent);
            }
            catch (CarwashApiException e)
            {
                _telemetryClient.TrackException(
                    e,
                    new Dictionary<string, string>
                    {
                            { "Error message", e.Message },
                            { "Response body", e.Response?.Content is HttpContent responseContent ? await responseContent.ReadAsStringAsync() : null },
                            { "Request body", e.Response?.RequestMessage?.Content is HttpContent requestContent ? await requestContent.ReadAsStringAsync() : null },
                            { "Request uri", $"{e.Response?.RequestMessage?.Method?.ToString()} {e.Response?.RequestMessage?.RequestUri?.AbsoluteUri}" },
                            { "Request headers", e.Response?.RequestMessage?.Headers?.ToJson() },
                    });

                throw e;
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);

                throw new Exception($"I'm not able to access the CarWash app right now.", e);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        /*
         * API response models
         */

        public class NotAvailableDatesAndTimes
        {
            public IEnumerable<DateTime> Dates { get; set; }

            public IEnumerable<DateTime> Times { get; set; }
        }

        public class LastSettings
        {
            public string VehiclePlateNumber { get; set; }

            public string Location { get; set; }

            public List<ServiceType> Services { get; set; }
        }

        public class ReservationCapacity
        {
            public DateTime StartTime { get; set; }

            public int FreeCapacity { get; set; }
        }

        [Serializable]
        private class CarwashApiException : Exception
        {
            public CarwashApiException(string message, HttpResponseMessage response) : base(message)
            {
                Response = response;
            }

            public CarwashApiException(string message, HttpResponseMessage response, Exception innerException) : base(message, innerException)
            {
                Response = response;
            }

            protected CarwashApiException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }

            public HttpResponseMessage Response { get; set; }
        }
    }
}
