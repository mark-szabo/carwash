using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using CarWash.PWA.Hubs;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class KeyLockerTests
    {
        [Theory]
        [InlineData(0b00000000, new bool[] { false, false, false, false, false, false, false, false })]
        [InlineData(0b00000001, new bool[] { true, false, false, false, false, false, false, false })]
        [InlineData(0b11111111, new bool[] { true, true, true, true, true, true, true, true })]
        [InlineData(0b00001111, new bool[] { true, true, true, true, false, false, false, false })]
        [InlineData(0b0000000100000000, new bool[] { false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false })]
        [InlineData(0b10001000, new bool[] { false, false, false, true, false, false, false, true })]
        [InlineData(0b1000100000000000, new bool[] { false, false, false, false, false, false, false, false, false, false, false, true, false, false, false, true })]
        public void GetBoxStates_ReturnsExpectedStates(int inputs, bool[] expectedStates)
        {
            // Arrange
            var message = new KeyLockerDeviceMessage
            {
                // Use reflection to set internal class property for testing
                Inputs = inputs
            };

            // Act
            var states = message.GetBoxStates();

            // Assert
            Assert.Equal(expectedStates.Length, states.Count);
            Assert.Equal(expectedStates, states);
        }

        // --- KeyLockerService Tests ---
        [Fact]
        public async Task GenerateBoxesToLocker_CreatesBoxesAndHistory()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act
            await service.GenerateBoxesToLocker("Box", 2, "Bldg", "1");

            // Assert
            Assert.Equal(2, context.KeyLockerBox.Count());
            Assert.Equal(2, context.KeyLockerBoxHistory.Count());
        }

        [Fact]
        public async Task FreeUpBoxAsync_UpdatesState()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);
            await service.GenerateBoxesToLocker("Box", 1, "Bldg", "1");
            var boxId = context.KeyLockerBox.First().Id;
            context.KeyLockerBox.First().State = KeyLockerBoxState.Used;

            // Act
            await service.FreeUpBoxAsync(boxId);

            // Assert
            Assert.Equal(KeyLockerBoxState.Empty, context.KeyLockerBox.First().State);
        }

        [Fact]
        public async Task OpenBoxBySerialAsync_ReturnsBox_WhenBoxExists()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);
            await service.GenerateBoxesToLocker("Box", 1, "Bldg", "1", "locker1");
            context.KeyLockerBox.First().State = KeyLockerBoxState.Empty;

            // Act
            var result = await service.OpenBoxBySerialAsync("locker1", 1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.BoxSerial);
        }

        [Fact]
        public async Task FreeUpBoxAsync_Throws_IfBoxIdIsNull()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.FreeUpBoxAsync(null));
        }

        [Fact]
        public async Task FreeUpBoxAsync_UpdatesHistory()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);
            await service.GenerateBoxesToLocker("Box", 1, "Bldg", "1");
            var boxId = context.KeyLockerBox.First().Id;
            context.KeyLockerBox.First().State = KeyLockerBoxState.Used;

            // Act
            await service.FreeUpBoxAsync(boxId);

            // Assert
            Assert.Equal(KeyLockerBoxState.Empty, context.KeyLockerBox.First().State);
            Assert.True(context.KeyLockerBoxHistory.Any());
        }

        [Fact]
        public async Task OpenRandomAvailableBoxAsync_ReturnsBox_WhenAvailable()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);
            await service.GenerateBoxesToLocker("Box", 2, "Bldg", "1", "locker1");
            // Set both boxes to Empty
            foreach (var box in context.KeyLockerBox) box.State = KeyLockerBoxState.Empty;
            await context.SaveChangesAsync();

            // Act
            var result = await service.OpenRandomAvailableBoxAsync("locker1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("locker1", result.LockerId);
        }

        [Fact]
        public async Task OpenRandomAvailableBoxAsync_Throws_WhenNoneAvailable()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);
            await service.GenerateBoxesToLocker("Box", 1, "Bldg", "1", "locker1");
            context.KeyLockerBox.First().State = KeyLockerBoxState.Used;

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenRandomAvailableBoxAsync("locker1"));
        }

        [Fact]
        public async Task OpenBoxByIdAsync_Throws_WhenBoxIdNull()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act
            await Assert.ThrowsAsync<ArgumentException>(() => service.OpenBoxByIdAsync(null));
        }

        [Fact]
        public async Task OpenBoxByIdAsync_Throws_WhenBoxNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenBoxByIdAsync("notfound"));
        }

        [Fact]
        public async Task OpenBoxBySerialAsync_Throws_WhenBoxNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenBoxBySerialAsync("locker1", 99));
        }

        [Fact]
        public async Task UpdateBoxStateAsync_Throws_WhenBoxIdNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var iotHubClient = new Mock<Microsoft.Azure.Devices.ServiceClient>().Object;
            var service = new KeyLockerService(context, config, iotHubClient);

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.FreeUpBoxAsync("notfound"));
        }

        // --- KeyLockerController Tests ---
        [Fact]
        public async Task GenerateBoxes_ReturnsNoContent_ForValidRequest()
        {
            // Arrange
            var controller = TestControllerFactory.CreateKeyLockerController(isAdmin: true);
            var request = new GenerateBoxesRequest
            {
                NamePrefix = "Box",
                NumberOfBoxes = 2,
                Building = "Bldg",
                Floor = "1"
            };

            // Act
            var result = await controller.GenerateBoxes(request);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task GenerateBoxes_ReturnsForbid_ForNonAdmin()
        {
            // Arrange
            var controller = TestControllerFactory.CreateKeyLockerController(isAdmin: false);
            var request = new GenerateBoxesRequest
            {
                NamePrefix = "Box",
                NumberOfBoxes = 2,
                Building = "Bldg",
                Floor = "1"
            };

            // Act
            var result = await controller.GenerateBoxes(request);

            // Assert
            Assert.IsType<ForbidResult>(result);
        }

        [Fact]
        public async Task OpenBoxById_ReturnsBadRequest_ForNullId()
        {
            // Arrange
            var ctrl = TestControllerFactory.CreateKeyLockerController(true);

            // Act
            var result = await ctrl.OpenBoxById(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task OpenBoxById_ReturnsForbid_ForNonAdmin()
        {
            // Arrange
            var ctrl = TestControllerFactory.CreateKeyLockerController(false);

            // Act
            var result = await ctrl.OpenBoxById("someid");

            // Assert
            Assert.IsType<ForbidResult>(result);
        }
        [Fact]
        public async Task OpenBoxById_ReturnsNoContent_ForAdminAndValidId()
        {
            // Arrange
            var controller = TestControllerFactory.CreateKeyLockerController(true);
            var context = TestDbContextFactory.Create();
            var service = new KeyLockerService(context, TestOptionsMonitorFactory.Create(), new Mock<Microsoft.Azure.Devices.ServiceClient>().Object);
            await service.GenerateBoxesToLocker("Box", 1, "Bldg", "1");
            var boxId = context.KeyLockerBox.First().Id;
            var keyLockerServiceMock = new Mock<IKeyLockerService>();
            keyLockerServiceMock.Setup(x => x.OpenBoxByIdAsync(boxId, "user1", null)).ReturnsAsync(context.KeyLockerBox.First());
            var ctrl = TestControllerFactory.CreateKeyLockerController(true, context, keyLockerServiceMock.Object);

            // Act
            var result = await ctrl.OpenBoxById(boxId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task OpenBoxByReservationId_ReturnsNoContent_ForValidRequest()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var keyLockerServiceMock = new Mock<IKeyLockerService>();
            var reservation = new Reservation
            {
                Id = "res1",
                KeyLockerBoxId = "box1",
                UserId = "user1",
                VehiclePlateNumber = "ABC123",
                User = new User { Id = "user1", Email = "john@example.com", Company = "TestCo", FirstName = "Test" }
            };
            context.Reservation.Add(reservation);
            await context.SaveChangesAsync();
            keyLockerServiceMock.Setup(x => x.OpenBoxByIdAsync("box1", "user1", It.IsAny<Func<string, Task>>())).ReturnsAsync(new KeyLockerBox
            {
                Id = "box1",
                LockerId = "locker1",
                BoxSerial = 1,
                Building = "Bldg",
                Floor = "1",
                Name = "Box1"
            });
            var ctrl = TestControllerFactory.CreateKeyLockerController(true, context, keyLockerServiceMock.Object);

            // Act
            var result = await ctrl.OpenBoxByReservationId("res1");

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task OpenBoxByReservationId_ReturnsBadRequest_IfNoBoxId()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var reservation = new Reservation
            {
                Id = "res2",
                UserId = "user1",
                VehiclePlateNumber = "ABC123",
                User = new User { Id = "user1", Email = "john@example.com", Company = "TestCo", FirstName = "Test" }
            };
            context.Reservation.Add(reservation);
            await context.SaveChangesAsync();
            var ctrl = TestControllerFactory.CreateKeyLockerController(true, context);

            // Act
            var result = await ctrl.OpenBoxByReservationId("res2");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task PickUpByReservationId_ReturnsNoContent_ForValidRequest()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var keyLockerServiceMock = new Mock<IKeyLockerService>();
            var reservation = new Reservation
            {
                Id = "res3",
                KeyLockerBoxId = "box2",
                UserId = "user1",
                State = State.DropoffAndLocationConfirmed,
                VehiclePlateNumber = "ABC123",
                User = new User { Id = "user1", Email = "john@example.com", Company = "TestCo", FirstName = "Test" }
            };
            context.Reservation.Add(reservation);
            await context.SaveChangesAsync();
            keyLockerServiceMock.Setup(x => x.OpenBoxByIdAsync("box2", "user1", It.IsAny<Func<string, Task>>())).ReturnsAsync(new KeyLockerBox
            {
                Id = "box2",
                LockerId = "locker1",
                BoxSerial = 2,
                Building = "Bldg",
                Floor = "1",
                Name = "Box2"
            });
            keyLockerServiceMock.Setup(x => x.FreeUpBoxAsync("box2", "user1")).Returns(Task.CompletedTask);
            var ctrl = TestControllerFactory.CreateKeyLockerController(true, context, keyLockerServiceMock.Object);

            // Act
            var result = await ctrl.PickUpByReservationId("res3", true);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Reservation.First().KeyLockerBoxId);
            Assert.Equal(State.ReminderSentWaitingForKey, context.Reservation.First().State);
        }

        [Fact]
        public async Task FreeUpBoxByReservationId_ReturnsNoContent_ForValidRequest()
        {
            // Arrange
            var context = TestDbContextFactory.Create();
            var config = TestOptionsMonitorFactory.Create();
            var keyLockerServiceMock = new Mock<IKeyLockerService>();
            var reservation = new Reservation
            {
                Id = "res4",
                KeyLockerBoxId = "box3",
                UserId = "user1",
                VehiclePlateNumber = "ABC123"
            };
            context.Reservation.Add(reservation);
            await context.SaveChangesAsync();
            keyLockerServiceMock.Setup(x => x.FreeUpBoxAsync("box3", "user1")).Returns(Task.CompletedTask);
            var ctrl = TestControllerFactory.CreateKeyLockerController(true, context, keyLockerServiceMock.Object);

            // Act
            var result = await ctrl.FreeUpBoxByReservationId("res4");

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(context.Reservation.First().KeyLockerBoxId);
        }
    }

    // --- Test helpers ---
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }
    }

    public static class TestOptionsMonitorFactory
    {
        public static IOptionsMonitor<CarWashConfiguration> Create()
        {
            var config = new CarWashConfiguration();
            var mock = new Mock<IOptionsMonitor<CarWashConfiguration>>();
            mock.Setup(x => x.CurrentValue).Returns(config);
            return mock.Object;
        }
    }

    public static class TestControllerFactory
    {
        public static KeyLockerController CreateKeyLockerController(bool isAdmin, ApplicationDbContext context = null, IKeyLockerService keyLockerService = null)
        {
            var config = TestOptionsMonitorFactory.Create();
            if (context == null) context = TestDbContextFactory.Create();
            var userService = new Mock<IUserService>();
            userService.Setup(x => x.CurrentUser).Returns(new User
            {
                Id = "user1",
                IsCarwashAdmin = isAdmin,
                FirstName = "Test",
                Company = "TestCo"
            });
            if (keyLockerService == null)
            {
                var keyLockerServiceMock = new Mock<IKeyLockerService>();
                keyLockerServiceMock.Setup(x => x.GenerateBoxesToLocker(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
                keyLockerService = keyLockerServiceMock.Object;
            }
            var keyLockerHub = CreateHubContextStub();
            return new KeyLockerController(config, context, userService.Object, keyLockerService, keyLockerHub, CreateTelemetryClientStub());
        }

        private static IHubContext<KeyLockerHub> CreateHubContextStub()
        {
            var clientProxyMock = new Mock<IClientProxy>();
            var hubContextStub = new Mock<IHubContext<KeyLockerHub>>();
            hubContextStub.Setup(h => h.Clients.All).Returns(clientProxyMock.Object);
            hubContextStub.Setup(h => h.Clients.User(It.IsAny<string>())).Returns(clientProxyMock.Object);

            return hubContextStub.Object;
        }

        private static TelemetryClient CreateTelemetryClientStub()
        {
            TelemetryConfiguration configuration = new TelemetryConfiguration
            {
                TelemetryChannel = new Mock<ITelemetryChannel>().Object,
                ConnectionString = "InstrumentationKey=00000000-0000-0000-0000-000000000000"
            };
            configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            return new TelemetryClient(configuration);
        }
    }
}
