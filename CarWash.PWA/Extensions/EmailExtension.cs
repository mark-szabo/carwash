using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using CarWash.ClassLibrary.Models;

namespace CarWash.PWA.Extensions
{
    /// <summary>
    /// Email sender service
    /// </summary>
    public static class EmailExtension
    {
        private static string _storageAccountConnectionString;

        /// <summary>
        /// Pass the Storage account connection string to the service during Startup
        /// </summary>
        /// <param name="serviceProvider">services</param>
        /// <param name="configuration">Configuration</param>
        /// <returns>services</returns>
        public static IServiceProvider ConfigureEmailProvider(this IServiceProvider serviceProvider, CarWashConfiguration configuration)
        {
            _storageAccountConnectionString = configuration.ConnectionStrings.StorageAccount;

            return serviceProvider;
        }

        /// <summary>
        /// Schedule an email to be sent by Azure Logic App
        /// </summary>
        /// <param name="email">Email object containing the email to be sent</param>
        /// <returns>void</returns>
        public static async Task Send(this Email email)
        {
            if (email == null) return;

            // Parse the connection string and return a reference to the storage account.
            var storage = CloudStorageAccount.Parse(_storageAccountConnectionString);

            // Create the queue client.
            var queueClient = storage.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            var queue = queueClient.GetQueueReference("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(email));
            await queue.AddMessageAsync(message);
        }
    }
}
