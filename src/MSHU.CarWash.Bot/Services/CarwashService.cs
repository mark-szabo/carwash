using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
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
        public async Task<List<Reservation>> GetMyActiveReservations(CancellationToken cancellationToken = default(CancellationToken))
        {
            var reservations = await GetAsync<List<Reservation>>("/api/reservations", cancellationToken);

            return reservations.Where(r => r.State < State.Done).ToList();
        }

        /// <summary>
        /// Makes a GET request to the api endpoint specified in the parameter and returns the parsed response.
        /// </summary>
        /// <typeparam name="T">Ty the API response should be parsed to.</typeparam>
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
    }
}
