using System;
using System.Collections.Generic;
using System.Linq;
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
    public class SystemMessagesControllerTests
    {
        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-systemmessagescontroller");
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // Recreate database
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // Seed database
            dbContext.SystemMessage.Add(new SystemMessage
            {
                Message = "Test Message 1",
                StartDateTime = DateTime.UtcNow.AddHours(-1),
                EndDateTime = DateTime.UtcNow.AddHours(1),
                Severity = Severity.Info
            });

            dbContext.SystemMessage.Add(new SystemMessage
            {
                Message = "Test Message 2",
                StartDateTime = DateTime.UtcNow.AddHours(-2),
                EndDateTime = DateTime.UtcNow.AddHours(-1),
                Severity = Severity.Warning
            });

            dbContext.SaveChanges();

            return dbContext;
        }

        private static SystemMessagesController CreateControllerStub(ApplicationDbContext dbContext)
        {
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(us => us.CurrentUser).Returns(new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Company = "Contoso",
                IsCarwashAdmin = true
            });
            var cloudflareServiceStub = new Mock<ICloudflareService>();
            return new SystemMessagesController(dbContext, userServiceStub.Object, cloudflareServiceStub.Object);
        }

        private static SystemMessagesController CreateControllerStub(ApplicationDbContext dbContext, out Mock<ICloudflareService> cloudflareServiceMock)
        {
            var userServiceStub = new Mock<IUserService>();
            userServiceStub.Setup(us => us.CurrentUser).Returns(new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Company = "Contoso",
                IsCarwashAdmin = true
            });
            cloudflareServiceMock = new Mock<ICloudflareService>();
            return new SystemMessagesController(dbContext, userServiceStub.Object, cloudflareServiceMock.Object);
        }

        [Fact]
        public async Task GetSystemMessages_ReturnsAllMessages()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);

            // Act
            var result = await controller.GetSystemMessages();

            // Assert
            Assert.IsType<ActionResult<IEnumerable<SystemMessage>>>(result);
            Assert.IsAssignableFrom<IEnumerable<SystemMessage>>(result.Value);
            var messages = result.Value;
            Assert.Equal(2, messages.Count());
        }

        [Fact]
        public async Task CreateSystemMessage_AddsMessage()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);
            var newMessage = new SystemMessage
            {
                Message = "New Test Message",
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Severity = Severity.Success
            };

            // Act
            var result = await controller.CreateSystemMessage(newMessage);

            // Assert
            Assert.IsType<ActionResult<SystemMessage>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetSystemMessages", createdAtActionResult.ActionName);
            Assert.IsType<SystemMessage>(createdAtActionResult.Value);
            var createdMessage = (SystemMessage)createdAtActionResult.Value;
            Assert.Equal(newMessage.Message, createdMessage.Message);
        }

        [Fact]
        public async Task CreateSystemMessage_PurgesCloudflareCache()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, out var cloudflareServiceMock);
            var newMessage = new SystemMessage
            {
                Message = "New Test Message",
                StartDateTime = DateTime.UtcNow,
                EndDateTime = DateTime.UtcNow.AddHours(2),
                Severity = Severity.Success
            };

            // Act
            await controller.CreateSystemMessage(newMessage);

            // Assert
            cloudflareServiceMock.Verify(m => m.PurgeConfigurationCacheAsync(), Times.Once());
        }

        [Fact]
        public async Task UpdateSystemMessage_UpdatesMessage()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);
            var existingMessage = dbContext.SystemMessage.First();
            existingMessage.Message = "Updated Test Message";

            // Act
            var result = await controller.UpdateSystemMessage(existingMessage.Id, existingMessage);

            // Assert
            Assert.IsType<NoContentResult>(result);
            var updatedMessage = dbContext.SystemMessage.Find(existingMessage.Id);
            Assert.Equal(existingMessage.Message, updatedMessage.Message);
        }

        [Fact]
        public async Task UpdateSystemMessage_PurgesCloudflareCache()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, out var cloudflareServiceMock);
            var existingMessage = dbContext.SystemMessage.First();
            existingMessage.Message = "Updated Test Message";

            // Act
            await controller.UpdateSystemMessage(existingMessage.Id, existingMessage);

            // Assert
            cloudflareServiceMock.Verify(m => m.PurgeConfigurationCacheAsync(), Times.Once());
        }

        [Fact]
        public async Task DeleteSystemMessage_RemovesMessage()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext);
            var existingMessage = dbContext.SystemMessage.First();

            // Act
            var result = await controller.DeleteSystemMessage(existingMessage.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
            Assert.Null(dbContext.SystemMessage.Find(existingMessage.Id));
        }

        [Fact]
        public async Task DeleteSystemMessage_PurgesCloudflareCache()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, out var cloudflareServiceMock);
            var existingMessage = dbContext.SystemMessage.First();

            // Act
            await controller.DeleteSystemMessage(existingMessage.Id);

            // Assert
            cloudflareServiceMock.Verify(m => m.PurgeConfigurationCacheAsync(), Times.Once());
        }
    }
}
