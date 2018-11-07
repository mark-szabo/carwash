using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using MSHU.CarWash.ClassLibrary.Enums;
using MSHU.CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace MSHU.CarWash.Bot.Services
{
    public class CarwashService
    {
        private readonly HttpClient _client;

        public CarwashService(string token)
        {
            _client = new HttpClient();
            _client.BaseAddress = new Uri("https://carwashu.azurewebsites.net/");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<Reservation>> GetMyActiveReservations()
        {
            var reservations = await GetAsync<List<Reservation>>("/api/reservations");

            return reservations.Where(r => r.State < State.Done).ToList();
        }

        private async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await _client.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}
