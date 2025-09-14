using System;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Generic;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class WellKnownTests
    {
        private const string JOHN_EMAIL = "john.doe@test.com";

        [Fact]
        public void GetVapidPublicKey_ByDefault_ReturnsVapidPublicKey()
        {
            var dbContext = CreateInMemoryDbContext();
            var userServiceStub = new Mock<IUserService>();
            var hostingEnvironmentStub = new Mock<IWebHostEnvironment>();
            var pushServiceMock = new Mock<IPushService>();
            const string PUBLIC_KEY = "test public key";
            pushServiceMock.Setup(m => m.GetVapidPublicKey()).Returns(PUBLIC_KEY);
            var configurationStub = CreateConfigurationStub();
            var featureManagerMock = new Mock<Microsoft.FeatureManagement.IFeatureManager>();
            var controller = new WellKnownController(configurationStub, dbContext, pushServiceMock.Object, featureManagerMock.Object);

            var result = controller.GetVapidPublicKey();

            Assert.IsType<ActionResult<string>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = (OkObjectResult)result.Result;
            Assert.IsType<string>(ok.Value);
            var publicKey = (string)ok.Value;
            Assert.Equal(PUBLIC_KEY, publicKey);
            pushServiceMock.Verify(m => m.GetVapidPublicKey(), Times.Once());
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-wellknown");
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // Recreate database
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // Seed database
            var john = new User
            {
                Email = JOHN_EMAIL,
                FirstName = "John",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(john);

            dbContext.SaveChanges();

            return dbContext;
        }
        private static IOptionsMonitor<CarWashConfiguration> CreateConfigurationStub()
        {
            var configurationStub = new Mock<IOptionsMonitor<CarWashConfiguration>>();
            configurationStub.Setup(s => s.CurrentValue).Returns(() => new CarWashConfiguration
            {
                TimeZone = "UTC", // Explicitly set timezone for tests
                Slots = new List<Slot>
            {
                new Slot {StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(11, 0, 0), Capacity = 1},
                new Slot {StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(14, 0, 0), Capacity = 1},
                new Slot {StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(17, 0, 0), Capacity = 1}
            },
                Reservation = new CarWashConfiguration.ReservationSettings
                {
                    TimeUnit = 12,
                    UserConcurrentReservationLimit = 2,
                    MinutesToAllowReserveInPast = 120,
                    HoursAfterCompanyLimitIsNotChecked = 11
                },
                Services =
            [
                new() {
                    Id = 0,
                    Name = "exterior",
                    Group = "Basics",
                    TimeInMinutes = 12,
                    Price = 6311,
                    PriceMpv = 7889,
                },
                new() {
                    Id = 1,
                    Name = "interior",
                    Group = "Basics",
                    TimeInMinutes = 12,
                    Price = 3610,
                    PriceMpv = 5406,
                },
                new() {
                    Id = 2,
                    Name = "carpet",
                    Group = "Basics",
                    TimeInMinutes = 24,
                    Price = -1,
                    PriceMpv = -1,
                },
                new() {
                    Id = 9,
                    Name = "wheel cleaning",
                    Group = "Extras",
                    TimeInMinutes = 0,
                    Price = 2073,
                    PriceMpv = 2073,
                },
                new() {
                    Id = 13,
                    Name = "prewash",
                    Group = "Extras",
                    TimeInMinutes = 0,
                    Price = 1732,
                    PriceMpv = 1732,
                },
            ],
            });

            return configurationStub.Object;
        }
    }
}
