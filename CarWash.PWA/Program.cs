#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Azure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace CarWash.PWA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, builder) =>
                {
                    var config = builder.Build();

                    var keyVaultBaseUri = new Uri(config.GetValue<string>("KeyVault:BaseUrl"));
                    var defaultAzureCredential = new DefaultAzureCredential();
                    builder.AddAzureKeyVault(keyVaultBaseUri, defaultAzureCredential);

                    // Load configuration from Azure App Configuration
                    var endpoint = config.GetValue<string>("AppConfig:Endpoint");
                    builder.AddAzureAppConfiguration(options =>
                    {
                        options.Connect(new Uri(endpoint), defaultAzureCredential)
                            // Configure to reload configuration if the registered sentinel key is modified
                            .ConfigureRefresh(refreshOptions =>
                                refreshOptions.Register("VERSION", refreshAll: true)
                                              .SetRefreshInterval(TimeSpan.FromMinutes(5)));

                        options.ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(defaultAzureCredential);
                        });
                    });
                })
                .ConfigureKestrel(c => c.AddServerHeader = false)
                .UseStartup<Startup>();
    }
}
