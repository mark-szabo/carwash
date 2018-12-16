using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using MSHU.CarWash.Bot.Dialogs.Auth;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Services
{
    public class CarwashService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="CarwashService"/> class.
        /// CarWash Service for accessing the CarWash API.
        /// </summary>
        /// <param name="token">Authentication token.</param>
        public CarwashService(string token)
        {
            if (string.IsNullOrEmpty(token)) throw new AuthenticationException("Not authenticated.");

            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://carwashu.azurewebsites.net/");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CarwashService"/> class.
        /// CarWash Service for accessing the CarWash API.
        /// </summary>
        /// <param name="dc">Dialog context.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        public CarwashService(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var token = AuthDialog.GetToken(dc, cancellationToken).GetAwaiter().GetResult();
            if (string.IsNullOrEmpty(token)) throw new AuthenticationException("Not authenticated.");

            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://carwashu.azurewebsites.net/");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        /// <summary>
        /// Get the user's active (not done) reservations.
        /// </summary>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>List of active reservations.</returns>
        public async Task<List<Reservation>> GetMyActiveReservationsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var reservations = await GetAsync<List<Reservation>>("/api/reservations", cancellationToken);

            return reservations.Where(r => r.State < State.Done).ToList();
        }

        /// <summary>
        /// Get reservation by id.
        /// </summary>
        /// <param name="id">Reservation id.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Reservation object.</returns>
        public async Task<Reservation> GetReservationAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await GetAsync<Reservation>($"/api/reservations/{id}", cancellationToken);
        }

        /// <summary>
        /// Confirm key drop-off and location for a reservation.
        /// </summary>
        /// <param name="id">Reservation id.</param>
        /// <param name="location">Reservation location.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Void.</returns>
        public async Task ConfirmDropoffAsync(string id, string location, CancellationToken cancellationToken = default(CancellationToken))
        {
            var content = new StringContent(JsonConvert.SerializeObject(location), Encoding.UTF8, "application/json");
            await PostAsync<object>($"/api/reservations/{id}/confirmdropoff", content, cancellationToken);
        }

        /// <summary>
        /// Makes a GET request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Type the API response should be parsed to.</typeparam>
        /// <param name="endpoint">API endpoint (eg. "/api/reservations").</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>Parsed response.</returns>
        private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _client.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(content);
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
        private async Task<T> PostAsync<T>(string endpoint, HttpContent content, CancellationToken cancellationToken = default(CancellationToken))
        {
            var response = await _client.PostAsync(endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(responseContent);
        }
    }
}
