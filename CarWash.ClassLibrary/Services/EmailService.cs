using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class EmailService : IEmailService
    {
        private readonly QueueServiceClient _storage;

        /// <inheritdoc />
        public EmailService(CarWashConfiguration configuration)
        {
            // Parse the connection string and return a reference to the storage account.
            _storage = new QueueServiceClient(configuration.ConnectionStrings.StorageAccount);
        }

        /// <inheritdoc />
        public async Task Send(Email email, TimeSpan? delay = null)
        {
            if (email == null) throw new ArgumentNullException(nameof(email));

            // Retrieve a reference to a container.
            var queue = _storage.GetQueueClient("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = JsonConvert.SerializeObject(email);
            await queue.SendMessageAsync(message, visibilityTimeout: delay);
        }
    }
}
