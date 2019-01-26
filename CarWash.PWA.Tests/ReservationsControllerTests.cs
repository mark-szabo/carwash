using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using CarWash.PWA.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class ReservationsControllerTests
    {
        private const string JOHN_EMAIL = "john.doe@test.com";
        private const string ADMIN_EMAIL = "admin@test.com";
        private const string CARWASH_ADMIN_EMAIL = "carwash@test.com";

        [Fact]
        public void GetReservations_ByDefault_ReturnsAListOfReservations()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = controller.GetReservations();

            Assert.NotEmpty(result);
            const int NUMBER_OF_JOHNS_RESERVATIONS = 2;
            Assert.Equal(NUMBER_OF_JOHNS_RESERVATIONS, result.Count());
        }

        [Fact]
        public async Task GetReservation_ByDefault_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceReservation = await dbContext.Reservation.FirstAsync();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetReservation(referenceReservation.Id);
            var ok = (OkObjectResult)result.Result;
            var reservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(referenceReservation.Id, reservation.Id);
        }

        [Fact]
        public async Task GetReservation_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceReservation = await dbContext.Reservation.FirstAsync();
            var controller = CreateControllerStub(dbContext);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.GetReservation(referenceReservation.Id);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetReservation_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetReservation("invalid id");

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetReservation_OfOtherUserAsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            var adminReservation = await dbContext.Reservation.FirstAsync(r => r.UserId == admin.Id);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetReservation(adminReservation.Id);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetReservation_OfOtherUserAsAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var johnReservation = await dbContext.Reservation.FirstAsync(r => r.UserId == john.Id);
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.GetReservation(johnReservation.Id);
            var ok = (OkObjectResult)result.Result;
            var reservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(johnReservation.Id, reservation.Id);
        }

        [Theory]
        [InlineData("TST000")]
        [InlineData("TST 000")]
        [InlineData("TST-000")]
        public async Task PostReservation_ByDefault_ReturnsNewReservation(string value)
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = value,
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(new DateTime(2019, 12, 05, 11, 00, 00), reservation.EndDate);
            Assert.Equal("TST000", reservation.VehiclePlateNumber);
        }

        [Fact]
        public async Task PostReservation_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_ForOtherUserAsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            var newReservation = new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_ForOtherUserAsAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var newReservation = new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.PostReservation(newReservation);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(john.Id, reservation.UserId);
        }

        [Fact]
        public async Task PostReservation_WithNoService_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithStartAndEndNotOnSameDay_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 06, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithStartLaterThanEnd_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 14, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 05, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithStartInPast_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2018, 12, 05, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithStartInPastAsCarWashAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var carWashAdmin = await dbContext.Users.SingleAsync(u => u.Email == CARWASH_ADMIN_EMAIL);
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2018, 12, 05, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PostReservation(newReservation);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(carWashAdmin.Id, reservation.UserId);
        }

        [Fact]
        public async Task PostReservation_WithStartNotInSlotAndEndNotSet_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 09, 29, 29, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithStartAndEndNotInSlot_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 09, 29, 29, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 05, 12, 29, 29, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithUserLimitReached_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 11, 05, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 05, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 12, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 12, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithUserLimitReachedAsAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 11, 05, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 05, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 11, 06, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 06, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 12, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 12, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.PostReservation(newReservation);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(admin.Id, reservation.UserId);
        }

        [Fact]
        public async Task PostReservation_WhenBlocked_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            await dbContext.Blocker.AddAsync(new Blocker
            {
                StartDate = new DateTime(2019, 12, 01, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 30, 23, 59, 59, DateTimeKind.Local),
            });
            await dbContext.SaveChangesAsync();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WhenBlockedAsCarWashAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            await dbContext.Blocker.AddAsync(new Blocker
            {
                StartDate = new DateTime(2019, 12, 01, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 30, 23, 59, 59, DateTimeKind.Local),
            });
            await dbContext.SaveChangesAsync();
            var carWashAdmin = await dbContext.Users.SingleAsync(u => u.Email == CARWASH_ADMIN_EMAIL);
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PostReservation(newReservation);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(carWashAdmin.Id, reservation.UserId);
        }

        [Fact]
        public async Task PostReservation_WithCompanyLimitReached_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST01",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 04, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 04, 14, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 17, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithSlotLimitReached_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 04, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostReservation_WithDropoffConfirmed_ReturnsNewReservationWithLocation()
        {
            var dbContext = CreateInMemoryDbContext();
            const string LOCATION = "M/-3/180";
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Location = LOCATION,
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation, true);
            var created = (CreatedAtActionResult)result.Result;
            var reservation = (ReservationViewModel)created.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.IsType<ReservationViewModel>(created.Value);
            Assert.Equal(LOCATION, reservation.Location);
        }

        [Fact]
        public async Task PostReservation_WithDropoffConfirmedButNoLocationSpecified_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var newReservation = new Reservation
            {
                VehiclePlateNumber = "TEST01",
                StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Interior },
                Private = false,
            };
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostReservation(newReservation, true);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
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
                Email = JOHN_EMAIL,
                FirstName = "John",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(john);
            var admin = new User
            {
                Email = ADMIN_EMAIL,
                FirstName = "John, the admin",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = true,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(admin);
            dbContext.Users.Add(new User
            {
                Email = CARWASH_ADMIN_EMAIL,
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
                State = State.Done,
                StartDate = new DateTime(2019, 11, 06, 08, 00, 00),
                EndDate = new DateTime(2019, 11, 06, 11, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement= 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.ReminderSentWaitingForKey,
                StartDate = new DateTime(2019, 11, 27, 11, 00, 00),
                EndDate = new DateTime(2019, 11, 27, 14, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior },
                TimeRequirement = 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST02",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 04, 08, 00, 00),
                EndDate = new DateTime(2019, 12, 04, 11, 00, 00),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });

            dbContext.SaveChanges();

            return dbContext;
        }

        private static CarWashConfiguration CreateConfigurationStub() => new CarWashConfiguration
        {
            Companies = new List<Company>
            {
                new Company { Name = Company.Carwash, TenantId = "00000000-0000-0000-0000-000000000000", DailyLimit = 0 },
                new Company { Name = "contoso", TenantId = "11111111-1111-1111-1111-111111111111", DailyLimit = 2 },
            },
            Slots = new List<Slot>
            {
                new Slot {StartTime = 8, EndTime = 11, Capacity = 1},
                new Slot {StartTime = 11, EndTime = 14, Capacity = 1},
                new Slot {StartTime = 14, EndTime = 17, Capacity = 1}
            },
            Reservation = new CarWashConfiguration.ReservationSettings
            {
                TimeUnit = 12,
                UserConcurrentReservationLimit = 2,
                MinutesToAllowReserveInPast = 120,
                HoursAfterCompanyLimitIsNotChecked = 11
            }
        };

        private static ReservationsController CreateControllerStub(ApplicationDbContext dbContext, string email = JOHN_EMAIL)
        {
            var configurationStub = CreateConfigurationStub();
            var calendarServiceStub = new Mock<ICalendarService>();
            var pushServiceStub = new Mock<IPushService>();
            var user = dbContext.Users.Single(u => u.Email == email);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);

            return new ReservationsController(
                configurationStub,
                dbContext,
                userControllerStub.Object,
                calendarServiceStub.Object,
                pushServiceStub.Object);
        }
    }
}
