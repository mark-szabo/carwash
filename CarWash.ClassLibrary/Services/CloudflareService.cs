using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services;

/// <summary>
/// Service for managing Cloudflare cache operations.
/// </summary>
/// <param name="httpClient">The HTTP client for making API requests.</param>
/// <param name="configuration">The CarWash configuration containing Cloudflare settings.</param>
/// <param name="logger">The logger for logging operations.</param>
/// <param name="telemetryClient">The telemetry client.</param>
public class CloudflareService(HttpClient httpClient, IOptionsMonitor<CarWashConfiguration> configuration, ILogger<CloudflareService> logger, TelemetryClient? telemetryClient) : ICloudflareService
{
    private readonly CarWashConfiguration _configuration = configuration.CurrentValue;

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
                logger.LogWarning("Cloudflare API key or Zone ID not configured. Skipping cache purge.");
                return;
            }

            var purgeRequest = new
            {
                files = new List<string>
                {
                    "https://mimosonk.hu/api/.well-known/configuration",
                    "https://www.mimosonk.hu/api/.well-known/configuration"
                }
            };

            var requestJson = JsonSerializer.Serialize(purgeRequest);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await httpClient.PostAsync($"https://api.cloudflare.com/client/v4/zones/{zoneId}/purge_cache", content);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully purged Cloudflare cache for .well-known/configuration endpoints");
                telemetryClient?.TrackEvent("CloudflareCachePurged", new Dictionary<string, string>
                {
                    { "Path", "/api/.well-known/configuration" }
                });
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                logger.LogError("Failed to purge Cloudflare cache. Status: {StatusCode}, Response: {Response}", response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while purging Cloudflare cache");
            telemetryClient?.TrackException(ex);
        }
    }
}