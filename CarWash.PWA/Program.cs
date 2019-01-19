#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;

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
                    var keyVaultBaseUrl = config.GetValue<string>("KeyVault:BaseUrl");
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var keyVaultClient = new KeyVaultClient(
                        new KeyVaultClient.AuthenticationCallback(
                            azureServiceTokenProvider.KeyVaultTokenCallback));
                    builder.AddAzureKeyVault(
                        keyVaultBaseUrl, keyVaultClient, new DefaultKeyVaultSecretManager());
                })
                .UseApplicationInsights()
                .UseKestrel(c => c.AddServerHeader = false)
                .UseStartup<Startup>();
    }
}
