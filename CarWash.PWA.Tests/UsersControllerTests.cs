using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class UsersControllerTests
    {
        private const string JOHN_EMAIL = "john.doe@test.com";
        private const string ADMIN_EMAIL = "admin@test.com";
        private const string CARWASH_ADMIN_EMAIL = "carwash@test.com";

        [Fact]
        public void GetMe_ByDefault_ReturnsCurrentUser()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = controller.GetMe();
            var ok = (OkObjectResult)result.Result;
            var user = (UserViewModel)ok.Value;

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserViewModel>(ok.Value);
            Assert.Equal(john.Id, user.Id);
        }

        /*[Fact]
        public void GetMe_WithFakeUser_ReturnsNotFound()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(new User { Email = "fakeemailaddress" });
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = controller.GetMe();

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<NotFoundResult>(result.Result);
        }*/

        [Fact]
        public void GetUsers_AsAdmin_ReturnsUsersInCompany()
        {
            var controller = CreateDefaultController(ADMIN_EMAIL);

            var result = controller.GetUsers();
            var ok = (OkObjectResult)result.Result;
            var users = (IEnumerable<UserViewModel>)ok.Value;

            Assert.IsType<ActionResult<IEnumerable<UserViewModel>>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotEmpty(users);
            const int NUMBER_OF_USERS_IN_CONTOSO_COMPANY = 2;
            Assert.Equal(NUMBER_OF_USERS_IN_CONTOSO_COMPANY, users.Count());
        }

        [Fact]
        public void GetUsers_AsNotAdmin_ReturnsForbid()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);

            var result = controller.GetUsers();

            Assert.IsType<ActionResult<IEnumerable<UserViewModel>>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetUserDictionary_AsAdmin_ReturnsDictionaryOfUsersInCompany()
        {
            var controller = CreateDefaultController(ADMIN_EMAIL);

            var result = await controller.GetUserDictionary();
            var ok = (OkObjectResult)result.Result;
            var dictionary = (Dictionary<string, string>)ok.Value;

            Assert.IsType<ActionResult<Dictionary<string, string>>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<Dictionary<string, string>>(ok.Value);
            Assert.NotEmpty(dictionary);
            const int NUMBER_OF_USERS_IN_CONTOSO_COMPANY = 2;
            Assert.Equal(NUMBER_OF_USERS_IN_CONTOSO_COMPANY, dictionary.Count());
        }

        [Fact]
        public async Task GetUserDictionary_AsCarWashAdmin_ReturnsDictionaryOfAllUsers()
        {
            var controller = CreateDefaultController(CARWASH_ADMIN_EMAIL);

            var result = await controller.GetUserDictionary();
            var ok = (OkObjectResult)result.Result;
            var dictionary = (Dictionary<string, string>)ok.Value;

            Assert.IsType<ActionResult<Dictionary<string, string>>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<Dictionary<string, string>>(ok.Value);
            Assert.NotEmpty(dictionary);
            const int NUMBER_OF_ALL_USERS = 3;
            Assert.Equal(NUMBER_OF_ALL_USERS, dictionary.Count());
        }

        [Fact]
        public async Task GetUserDictionary_AsNotAdmin_ReturnsForbid()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);

            var result = await controller.GetUserDictionary();

            Assert.IsType<ActionResult<Dictionary<string, string>>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Fact]
        public async Task GetUser_AsAdmin_ReturnsUser()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var admin = dbContext.Users.Single(u => u.Email == ADMIN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(admin);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.GetUser(john.Id);
            var ok = (OkObjectResult)result.Result;
            var user = (UserViewModel)ok.Value;

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserViewModel>(ok.Value);
            Assert.Equal(john.Id, user.Id);
        }

        [Fact]
        public async Task GetUser_AsNotAdmin_ReturnsSelf()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.GetUser(john.Id);
            var ok = (OkObjectResult)result.Result;
            var user = (UserViewModel)ok.Value;

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.IsType<UserViewModel>(ok.Value);
            Assert.Equal(john.Id, user.Id);
        }

        [Fact]
        public async Task GetUser_AsNotAdmin_ReturnsForbid()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var admin = dbContext.Users.Single(u => u.Email == ADMIN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.GetUser(admin.Id);

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<ForbidResult>(result.Result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task PutSettings_SetCalendarIntegration_SetsCalendarIntegration(bool value)
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.PutSettings("calendarintegration", JsonSerializer.SerializeToElement(value));
            john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(value, john.CalendarIntegration);
        }

        [Theory]
        [InlineData(NotificationChannel.Disabled)]
        [InlineData(NotificationChannel.Email)]
        [InlineData(NotificationChannel.Push)]
        public async Task PutSettings_SetNotificationChannel_SetsNotificationChannel(NotificationChannel value)
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.PutSettings("notificationchannel", JsonSerializer.SerializeToElement((long)value));
            john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);

            Assert.IsType<NoContentResult>(result);
            Assert.Equal(value, john.NotificationChannel);
        }

        [Fact]
        public async Task PutSettings_WithWrongKey_ReturnsBadRequest()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);

            var result = await controller.PutSettings("fakekey", JsonSerializer.SerializeToElement("fakevalue"));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutSettings_WithNoValue_ReturnsBadRequest()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);

            var result = await controller.PutSettings("notificationchannel", JsonSerializer.SerializeToElement(""));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PutSettings_WithWrongValue_ReturnsBadRequest()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);
            const long NOT_EXISTENT_NOTIFICATION_CHANNEL = 24;

            var result = await controller.PutSettings("notificationchannel", JsonSerializer.SerializeToElement(NOT_EXISTENT_NOTIFICATION_CHANNEL));

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DownloadPersonalData_ByDefault_ReturnsPersonalDataObject()
        {
            var controller = CreateDefaultController(JOHN_EMAIL);

            var result = await controller.DownloadPersonalData();
            var ok = (OkObjectResult)result.Result;

            Assert.IsType<ActionResult<object>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task DeleteUser_ByDefault_DeletesUser()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == JOHN_EMAIL);
            var emailServiceStub = new Mock<IEmailService>();
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(john);
            var controller = new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);

            var result = await controller.DeleteUser(john.Id);
            var notExistentJohn = dbContext.Users.SingleOrDefault(u => u.Email == JOHN_EMAIL);

            Assert.IsType<ActionResult<UserViewModel>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            Assert.Null(notExistentJohn);
        }

        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-userscontroller");
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // Recreate database
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // Seed database
            dbContext.Users.Add(new User
            {
                Email = JOHN_EMAIL,
                FirstName = "John",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            });
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

            dbContext.SaveChanges();

            return dbContext;
        }

        private static UsersController CreateDefaultController(string userEmail)
        {
            var dbContext = CreateInMemoryDbContext();
            var emailServiceStub = new Mock<IEmailService>();

            var user = dbContext.Users.Single(u => u.Email == userEmail);
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(s => s.CurrentUser).Returns(user);

            return new UsersController(dbContext, userServiceStub.Object, emailServiceStub.Object);
        }
    }
}
