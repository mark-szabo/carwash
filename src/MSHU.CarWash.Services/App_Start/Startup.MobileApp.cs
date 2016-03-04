using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Web.Http;
using Microsoft.Azure.Mobile.Server;
using Microsoft.Azure.Mobile.Server.Authentication;
using Microsoft.Azure.Mobile.Server.Config;
using MSHU.CarWash.Services.Models;
using Owin;
using AutoMapper;
using MSHU.CarWash.Services.DataObjects;

namespace MSHU.CarWash.Services
{
    public partial class Startup
    {
        public static void ConfigureMobileApp(IAppBuilder app)
        {
            // Represents the configuration options for the service
            HttpConfiguration config = new HttpConfiguration();

            // For more information on Web API tracing, see http://go.microsoft.com/fwlink/?LinkId=620686 
            config.EnableSystemDiagnosticsTracing();

            // To enable individual features, you must call extension methods on the MobileAppConfiguration object 
            // before calling ApplyTo.

            // UseDefaultConfiguration() is equivalent to the following setup:
            // new MobileAppConfiguration()
            //     .AddMobileAppHomeController()             // from the Home package
            //     .MapApiControllers()
            //     .AddTables(                               // from the Tables package
            //         new MobileAppTableConfiguration()
            //             .MapTableControllers()
            //             .AddEntityFramework()             // from the Entity package
            //         )
            //     .AddPushNotifications()                   // from the Notifications package
            //     .MapLegacyCrossDomainController()         // from the CrossDomain package
            //     .ApplyTo(config);

            new MobileAppConfiguration()
                .UseDefaultConfiguration()
                .ApplyTo(config);

            // Use Entity Framework Code First to create database tables based on your DbContext
            Database.SetInitializer(new CarWashInitializer());

            // To prevent Entity Framework from modifying your database schema, use a null database initializer
            // Database.SetInitializer<CarWashContext>(null);

            MobileAppSettingsDictionary settings = config.GetMobileAppSettingsProvider().GetMobileAppSettings();

            if (string.IsNullOrEmpty(settings.HostName))
            {
                // This middleware is intended to be used locally for debugging. By default, HostName will
                // only have a value when running in an App Service application.
                app.UseAppServiceAuthentication(new AppServiceAuthenticationOptions
                {
                    SigningKey = ConfigurationManager.AppSettings["SigningKey"],
                    ValidAudiences = new[] { ConfigurationManager.AppSettings["ValidAudience"] },
                    ValidIssuers = new[] { ConfigurationManager.AppSettings["ValidIssuer"] },
                    TokenHandler = config.GetAppServiceTokenHandler()
                });
            }
            app.UseWebApi(config);
        }

        public static void ConfigureAutoMapper()
        {
            Mapper.CreateMap<Employee, EmployeeDto>()
                .ForMember(employeeDto => employeeDto.Id, mce => mce.MapFrom(employee => employee.EmployeeId));
        }
    }

    public class CarWashInitializer : CreateDatabaseIfNotExists<CarWashContext>
    {
        protected override void Seed(CarWashContext context)
        {
            //List<TodoItem> todoItems = new List<TodoItem>
            //{
            //    new TodoItem { Id = Guid.NewGuid().ToString(), Text = "First item", Complete = false },
            //    new TodoItem { Id = Guid.NewGuid().ToString(), Text = "Second item", Complete = false },
            //};

            //foreach (TodoItem todoItem in todoItems)
            //{
            //    context.Set<TodoItem>().Add(todoItem);
            //}

            base.Seed(context);
        }
    }
}

