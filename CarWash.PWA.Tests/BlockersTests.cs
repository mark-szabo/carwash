using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
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
    public class BlockersTests
    {
        private const string JOHN_EMAIL = "john.doe@test.com";
        private const string ADMIN_EMAIL = "admin@test.com";
        private const string CARWASH_ADMIN_EMAIL = "carwash@test.com";

        [Fact]
        public async Task GetBlockers_AsAdmin_ReturnsAListOfBlockers()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.GetBlockers();

            Assert.IsType<ActionResult<IEnumerable<Blocker>>>(result);
            Assert.IsAssignableFrom<IEnumerable<Blocker>>(result.Value);
            var blockers = result.Value;
            Assert.Single(blockers);
        }

        [Fact]
        public async Task GetBlockers_AsCarWashAdmin_ReturnsAListOfBlockers()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.GetBlockers();

            Assert.IsType<ActionResult<IEnumerable<Blocker>>>(result);
            Assert.IsAssignableFrom<IEnumerable<Blocker>>(result.Value);
            var blockers = result.Value;
            Assert.Single(blockers);
        }

        [Fact]
        public async Task GetBlockers_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetBlockers();

            Assert.IsType<ActionResult<IEnumerable<Blocker>>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetBlocker_AsAdmin_ReturnsAListOfBlockers()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceBlocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext, ADMIN_EMAIL);

            var result = await controller.GetBlocker(referenceBlocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = (OkObjectResult)result.Result;
            Assert.IsType<Blocker>(ok.Value);
            var blocker = (Blocker)ok.Value;
            Assert.NotNull(blocker);
        }

        [Fact]
        public async Task GetBlocker_AsCarWashAdmin_ReturnsAListOfBlockers()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceBlocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.GetBlocker(referenceBlocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = (OkObjectResult)result.Result;
            Assert.IsType<Blocker>(ok.Value);
            var blocker = (Blocker)ok.Value;
            Assert.NotNull(blocker);
        }

        [Fact]
        public async Task GetBlocker_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceBlocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.GetBlocker(referenceBlocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetBlocker_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var referenceBlocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.GetBlocker(referenceBlocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetBlocker_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.GetBlocker("invalid id");

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_AsCarWashAdmin_ReturnsNewBlocker()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 12, 02, 00, 00, 00, DateTimeKind.Local),
                EndDate = null,
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            var created = (CreatedAtActionResult)result.Result;
            Assert.IsType<Blocker>(created.Value);
            var blocker = (Blocker)created.Value;
            Assert.NotNull(blocker);
            Assert.Equal(new DateTime(2019, 12, 02, 23, 59, 59, DateTimeKind.Local), blocker.EndDate);
        }

        [Fact]
        public async Task PostBlocker_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 12, 02, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 12, 02, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithLongerThanOneMonth_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 02, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 04, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithStartLaterThanEnd_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 12, 04, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 12, 02, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithOverlappingAtBeginning_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            //an existing blocker is overlapping with the beginning of the new blocker
            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 18, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 20, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithOverlappingAtEnd_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            //an existing blocker is overlapping with the end of the new blocker
            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 22, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 24, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithOverlappingSubset_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            //an existing blocker is a subset of the new blocker
            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 21, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 21, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithOverlappingSuperset_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            //an existing blocker is a superset of the new blocker
            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 19, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 23, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithExactlyOverlapping_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            //an existing blocker is the same as the new
            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 20, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 22, 23, 59, 59, DateTimeKind.Local),
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task PostBlocker_WithExistingReservation_CancelsReservation()
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
            var emailServiceMock = new Mock<IEmailService>();
            emailServiceMock.Setup(m => m.Send(It.IsAny<Email>(), It.IsAny<TimeSpan?>())).Returns(Task.CompletedTask);
            var calendarServiceMock = new Mock<ICalendarService>();
            calendarServiceMock.Setup(m => m.DeleteEventAsync(It.IsAny<Reservation>())).Returns(Task.CompletedTask);
            var carWashAdmin = dbContext.Users.Single(u => u.Email == CARWASH_ADMIN_EMAIL);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(carWashAdmin);
            var controller = new BlockersController(
                dbContext,
                userControllerStub.Object,
                emailServiceMock.Object,
                calendarServiceMock.Object);

            var result = await controller.PostBlocker(new Blocker
            {
                StartDate = new DateTime(2019, 11, 05, 00, 00, 00, DateTimeKind.Local),
                EndDate = null,
            });

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<CreatedAtActionResult>(result.Result);
            var created = (CreatedAtActionResult)result.Result;
            Assert.IsType<Blocker>(created.Value);
            var blocker = (Blocker)created.Value;
            Assert.NotNull(blocker);
            emailServiceMock.Verify(m => m.Send(It.IsAny<Email>(), It.IsAny<TimeSpan?>()), Times.Once());
            calendarServiceMock.Verify(m => m.DeleteEventAsync(It.IsAny<Reservation>()), Times.Once());
        }

        [Fact]
        public async Task DeleteBlocker_ByDefault_DeletesBlocker()
        {
            var dbContext = CreateInMemoryDbContext();
            var blocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.DeleteBlocker(blocker.Id);
            var nonExistentBlocker = dbContext.Blocker.SingleOrDefault(b => b.Id == blocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(nonExistentBlocker);
        }

        [Fact]
        public async Task DeleteBlocker_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var blocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext);

            var result = await controller.DeleteBlocker(blocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task DeleteBlocker_GivenInvalidModel_ReturnsBadRequest()
        {
            var dbContext = CreateInMemoryDbContext();
            var blocker = await dbContext.Blocker.FirstAsync();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);
            controller.ModelState.AddModelError("error", "some error");

            var result = await controller.DeleteBlocker(blocker.Id);

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteBlocker_WithInvalidId_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, CARWASH_ADMIN_EMAIL);

            var result = await controller.DeleteBlocker("invalid id");

            Assert.IsType<ActionResult<Blocker>>(result);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-blockerscontroller");
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
            dbContext.Users.Add(new User
            {
                Email = ADMIN_EMAIL,
                FirstName = "John, the admin",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = true,
                IsCarwashAdmin = false,
            });
            dbContext.Users.Add(new User
            {
                Email = CARWASH_ADMIN_EMAIL,
                FirstName = "John, from CarWash",
                LastName = "Doe",
                Company = Company.Carwash,
                IsAdmin = false,
                IsCarwashAdmin = true,
            });

            dbContext.Blocker.Add(new Blocker
            {
                StartDate = new DateTime(2019, 11, 20, 00, 00, 00, DateTimeKind.Local),
                EndDate = new DateTime(2019, 11, 22, 23, 59, 59, DateTimeKind.Local),
            });

            dbContext.SaveChanges();

            return dbContext;
        }

        private static BlockersController CreateControllerStub(ApplicationDbContext dbContext, string email = JOHN_EMAIL)
        {
            var emailServiceStub = new Mock<IEmailService>();
            var calendarServiceStub = new Mock<ICalendarService>();
            var user = dbContext.Users.Single(u => u.Email == email);
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(user);

            return new BlockersController(
                dbContext,
                userControllerStub.Object,
                emailServiceStub.Object,
                calendarServiceStub.Object);
        }
    }
}
