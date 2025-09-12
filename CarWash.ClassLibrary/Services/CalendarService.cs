using Microsoft.ApplicationInsights;
using CarWash.ClassLibrary.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class CalendarService(IOptionsMonitor<CarWashConfiguration> configuration, TelemetryClient telemetryClient, HttpClient? httpClient = null) : ICalendarService
    {
        private readonly HttpClient _client = httpClient ?? new HttpClient();

        /// <inheritdoc />
        public async Task<string?> CreateEventAsync(Reservation reservation)
        {
            var calendarEvent = GetCalendarEventFromReservation(reservation);

            try
            {
                return await CallLogicApp(calendarEvent);
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);

                return null;
            }
        }

        /// <inheritdoc />
        public async Task<string?> UpdateEventAsync(Reservation reservation)
        {
            if (reservation.OutlookEventId == null) return await CreateEventAsync(reservation);

            var calendarEvent = GetCalendarEventFromReservation(reservation);

            try
            {
                return await CallLogicApp(calendarEvent);
            }
            catch (Exception e)
            {
                telemetryClient.TrackException(e);

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
                telemetryClient.TrackException(e);
            }
        }

        /// <summary>
        /// Call the Logic App with the event in the request body
        /// </summary>
        /// <param name="calendarEvent">The event to be created/updated/deleted</param>
        /// <returns>response body</returns>
        private async Task<string> CallLogicApp(Event calendarEvent)
        {
            var content = new StringContent(JsonSerializer.Serialize(calendarEvent, Constants.DefaultJsonSerializerOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(configuration.CurrentValue.CalendarService.LogicAppUrl, content);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Convert a Reservation object to an Event object for the Logic App
        /// </summary>
        /// <param name="reservation">Reservation object to convert from</param>
        /// <returns>Converted Event object</returns>
        private Event GetCalendarEventFromReservation(Reservation reservation)
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
                    $"Please don't forget to leave the key at the reception and <a href=\"{configuration.CurrentValue.ConnectionStrings.BaseUrl}\">confirm drop-off & vehicle location by clicking here</a>!"
            };
        }
    }

    internal class Event
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("subject")]
        public string? Subject { get; set; }

        [JsonPropertyName("isCancelled")]
        public bool IsCancelled { get; set; }

        [JsonPropertyName("to")]
        public string? To { get; set; }

        [JsonPropertyName("startTime")]
        public string? StartTime { get; set; }

        [JsonPropertyName("endTime")]
        public string? EndTime { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}

