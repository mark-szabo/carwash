using System;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class EmailService : IEmailService
    {
        private readonly CloudStorageAccount _storage;

        /// <inheritdoc />
        public EmailService(CarWashConfiguration configuration)
        {
            // Parse the connection string and return a reference to the storage account.
            _storage = CloudStorageAccount.Parse(configuration.ConnectionStrings.StorageAccount);
        }

        /// <inheritdoc />
        public async Task Send(Email email, TimeSpan? delay = null)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));

            // Create the queue client.
            var queueClient = _storage.CreateCloudQueueClient();

            // Retrieve a reference to a container.
            var queue = queueClient.GetQueueReference("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = new CloudQueueMessage(JsonConvert.SerializeObject(email));
            await queue.AddMessageAsync(message, initialVisibilityDelay: delay, timeToLive: null, options: null, operationContext: null);
        }
    }
}
