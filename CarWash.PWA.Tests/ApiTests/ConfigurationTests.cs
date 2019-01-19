using Microsoft.Extensions.Configuration;
using Xunit;

namespace CarWash.PWA.Tests.ApiTests
{
    public class ConfigurationTests
    {
        [Fact]
        public void Configuration_Contains_SQLConnectionString()
        {
            var configuration = GetConfiguration();
            var sqlConnectionString = configuration.GetConnectionString("Database");

            Assert.True(!string.IsNullOrWhiteSpace(sqlConnectionString), "SQL connection string wasn't found in 'appsettings.json'.");
        }

        [Fact]
        public void Configuration_Contains_StorageConnectionString()
        {
            var configuration = GetConfiguration();
            var storageConnectionString = configuration.GetConnectionString("StorageAccount");

            Assert.True(!string.IsNullOrWhiteSpace(storageConnectionString), "Storage Account connection string wasn't found in 'appsettings.json'.");
        }

        [Fact]
        public void Configuration_Contains_AzureAdConfigs()
        {
            var configuration = GetConfiguration();
            var azureAdInstance = configuration.GetValue<string>("AzureAd:Instance");
            var azureAdClientId = configuration.GetValue<string>("AzureAd:ClientId");

            Assert.True(!string.IsNullOrWhiteSpace(azureAdInstance), "AzureAD instance wasn't found in 'appsettings.json'.");
            Assert.True(!string.IsNullOrWhiteSpace(azureAdClientId), "AzureAD client id wasn't found in 'appsettings.json'.");
        }

        [Fact]
        public void Configuration_Contains_VapidConfigs()
        {
            var configuration = GetConfiguration();
            var vapidSubject = configuration.GetValue<string>("Vapid:Subject");
            var vapidPublicKey = configuration.GetValue<string>("Vapid:PublicKey");
            var vapidPrivateKey = configuration.GetValue<string>("Vapid:PrivateKey");

            Assert.True(!string.IsNullOrWhiteSpace(vapidSubject), "VAPID subject wasn't found in 'appsettings.json'.");
            Assert.True(!string.IsNullOrWhiteSpace(vapidPublicKey), "VAPID public key wasn't found in 'appsettings.json'.");
            Assert.True(!string.IsNullOrWhiteSpace(vapidPrivateKey), "VAPID private key wasn't found in 'appsettings.json'.");
        }

        [Fact]
        public void Configuration_Contains_CalendarServiceConfigs()
        {
            var configuration = GetConfiguration();
            var logicAppUrl = configuration.GetValue<string>("CalendarService:LogicAppUrl");

            Assert.True(!string.IsNullOrWhiteSpace(logicAppUrl), "CalendarService's Logic App URL wasn't found in 'appsettings.json'.");
        }

        public IConfiguration GetConfiguration() => new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .AddJsonFile("appsettings.Development.json", true)
            .AddEnvironmentVariables().Build();
    }
}
