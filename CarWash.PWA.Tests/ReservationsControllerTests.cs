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
        private const string JANE_EMAIL = "jane.doe@test.com";
        private const string JOHNNY_EMAIL = "johnny.doe@test.com";
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

        [Theory]
        [InlineData("TST000")]
        [InlineData("TST 000")]
        [InlineData("TST-000")]
        public async Task PutReservation_ByDefault_ReturnsNewReservation(string value)
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.VehiclePlateNumber = value;
            var NEW_START_DATE = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local);
            reservation.StartDate = NEW_START_DATE;
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(NEW_START_DATE, updatedReservation.StartDate);
            Assert.Equal(new DateTime(2019, 12, 05, 11, 00, 00), updatedReservation.EndDate);
            Assert.Equal("TST000", updatedReservation.VehiclePlateNumber);
            Assert.Equal(reservation.State, updatedReservation.State);
        }

        [Fact]
        public async Task PutReservation_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            var controller = CreateControllerStub(dbContext);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_ForOtherUserAsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            // TEST02 is admin's reservation
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST02");
            reservation.VehiclePlateNumber = "TST000";
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_ForOtherUserAsAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.VehiclePlateNumber = "TST000";
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.PutReservation(reservation.Id, reservation);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(john.Id, updatedReservation.UserId);
            Assert.Equal("TST000", updatedReservation.VehiclePlateNumber);
        }

        [Fact]
        public async Task PutReservation_WithUserUpdated_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.UserId = admin.Id;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithUserUpdatedAsAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST02");
            reservation.UserId = john.Id;
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.PutReservation(reservation.Id, reservation);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(john.Id, updatedReservation.UserId);
        }

        [Fact]
        public async Task PutReservation_WithNoService_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.Services = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithStartAndEndNotOnSameDay_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local);
            reservation.EndDate = new DateTime(2019, 12, 06, 11, 00, 00, DateTimeKind.Local);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithStartLaterThanEnd_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 05, 14, 00, 00, DateTimeKind.Local);
            reservation.EndDate = new DateTime(2019, 12, 05, 11, 00, 00, DateTimeKind.Local);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithStartInPast_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2018, 12, 05, 14, 00, 00, DateTimeKind.Local);
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithStartInPastAsCarWashAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var carWashAdmin = await dbContext.Users.SingleAsync(u => u.Email == CARWASH_ADMIN_EMAIL);
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST03");
            reservation.StartDate = new DateTime(2018, 12, 05, 14, 00, 00, DateTimeKind.Local);
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PutReservation(reservation.Id, reservation);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(carWashAdmin.Id, updatedReservation.UserId);
        }

        [Fact]
        public async Task PutReservation_WithStartNotInSlotAndEndNotSet_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 05, 09, 29, 29, DateTimeKind.Local);
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithStartAndEndNotInSlot_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 05, 09, 29, 29, DateTimeKind.Local);
            reservation.EndDate = new DateTime(2019, 12, 05, 12, 29, 29, DateTimeKind.Local);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WhenBlocked_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            await dbContext.Blocker.AddAsync(new Blocker
            {
                StartDate = new DateTime(2019, 12, 01, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 30, 23, 59, 59, DateTimeKind.Local),
            });
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local);
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WhenBlockedAsCarWashAdmin_ReturnsReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            await dbContext.Blocker.AddAsync(new Blocker
            {
                StartDate = new DateTime(2019, 12, 01, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 30, 23, 59, 59, DateTimeKind.Local),
            });
            await dbContext.SaveChangesAsync();
            var carWashAdmin = await dbContext.Users.SingleAsync(u => u.Email == CARWASH_ADMIN_EMAIL);
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST03");
            reservation.StartDate = new DateTime(2019, 12, 05, 08, 00, 00, DateTimeKind.Local);
            reservation.EndDate = null;
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PutReservation(reservation.Id, reservation);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(carWashAdmin.Id, updatedReservation.UserId);
        }

        [Fact]
        public async Task PutReservation_WithCompanyLimitReached_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST02",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 04, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 04, 14, 00, 00, DateTimeKind.Local);
            reservation.EndDate = new DateTime(2019, 12, 04, 17, 00, 00, DateTimeKind.Local);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithSlotLimitReached_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.StartDate = new DateTime(2019, 12, 04, 08, 00, 00, DateTimeKind.Local);
            reservation.EndDate = new DateTime(2019, 12, 04, 11, 00, 00, DateTimeKind.Local);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PutReservation_WithDropoffConfirmed_ReturnsNewReservationWithLocation()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            const string LOCATION = "M/-3/180";
            reservation.Location = LOCATION;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation, true);
            var ok = (OkObjectResult)result.Result;
            var updatedReservation = (ReservationViewModel)ok.Value;

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<ReservationViewModel>(ok.Value);
            Assert.Equal(LOCATION, reservation.Location);
        }

        [Fact]
        public async Task PutReservation_WithDropoffConfirmedButNoLocationSpecified_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            reservation.Location = null;
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PutReservation(reservation.Id, reservation, true);

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteReservation_ByDefault_DeletesReservation()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            var controller = CreateControllerStub(dbContext);

            var result = await controller.DeleteReservation(reservation.Id);
            var notExistentReservation = await dbContext.Reservation.AsNoTracking().FirstOrDefaultAsync(r => r.VehiclePlateNumber == "TEST01");

            Assert.IsType<ActionResult<ReservationViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(notExistentReservation);
        }

        [Fact]
        public async Task GetCompanyReservations_AsAdmin_ReturnsAListOfReservations()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.GetCompanyReservations();
            var ok = (OkObjectResult)result.Result;
            var reservations = (IEnumerable<AdminReservationViewModel>)ok.Value;

            Assert.IsType<ActionResult<IEnumerable<AdminReservationViewModel>>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<AdminReservationViewModel>>(ok.Value);
            Assert.NotEmpty(reservations);
            const int NUMBER_OF_COMPANY_RESERVATIONS_NOT_COUNTING_ADMINS_OWN = 6;
            Assert.Equal(NUMBER_OF_COMPANY_RESERVATIONS_NOT_COUNTING_ADMINS_OWN, reservations.Count());
        }

        [Fact]
        public async Task GetCompanyReservations_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetCompanyReservations();

            Assert.IsType<ActionResult<IEnumerable<AdminReservationViewModel>>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetBacklog_AsCarWashAdmin_ReturnsAListOfReservations()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.GetBacklog();
            var ok = (OkObjectResult)result.Result;
            var reservations = (IEnumerable<AdminReservationViewModel>)ok.Value;

            Assert.IsType<ActionResult<IEnumerable<AdminReservationViewModel>>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsAssignableFrom<IEnumerable<AdminReservationViewModel>>(ok.Value);
            Assert.NotEmpty(reservations);
            const int NUMBER_OF_ALL_RESERVATIONS_EXCEPT_PAST_DONE = 8;
            Assert.Equal(NUMBER_OF_ALL_RESERVATIONS_EXCEPT_PAST_DONE, reservations.Count());
        }

        [Fact]
        public async Task GetBacklog_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetBacklog();

            Assert.IsType<ActionResult<IEnumerable<AdminReservationViewModel>>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task ConfirmDropoff_ByDefault_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            const string LOCATION = "M/-3/180";
            var controller = CreateControllerStub(dbContext);

            var result = await controller.ConfirmDropoff(reservation.Id, LOCATION);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoff_WithNoLocation_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.AsNoTracking().FirstAsync(r => r.VehiclePlateNumber == "TEST01");
            var controller = CreateControllerStub(dbContext);

            var result = await controller.ConfirmDropoff(reservation.Id, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoff_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            const string LOCATION = "M/-3/180";
            var controller = CreateControllerStub(dbContext);

            var result = await controller.ConfirmDropoff("invalid id", LOCATION);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoff_OfOtherUserAsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            var adminReservation = await dbContext.Reservation.FirstAsync(r => r.UserId == admin.Id);
            const string LOCATION = "M/-3/180";
            var controller = CreateControllerStub(dbContext);

            var result = await controller.ConfirmDropoff(adminReservation.Id, LOCATION);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoff_OfOtherUserAsAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var johnReservation = await dbContext.Reservation.FirstAsync(r => r.UserId == john.Id);
            const string LOCATION = "M/-3/180";
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.ConfirmDropoff(johnReservation.Id, LOCATION);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FirstAsync(r => r.UserId == john.Id);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithNoEmail_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            const string LOCATION = "M/-3/180";
            var model = new ConfirmDropoffByEmailViewModel
            {
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithNoLocation_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = JOHN_EMAIL,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithInvalidEmail_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            const string LOCATION = "M/-3/180";
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = "invalid email",
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithNoReservations_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            const string LOCATION = "M/-3/180";
            // Jane does not have any reservations.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = JANE_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithOneActiveReservation_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservationIdToBeUpdated = (await dbContext.Reservation.SingleAsync(r => r.User.Email == JOHN_EMAIL && (r.State == State.SubmittedNotActual || r.State == State.ReminderSentWaitingForKey))).Id;
            const string LOCATION = "M/-3/180";
            // John has exactly one active reservation.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = JOHN_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservationIdToBeUpdated);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithOneReservationWaitingForKey_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservationIdToBeUpdated = (await dbContext.Reservation.SingleAsync(r => r.User.Email == JOHN_EMAIL && r.State == State.ReminderSentWaitingForKey)).Id;
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
            const string LOCATION = "M/-3/180";
            // John has exactly one reservation waiting for key.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = JOHN_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservationIdToBeUpdated);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithOneReservationTodayWaitingForKey_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = await dbContext.Users.SingleAsync(u => u.Email == JOHN_EMAIL);
            var today = DateTime.Today;
            var reservationToBeUpdated = await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.ReminderSentWaitingForKey,
                StartDate = new DateTime(today.Year, today.Month, today.Day, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(today.Year, today.Month, today.Day, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            const string LOCATION = "M/-3/180";
            // John has two reservations waiting for key, but only one of those is today.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = JOHN_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservationToBeUpdated.Entity.Id);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithOneActiveReservationToday_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            var today = DateTime.Today;
            var reservationToBeUpdated = await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST02",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(today.Year, today.Month, today.Day, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(today.Year, today.Month, today.Day, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            const string LOCATION = "M/-3/180";
            // Admin has two reservations scheduled, but only one of those is today.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = ADMIN_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservationToBeUpdated.Entity.Id);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithPlateSpecified_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            const string PLATE = "TEST05";
            var reservationToBeUpdated = await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = PLATE,
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 01, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 01, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            const string LOCATION = "M/-3/180";
            // Admin has two reservations scheduled, both in the future. But we specify the vehicle plate number which is unique among the active reservations of Admin.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = ADMIN_EMAIL,
                Location = LOCATION,
                VehiclePlateNumber = PLATE,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservationToBeUpdated.Entity.Id);
            Assert.Equal(LOCATION, updatedReservation.Location);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithConflictAndPlateNotSpecified_ReturnsConflict()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            const string PLATE = "TEST05";
            var reservationToBeUpdated = await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = PLATE,
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 01, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 01, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            const string LOCATION = "M/-3/180";
            // Admin has two reservations scheduled, both in the future. And we do not specify the vehicle plate number.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = ADMIN_EMAIL,
                Location = LOCATION,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<ConflictObjectResult>(result);
            var message = ((ConflictObjectResult)result).Value;
            Assert.Equal("More than one reservation found where the reservation state is submitted or waiting for key. Please specify vehicle plate number!", message);
        }

        [Fact]
        public async Task ConfirmDropoffByEmail_WithConflictAndPlateSpecified_ReturnsConflict()
        {
            var dbContext = CreateInMemoryDbContext();
            var admin = await dbContext.Users.SingleAsync(u => u.Email == ADMIN_EMAIL);
            const string PLATE = "TEST02";
            var reservationToBeUpdated = await dbContext.Reservation.AddAsync(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = PLATE,
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 01, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 01, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                Private = false,
            });
            await dbContext.SaveChangesAsync();
            const string LOCATION = "M/-3/180";
            // Admin has two reservations scheduled, both in the future. We do specify the vehicle plate number, but both of the active reservations have the same plate.
            var model = new ConfirmDropoffByEmailViewModel
            {
                Email = ADMIN_EMAIL,
                Location = LOCATION,
                VehiclePlateNumber = PLATE,
            };
            var controller = CreateServiceControllerStub(dbContext);

            var result = await controller.ConfirmDropoffByEmail(model);

            Assert.IsType<ConflictObjectResult>(result);
            var message = ((ConflictObjectResult)result).Value;
            Assert.Equal("More than one reservation found where the reservation state is submitted or waiting for key.", message);
        }

        [Fact]
        public async Task StartWash_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.StartWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(State.WashInProgress, updatedReservation.State);
        }

        [Fact]
        public async Task StartWash_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.StartWash(reservation.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task StartWash_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.StartWash(null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task StartWash_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.StartWash("invalid id");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CompleteWash_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress && !r.Private);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(State.Done, updatedReservation.State);
        }

        [Fact]
        public async Task CompleteWash_ForPrivateCarAsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress && r.Private);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(State.NotYetPaid, updatedReservation.State);
        }

        [Fact]
        public async Task CompleteWash_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task CompleteWash_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.CompleteWash(null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CompleteWash_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.CompleteWash("invalid id");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CompleteWash_ForUserWithDisabledNotifications_DoesNotSendsNotification()
        {
            var dbContext = CreateInMemoryDbContext();
            var johnny = await dbContext.Users.SingleAsync(u => u.Email == JOHNNY_EMAIL);
            johnny.NotificationChannel = NotificationChannel.Disabled;
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>()));
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()));
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>()), Times.Never());
            pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Never());
        }

        [Fact]
        public async Task CompleteWash_ForUserWithEmailNotifications_SendsEmailNotification()
        {
            var dbContext = CreateInMemoryDbContext();
            var johnny = await dbContext.Users.SingleAsync(u => u.Email == JOHNNY_EMAIL);
            johnny.NotificationChannel = NotificationChannel.Email;
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>())).Returns(Task.CompletedTask);
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>())).Returns(Task.CompletedTask);
            var calendarServiceStub = new Mock<ICalendarService>();
            var user = dbContext.Users.Single(u => u.Email == CARWASH_ADMIN_EMAIL);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);
            var controller = new ReservationsController(CreateConfigurationStub(), dbContext, userControllerStub.Object, emailServiceMock.Object, calendarServiceStub.Object, pushServiceMock.Object);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>()), Times.Once());
            pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Never());
        }

        [Fact]
        public async Task CompleteWash_ForUserWithNotificationsNotSet_SendsEmailNotification()
        {
            var dbContext = CreateInMemoryDbContext();
            var johnny = await dbContext.Users.SingleAsync(u => u.Email == JOHNNY_EMAIL);
            johnny.NotificationChannel = NotificationChannel.NotSet;
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>())).Returns(Task.CompletedTask);
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>())).Returns(Task.CompletedTask);
            var calendarServiceStub = new Mock<ICalendarService>();
            var user = dbContext.Users.Single(u => u.Email == CARWASH_ADMIN_EMAIL);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);
            var controller = new ReservationsController(CreateConfigurationStub(), dbContext, userControllerStub.Object, emailServiceMock.Object, calendarServiceStub.Object, pushServiceMock.Object);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>()), Times.Once());
            pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Never());
        }

        [Fact]
        public async Task CompleteWash_ForUserWithPushNotifications_SendsPushNotification()
        {
            var dbContext = CreateInMemoryDbContext();
            var johnny = await dbContext.Users.SingleAsync(u => u.Email == JOHNNY_EMAIL);
            johnny.NotificationChannel = NotificationChannel.Push;
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>())).Returns(Task.CompletedTask);
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>())).Returns(Task.CompletedTask);
            var calendarServiceStub = new Mock<ICalendarService>();
            var user = dbContext.Users.Single(u => u.Email == CARWASH_ADMIN_EMAIL);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);
            var controller = new ReservationsController(CreateConfigurationStub(), dbContext, userControllerStub.Object, emailServiceMock.Object, calendarServiceStub.Object, pushServiceMock.Object);

            var result = await controller.CompleteWash(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>()), Times.Never());
            pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Once());
        }

        [Fact]
        public async Task ConfirmPayment_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.NotYetPaid);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.ConfirmPayment(reservation.Id);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(State.Done, updatedReservation.State);
        }

        [Fact]
        public async Task ConfirmPayment_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.NotYetPaid);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.ConfirmPayment(reservation.Id);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task ConfirmPayment_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.ConfirmPayment(null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ConfirmPayment_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.NotYetPaid);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.ConfirmPayment("invalid id");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SetState_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetState(reservation.Id, State.DropoffAndLocationConfirmed);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(State.DropoffAndLocationConfirmed, updatedReservation.State);
        }

        [Fact]
        public async Task SetState_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.SetState(reservation.Id, State.DropoffAndLocationConfirmed);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task SetState_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetState(null, State.DropoffAndLocationConfirmed);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SetState_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetState("invalid id", State.DropoffAndLocationConfirmed);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddCarwashComment_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string COMMENT = "test";

            var result = await controller.AddCarwashComment(reservation.Id, COMMENT);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(COMMENT, updatedReservation.CarwashComment);
        }

        [Fact]
        public async Task AddCarwashComment_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext);
            const string COMMENT = "test";

            var result = await controller.AddCarwashComment(reservation.Id, COMMENT);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task AddCarwashComment_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string COMMENT = "test";

            var result = await controller.AddCarwashComment(null, COMMENT);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddCarwashComment_WithNoComment_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.AddCarwashComment(reservation.Id, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AddCarwashComment_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string COMMENT = "test";

            var result = await controller.AddCarwashComment("invalid id", COMMENT);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddCarwashComment_ForUserWithPushNotifications_SendsPushNotification()
        {
            var dbContext = CreateInMemoryDbContext();
            var johnny = await dbContext.Users.SingleAsync(u => u.Email == JOHNNY_EMAIL);
            johnny.NotificationChannel = NotificationChannel.Push;
            await dbContext.SaveChangesAsync();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.WashInProgress);
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>())).Returns(Task.CompletedTask);
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>())).Returns(Task.CompletedTask);
            var calendarServiceStub = new Mock<ICalendarService>();
            var user = dbContext.Users.Single(u => u.Email == CARWASH_ADMIN_EMAIL);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);
            var controller = new ReservationsController(CreateConfigurationStub(), dbContext, userControllerStub.Object, emailServiceMock.Object, calendarServiceStub.Object, pushServiceMock.Object);
            const string COMMENT = "test";

            var result = await controller.AddCarwashComment(reservation.Id, COMMENT);

            Assert.IsType<NoContentResult>(result);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>()), Times.Never());
            pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Once());
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task SetMpv_AsCarWashAdmin_ReturnsNoContent(bool mpv)
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetMpv(reservation.Id, mpv);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(mpv, updatedReservation.Mpv);
        }

        [Fact]
        public async Task SetMpv_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext);

            var result = await controller.SetMpv(reservation.Id, true);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task SetMpv_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetMpv(null, true);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SetMpv_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.SetMpv("invalid id", true);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateServices_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            var SERVICES = new List<ServiceType> { ServiceType.Exterior, ServiceType.PreWash, ServiceType.WheelCleaning };

            var result = await controller.UpdateServices(reservation.Id, SERVICES);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(SERVICES, updatedReservation.Services);
        }

        [Fact]
        public async Task UpdateServices_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext);
            var SERVICES = new List<ServiceType> { ServiceType.Exterior, ServiceType.PreWash, ServiceType.WheelCleaning };

            var result = await controller.UpdateServices(reservation.Id, SERVICES);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateServices_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            var SERVICES = new List<ServiceType> { ServiceType.Exterior, ServiceType.PreWash, ServiceType.WheelCleaning };

            var result = await controller.UpdateServices(null, SERVICES);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateServices_WithNoService_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.UpdateServices(reservation.Id, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateServices_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            var SERVICES = new List<ServiceType> { ServiceType.Exterior, ServiceType.PreWash, ServiceType.WheelCleaning };

            var result = await controller.UpdateServices("invalid id", SERVICES);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateLocation_AsCarWashAdmin_ReturnsNoContent()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string LOCATION = "M/-1/15";

            var result = await controller.UpdateLocation(reservation.Id, LOCATION);

            Assert.IsType<NoContentResult>(result);
            var updatedReservation = await dbContext.Reservation.FindAsync(reservation.Id);
            Assert.Equal(LOCATION, updatedReservation.Location);
        }

        [Fact]
        public async Task UpdateLocation_AsNotCarWashAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext);
            const string LOCATION = "M/-1/15";

            var result = await controller.UpdateLocation(reservation.Id, LOCATION);

            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task UpdateLocation_WithNoId_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string LOCATION = "M/-1/15";

            var result = await controller.UpdateLocation(null, LOCATION);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateLocation_WithNoLocation_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.UpdateLocation(reservation.Id, null);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task UpdateLocation_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var reservation = await dbContext.Reservation.FirstAsync(r => r.State == State.DropoffAndLocationConfirmed);
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            const string LOCATION = "M/-1/15";

            var result = await controller.UpdateLocation("invalid id", LOCATION);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetObfuscatedReservations_ByDefault_ReturnsAListOfReservations()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = controller.GetObfuscatedReservations();

            Assert.NotEmpty(result);
            const int NUMBER_OF_ALL_RESERVATIONS = 8;
            Assert.Equal(NUMBER_OF_ALL_RESERVATIONS, result.Count());
        }

        [Fact]
        public async Task GetNotAvailableDatesAndTimes_AsCarWashAdmin_ReturnsEmptyList()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.GetNotAvailableDatesAndTimes();

            Assert.Empty(result.Dates);
            Assert.Empty(result.Times);
        }

        [Fact]
        public async Task GetNotAvailableDatesAndTimes_AsNotCarWashAdmin_ReturnsAListOfNotAvailableDatesAndTimes()
        {
            var dbContext = CreateInMemoryDbContext();
            await dbContext.Blocker.AddAsync(new Blocker
            {
                StartDate = new DateTime(2019, 11, 20, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 22, 23, 59, 59, DateTimeKind.Local),
            });
            await dbContext.SaveChangesAsync();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetNotAvailableDatesAndTimes();

            Assert.NotEmpty(result.Dates);
            Assert.NotEmpty(result.Times);
            Assert.True(result.Dates.Count() > 1);
            Assert.True(result.Times.Count() > 1);
        }

        [Fact]
        public async Task GetLastSettings_ByDefault_ReturnsLastSettings()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetLastSettings();
            var ok = (OkObjectResult)result.Result;
            var lastSettings = (LastSettingsViewModel)ok.Value;

            Assert.IsType<ActionResult<LastSettingsViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<LastSettingsViewModel>(ok.Value);
            Assert.Equal("TEST00", lastSettings.VehiclePlateNumber);
            Assert.Equal("M/-1/11", lastSettings.Location);
            Assert.Equal(new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior }, lastSettings.Services);
        }

        [Fact]
        public async Task GetReservationCapacity_ByDefault_ReturnsAListOfSlotCapacity()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetReservationCapacity(new DateTime(2019, 12, 04));
            var ok = (OkObjectResult)result.Result;
            var slotCapacity = ((IEnumerable<ReservationCapacityViewModel>)ok.Value).ToList();

            Assert.NotEmpty(slotCapacity);
            Assert.Equal(3, slotCapacity.Count);
            Assert.Equal(0, slotCapacity[0].FreeCapacity);
            Assert.Equal(1, slotCapacity[1].FreeCapacity);
            Assert.Equal(1, slotCapacity[2].FreeCapacity);
        }

        [Fact]
        public async Task Export_AsCarWashAdmin_ReturnsFile()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.Export(new DateTime(2019, 01, 01), new DateTime(2019, 12, 31));

            Assert.IsType<FileStreamResult>(result);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Export_AsAdmin_ReturnsFile()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.Export(new DateTime(2019, 01, 01), new DateTime(2019, 12, 31));

            Assert.IsType<FileStreamResult>(result);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Export_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.Export(new DateTime(2019, 01, 01), new DateTime(2019, 12, 31));

            Assert.IsType<ForbidResult>(result);
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
            var jane = new User
            {
                Email = JANE_EMAIL,
                FirstName = "Jane",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(jane);
            var johnny = new User
            {
                Email = JOHNNY_EMAIL,
                FirstName = "Johny",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            dbContext.Users.Add(johnny);
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
            var carWashAdmin = new User
            {
                Email = CARWASH_ADMIN_EMAIL,
                FirstName = "John, from CarWash",
                LastName = "Doe",
                Company = Company.Carwash,
                IsAdmin = false,
                IsCarwashAdmin = true,
            };
            dbContext.Users.Add(carWashAdmin);

            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST00",
                State = State.Done,
                StartDate = new DateTime(2019, 11, 06, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 06, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
                Location = "M/-1/11",
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = john.Id,
                VehiclePlateNumber = "TEST01",
                State = State.ReminderSentWaitingForKey,
                StartDate = new DateTime(2019, 11, 27, 11, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 27, 14, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior },
                TimeRequirement = 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = johnny.Id,
                VehiclePlateNumber = "TEST11",
                State = State.DropoffAndLocationConfirmed,
                StartDate = new DateTime(2019, 11, 07, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 07, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = johnny.Id,
                VehiclePlateNumber = "TEST12",
                State = State.WashInProgress,
                StartDate = new DateTime(2019, 11, 08, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 08, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = johnny.Id,
                VehiclePlateNumber = "TEST13",
                State = State.WashInProgress,
                StartDate = new DateTime(2019, 11, 09, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 09, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = true,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = johnny.Id,
                VehiclePlateNumber = "TEST14",
                State = State.NotYetPaid,
                StartDate = new DateTime(2019, 11, 10, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 10, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = true,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = admin.Id,
                VehiclePlateNumber = "TEST02",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 04, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 11, 00, 00, DateTimeKind.Local),
                Services = new List<ServiceType> { ServiceType.Exterior, ServiceType.Interior },
                TimeRequirement = 12,
                Private = false,
            });
            dbContext.Reservation.Add(new Reservation
            {
                UserId = carWashAdmin.Id,
                VehiclePlateNumber = "TEST03",
                State = State.SubmittedNotActual,
                StartDate = new DateTime(2019, 12, 03, 08, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 03, 11, 00, 00, DateTimeKind.Local),
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
            var emailServiceStub = new Mock<IEmailService>();
            var calendarServiceStub = new Mock<ICalendarService>();
            var pushServiceStub = new Mock<IPushService>();
            var user = dbContext.Users.Single(u => u.Email == email);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);

            return new ReservationsController(
                configurationStub,
                dbContext,
                userControllerStub.Object,
                emailServiceStub.Object,
                calendarServiceStub.Object,
                pushServiceStub.Object);
        }

        private static ReservationsController CreateServiceControllerStub(ApplicationDbContext dbContext)
        {
            var configurationStub = CreateConfigurationStub();
            var emailServiceStub = new Mock<IEmailService>();
            var calendarServiceStub = new Mock<ICalendarService>();
            var pushServiceStub = new Mock<IPushService>();
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(() => null);

            return new ReservationsController(
                configurationStub,
                dbContext,
                userControllerStub.Object,
                emailServiceStub.Object,
                calendarServiceStub.Object,
                pushServiceStub.Object);
        }
    }
}
