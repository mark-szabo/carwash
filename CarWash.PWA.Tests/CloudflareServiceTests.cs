using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class CloudflareServiceTests
    {
        [Fact]
        public async Task PurgeConfigurationCacheAsync_WithValidConfiguration_SendsCorrectRequest()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            var loggerMock = new Mock<ILogger<CloudflareService>>();
            
            var configurationMock = new Mock<IOptionsMonitor<CarWashConfiguration>>();
            configurationMock.Setup(x => x.CurrentValue).Returns(new CarWashConfiguration
            {
                ConnectionStrings = new CarWashConfiguration.ConnectionStringsConfiguration
                {
                    CloudflareApiKey = "test-api-key",
                    CloudflareZoneId = "test-zone-id"
                }
            });

            var service = new CloudflareService(httpClient, configurationMock.Object, loggerMock.Object, null);

            // Act
            await service.PurgeConfigurationCacheAsync();

            // Assert
            httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri == new Uri("https://api.cloudflare.com/client/v4/zones/test-zone-id/purge_cache") &&
                    req.Headers.Authorization.Scheme == "Bearer" &&
                    req.Headers.Authorization.Parameter == "test-api-key"),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task PurgeConfigurationCacheAsync_WithMissingApiKey_LogsWarningAndReturns()
        {
            // Arrange
            var httpClient = new HttpClient();
            var loggerMock = new Mock<ILogger<CloudflareService>>();
            
            var configurationMock = new Mock<IOptionsMonitor<CarWashConfiguration>>();
            configurationMock.Setup(x => x.CurrentValue).Returns(new CarWashConfiguration
            {
                ConnectionStrings = new CarWashConfiguration.ConnectionStringsConfiguration
                {
                    CloudflareApiKey = null, // Missing API key
                    CloudflareZoneId = "test-zone-id"
                }
            });

            var service = new CloudflareService(httpClient, configurationMock.Object, loggerMock.Object, null);

            // Act
            await service.PurgeConfigurationCacheAsync();

            // Assert
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Cloudflare API key or Zone ID not configured")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}