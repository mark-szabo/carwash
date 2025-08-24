using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class CalendarServiceTests
    {
        [Fact]
        public async Task CreateEventAsync_ByDefault_MakesHttpRequest()
        {
            // ARRANGE
            var configurationStub = CreateConfigurationStub();
            const string OUTLOOK_EVENT_ID = "thisisanoutlookeventid";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpMessageHandlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted,
                   Content = new StringContent(OUTLOOK_EVENT_ID),
               })
               .Verifiable();

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://test.com/"),
            };

            var calendarService = new CalendarService(configurationStub, CreateTelemetryClientStub(),  httpClient);

            var reservationStub = CreateDefaultReservation();

            // ACT
            var outlookEventId = await calendarService.CreateEventAsync(reservationStub);

            // ASSERT
            Assert.Equal(OUTLOOK_EVENT_ID, outlookEventId);

            var expectedUri = new Uri(configurationStub.CurrentValue.CalendarService.LogicAppUrl);

            httpMessageHandlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Post
                  && req.RequestUri == expectedUri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task UpdateEventAsync_ByDefault_MakesHttpRequest()
        {
            // ARRANGE
            var configurationStub = CreateConfigurationStub();
            const string OUTLOOK_EVENT_ID = "thisisanoutlookeventid";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpMessageHandlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted,
                   Content = new StringContent(OUTLOOK_EVENT_ID),
               })
               .Verifiable();

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://test.com/"),
            };

            var calendarService = new CalendarService(configurationStub, CreateTelemetryClientStub(), httpClient);

            var reservationStub = CreateDefaultReservation();
            reservationStub.OutlookEventId = OUTLOOK_EVENT_ID;

            // ACT
            var outlookEventId = await calendarService.UpdateEventAsync(reservationStub);

            // ASSERT
            Assert.Equal(OUTLOOK_EVENT_ID, outlookEventId);

            var expectedUri = new Uri(configurationStub.CurrentValue.CalendarService.LogicAppUrl);

            httpMessageHandlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Post
                  && req.RequestUri == expectedUri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task DeleteEventAsync_ByDefault_MakesHttpRequest()
        {
            // ARRANGE
            var configurationStub = CreateConfigurationStub();
            const string OUTLOOK_EVENT_ID = "thisisanoutlookeventid";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            httpMessageHandlerMock
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
                  "SendAsync",
                  ItExpr.IsAny<HttpRequestMessage>(),
                  ItExpr.IsAny<CancellationToken>()
               )
               .ReturnsAsync(new HttpResponseMessage()
               {
                   StatusCode = HttpStatusCode.Accepted,
                   Content = new StringContent(OUTLOOK_EVENT_ID),
               })
               .Verifiable();

            var httpClient = new HttpClient(httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://test.com/"),
            };

            var calendarService = new CalendarService(configurationStub, CreateTelemetryClientStub(), httpClient);

            var reservationStub = CreateDefaultReservation();
            reservationStub.OutlookEventId = OUTLOOK_EVENT_ID;

            // ACT
            await calendarService.DeleteEventAsync(reservationStub);

            // ASSERT
            var expectedUri = new Uri(configurationStub.CurrentValue.CalendarService.LogicAppUrl);

            httpMessageHandlerMock.Protected().Verify(
               "SendAsync",
               Times.Exactly(1),
               ItExpr.Is<HttpRequestMessage>(req =>
                  req.Method == HttpMethod.Post
                  && req.RequestUri == expectedUri
               ),
               ItExpr.IsAny<CancellationToken>()
            );
        }

        private Reservation CreateDefaultReservation() => new Reservation
        {
            User = new User
            {
                FirstName = "Test",
                LastName = "User",
                Company = "TestCompany",
                Email = "test@test.com",
            },
            VehiclePlateNumber = "TEST01",
            StartDate = new DateTime(2019, 01, 18, 11, 00, 00),
            EndDate = new DateTime(2019, 01, 18, 14, 00, 00),
            Location = "M/-3/170",
        };

        private IOptionsMonitor<CarWashConfiguration> CreateConfigurationStub()
        {
            var configurationStub = new Mock<IOptionsMonitor<CarWashConfiguration>>();
            configurationStub.Setup(s => s.CurrentValue).Returns(() => new CarWashConfiguration
            {
                CalendarService = new CarWashConfiguration.CalendarServiceConfiguration
                {
                    LogicAppUrl = "https://test.com/",
                }
            });

            return configurationStub.Object;
        }

        private static TelemetryClient CreateTelemetryClientStub()
        {
            TelemetryConfiguration configuration = new TelemetryConfiguration
            {
                TelemetryChannel = new Mock<ITelemetryChannel>().Object,
                InstrumentationKey = Guid.NewGuid().ToString()
            };
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            return new TelemetryClient(configuration);
        }
    }
}
