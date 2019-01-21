using CarWash.ClassLibrary.Models;
using CarWash.PWA.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace CarWash.PWA.Tests
{
    public class UsersControllerTests
    {
        [Fact]
        public void GetCurrentUser_ByDefault_ReturnsCurrentUser()
        {
            var dbContext = CreateInMemoryDbContext();
            var john = dbContext.Users.Single(u => u.Email == "john.doe@test.com");
            var httpContextAccessorStub = new Mock<IHttpContextAccessor>();
            httpContextAccessorStub.SetupGet(s => s.HttpContext.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new List<Claim> { new Claim(ClaimTypes.Upn, john.Email) })));
            var controller = new UsersController(dbContext, httpContextAccessorStub.Object);
            
            var result = controller.GetCurrentUser();

            Assert.IsType<User>(result);
            Assert.Equal(john, result);
        }

        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseInMemoryDatabase("carwashu-test-userscontroller");
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

            dbContext.SaveChanges();

            return dbContext;
        }
    }
}
