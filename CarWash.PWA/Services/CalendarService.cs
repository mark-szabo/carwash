using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using CarWash.ClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CarWash.PWA.Services
{
    /// <inheritdoc />
    public class CalendarService : ICalendarService
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly HttpClient _client;
        private readonly string _logicAppUrl;

        /// <inheritdoc />
        public CalendarService(IConfiguration configuration)
        {
            _telemetryClient = new TelemetryClient();
            _client = new HttpClient();
            _logicAppUrl = configuration.GetValue<string>("CalendarService:LogicAppUrl");
        }

        /// <inheritdoc />
        public async Task<string> CreateEventAsync(Reservation reservation)
        {
            var calendarEvent = GetCalendarEventFromReservation(reservation);

            try
            {
                return await CallLogicApp(calendarEvent);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);

                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string> UpdateEventAsync(Reservation reservation)
        {
            if (reservation.OutlookEventId == null) return await CreateEventAsync(reservation);

            var calendarEvent = GetCalendarEventFromReservation(reservation);

            try
            {
                return await CallLogicApp(calendarEvent);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);

                return null;
            }
        }

        /// <inheritdoc />
        public async Task DeleteEventAsync(Reservation reservation)
        {
            if (reservation.OutlookEventId == null) return;

            var calendarEvent = GetCalendarEventFromReservation(reservation);
            calendarEvent.IsCancelled = true;

            try
            {
                await CallLogicApp(calendarEvent);
            }
            catch (Exception e)
            {
                _telemetryClient.TrackException(e);
            }
        }

        /// <summary>
        /// Call the Logic App with the event in the request body
        /// </summary>
        /// <param name="calendarEvent">The event to be created/updated/deleted</param>
        /// <returns>response body</returns>
        private async Task<string> CallLogicApp(Event calendarEvent)
        {
            var content = new StringContent(JsonConvert.SerializeObject(calendarEvent), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(_logicAppUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Convert a Reservation object to an Event object for the Logic App
        /// </summary>
        /// <param name="reservation">Reservation object to convert from</param>
        /// <returns>Converted Event object</returns>
        private static Event GetCalendarEventFromReservation(Reservation reservation)
        {
            if (reservation.User == null) throw new Exception("User is not loaded for reservation!");

            return new Event
            {
                Id = reservation.OutlookEventId,
                To = reservation.User.Email,
                Subject = $"🚗 Car wash ({reservation.VehiclePlateNumber})",
                StartTime = reservation.StartDate.ToString(),
                EndTime = reservation.EndDate.ToString(),
                Location = reservation.Location,
                Body =
                    "Please don't forget to leave the key at the reception and <a href=\"https://carwashu.azurewebsites.net\">confirm drop-off & vehicle location by clicking here</a>!"
            };
        }
    }

    internal class Event
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("isCancelled")]
        public bool IsCancelled { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }
    }
}

