using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using CarWash.ClassLibrary.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class EmailService(IOptionsMonitor<CarWashConfiguration> configuration) : IEmailService
    {
        private readonly QueueServiceClient _storage = new(configuration.CurrentValue.ConnectionStrings.StorageAccount);

        /// <inheritdoc />
        public async Task Send(Email email, TimeSpan? delay = null)
        {
            ArgumentNullException.ThrowIfNull(email);

            // Retrieve a reference to a container.
            var queue = _storage.GetQueueClient("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = JsonSerializer.Serialize(email);
            await queue.SendMessageAsync(message, visibilityTimeout: delay);
        }
    }
}
