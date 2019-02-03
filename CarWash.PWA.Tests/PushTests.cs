using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class PushTests
    {
        private const string JOHN_EMAIL = "john.doe@test.com";

        [Fact]
        public void GetVapidPublicKey_ByDefault_ReturnsVapidPublicKey()
        {
            var userControllerStub = new Mock<IUsersController>();
            var hostingEnvironmentStub = new Mock<IHostingEnvironment>();
            var pushServiceMock = new Mock<IPushService>();
            const string PUBLIC_KEY = "test public key";
            pushServiceMock.Setup(m => m.GetVapidPublicKey()).Returns(PUBLIC_KEY);
            var controller = new PushController(userControllerStub.Object, hostingEnvironmentStub.Object, pushServiceMock.Object);

            var result = controller.GetVapidPublicKey();

            Assert.IsType<ActionResult<string>>(result);
            Assert.IsType<OkObjectResult>(result.Result);
            var ok = (OkObjectResult)result.Result;
            Assert.IsType<string>(ok.Value);
            var publicKey = (string)ok.Value;
            Assert.Equal(PUBLIC_KEY, publicKey);
            pushServiceMock.Verify(m => m.GetVapidPublicKey(), Times.Once());
        }

        [Fact]
        public async Task Register_ByDefault_ReturnsNoContent()
        {
            var userControllerStub = CreateUserControllerStub();
            var hostingEnvironmentStub = new Mock<IHostingEnvironment>();
            var pushServiceMock = new Mock<IPushService>();
            pushServiceMock.Setup(m => m.Register(It.IsAny<PushSubscription>())).Returns(Task.CompletedTask);
            var controller = new PushController(userControllerStub.Object, hostingEnvironmentStub.Object, pushServiceMock.Object);
            var model = new PushSubscriptionViewModel
            {
                Subscription = new Subscription
                {
                    Endpoint = "https://db5p.notify.windows.com/w/?token=BQYAAACt1jdFc8Z7mH6fQOhqwzfh...",
                    ExpirationTime = 1551728506000,
                    Keys = new Keys
                    {
                        Auth = "_PhChqC4oDiArJ-DpkfNrQ",
                        P256Dh = "BNyBjPuc30Akf7lvT7OtZUimZOgI...",
                    },
                },
            };

            var result = await controller.Register(model);

            Assert.IsType<NoContentResult>(result);
            pushServiceMock.Verify(m => m.Register(It.IsAny<PushSubscription>()), Times.Once());
        }

        //[Fact]
        //public async Task Send_InDevelopment_ReturnsAccepted()
        //{
        //    var userControllerStub = CreateUserControllerStub();
        //    var hostingEnvironmentStub = new Mock<IHostingEnvironment>();
        //    hostingEnvironmentStub.Setup(s => s.IsDevelopment()).Returns(true);
        //    var pushServiceMock = new Mock<IPushService>();
        //    pushServiceMock.Setup(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>())).Returns(Task.CompletedTask);
        //    var controller = new PushController(userControllerStub.Object, hostingEnvironmentStub.Object, pushServiceMock.Object);
        //    var notification = new Notification
        //    {
        //        Body = "Test notification.",
        //    };

        //    var result = await controller.Send("user id", notification);

        //    Assert.IsType<NoContentResult>(result);
        //    pushServiceMock.Verify(m => m.Send(It.IsAny<string>(), It.IsAny<Notification>()), Times.Once());
        //}

        private static Mock<IUsersController> CreateUserControllerStub()
        {
            var john = new User
            {
                Email = JOHN_EMAIL,
                FirstName = "John",
                LastName = "Doe",
                Company = "contoso",
                IsAdmin = false,
                IsCarwashAdmin = false,
            };
            var userControllerStub = new Mock<IUsersController>();
            userControllerStub.Setup(s => s.GetCurrentUser()).Returns(john);

            return userControllerStub;
        }
    }
}
