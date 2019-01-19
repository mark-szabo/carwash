using CarWash.PWA.Services;
using Microsoft.Extensions.Configuration;
using System;
using Xunit;

namespace CarWash.PWA.Tests.ApiTests
{
    public class CalendarServiceTests
    {
        [Fact]
        public void Test1()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile("appsettings.Development.json", true)
                .AddEnvironmentVariables().Build();
            var calendarService = new CalendarService(configuration);
        }
    }
}
