using System;
using System.Linq;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using CarWash.ClassLibrary.Services;
using CarWash.PWA.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class CompanyControllerTests
    {
        private static ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-companycontroller");
            optionsBuilder.EnableSensitiveDataLogging();
            var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            // Recreate database
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            // Seed database
            dbContext.Company.Add(new Company
            {
                Id = "test-company-1",
                Name = "Test Company 1",
                TenantId = "tenant-1",
                DailyLimit = 5,
                CreatedOn = DateTime.UtcNow.AddDays(-1),
                UpdatedOn = DateTime.UtcNow.AddDays(-1)
            });

            dbContext.SaveChanges();

            return dbContext;
        }

        private static CompanyController CreateControllerStub(ApplicationDbContext dbContext, out Mock<ICloudflareService> cloudflareServiceMock)
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
            return new CompanyController(dbContext, userServiceStub.Object, cloudflareServiceMock.Object);
        }

        [Fact]
        public async Task UpdateCompanyLimit_PurgesCloudflareCache()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, out var cloudflareServiceMock);
            var existingCompany = dbContext.Company.First();
            var newLimit = 10;

            // Act
            await controller.UpdateCompanyLimit(existingCompany.Id, newLimit);

            // Assert
            cloudflareServiceMock.Verify(m => m.PurgeConfigurationCacheAsync(), Times.Once());
        }

        [Fact]
        public async Task DeleteCompany_PurgesCloudflareCache()
        {
            // Arrange
            var dbContext = CreateInMemoryDbContext();
            var controller = CreateControllerStub(dbContext, out var cloudflareServiceMock);
            var existingCompany = dbContext.Company.First();

            // Act
            await controller.DeleteCompany(existingCompany.Id);

            // Assert
            cloudflareServiceMock.Verify(m => m.PurgeConfigurationCacheAsync(), Times.Once());
        }
    }
}