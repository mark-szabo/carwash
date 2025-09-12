using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Service for managing Cloudflare cache operations.
    /// </summary>
    public class CloudflareService : ICloudflareService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CloudflareService> _logger;
        private readonly CarWashConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the CloudflareService class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making API requests.</param>
        /// <param name="logger">The logger for logging operations.</param>
        /// <param name="configuration">The CarWash configuration containing Cloudflare settings.</param>
        public CloudflareService(HttpClient httpClient, ILogger<CloudflareService> logger, IOptionsMonitor<CarWashConfiguration> configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration.CurrentValue;
        }

        /// <summary>
        /// Purges the cache for the .well-known/configuration endpoints.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task PurgeConfigurationCacheAsync()
        {
            try
            {
                var apiKey = _configuration.ConnectionStrings.CloudflareApiKey;
                var zoneId = _configuration.ConnectionStrings.CloudflareZoneId;

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(zoneId))
                {
                    _logger.LogWarning("Cloudflare API key or Zone ID not configured. Skipping cache purge.");
                    return;
                }

                var urls = new List<string>
                {
                    "https://mimosonk.hu/api/.well-known/configuration",
                    "https://www.mimosonk.hu/api/.well-known/configuration"
                };

                var purgeRequest = new
                {
                    files = urls
                };

                var requestJson = JsonSerializer.Serialize(purgeRequest);
                var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsync($"https://api.cloudflare.com/client/v4/zones/{zoneId}/purge_cache", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully purged Cloudflare cache for .well-known/configuration endpoints");
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to purge Cloudflare cache. Status: {StatusCode}, Response: {Response}", 
                        response.StatusCode, responseContent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while purging Cloudflare cache");
            }
        }
    }
}