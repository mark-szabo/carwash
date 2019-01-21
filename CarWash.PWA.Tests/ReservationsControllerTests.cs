using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using CarWash.PWA.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class ReservationsControllerTests
    {
        [Fact]
        public void GetReservations_ByDefault_ReturnsAListOfReservations()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);
            
            var result = controller.GetReservations();

            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count());
        }

        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-reservationscontroller");
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // Datae database
            dbContext.Database.EnsureDeleted();

            // Seed database
            var john = new User
            {
                Email = "john.doe@test.com",
                FirstName = "John",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(john);
            dbContext.Users.Add(new User
            {
                Email = "admin@test.com",
                FirstName = "John, the admin",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = true,
                IsCarwashAdmin = false,
            });
            dbContext.Users.Add(new User
            {
                Email = "carwash@test.com",
                FirstName = "John, from CarWash",
                LastName = "Doe",
                Company = Company.Carwash,
                IsAdmin = false,
                IsCarwashAdmin = true,
            });

            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.ReminderSentWaitingForKey,
                StartDate = new DateTime(2019, 11, 06, 08, 00, 00),
                EndDate = new DateTime(2019, 11, 06, 11, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 11, 27, 11, 00, 00),
                EndDate = new DateTime(2019, 11, 27, 14, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior },
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 04, 08, 00, 00),
                EndDate = new DateTime(2019, 12, 04, 11, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });

            dbContext.SaveChanges();

            return dbContext;
        }

        private CarWashConfiguration CreateConfigurationStub() => new CarWashConfiguration
        {
            Companies = new List<Company>
            {
                new Company { Name = Company.Carwash, TenantId = "00000000-0000-0000-0000-000000000000", DailyLimit = 0 },
                new Company { Name = "contoso", TenantId = "11111111-1111-1111-1111-111111111111", DailyLimit = 5 },
            }
        };

        private ReservationsController CreateControllerStub(ApplicationDbContext dbContext)
        {
            var configurationStub = CreateConfigurationStub();
            var calendarServiceStub = new Mock<ICalendarService>();
            var pushServiceStub = new Mock<IPushService>();
            var john = dbContext.Users.Single(u => u.Email == "john.doe@test.com");
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(john);

            return new ReservationsController(
                configurationStub,
                dbContext,
                userControllerStub.Object,
                calendarServiceStub.Object,
                pushServiceStub.Object);
        }
    }
}
