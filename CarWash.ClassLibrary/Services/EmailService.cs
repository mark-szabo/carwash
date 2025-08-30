using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using CarWash.ClassLibrary.Models;
using System.Text.Json;

namespace CarWash.ClassLibrary.Services
{
    /// <inheritdoc />
    public class EmailService(QueueServiceClient queueServiceClient) : IEmailService
    {
        /// <inheritdoc />
        public async Task Send(Email email, TimeSpan? delay = null)
        {
            ArgumentNullException.ThrowIfNull(email);

            // Retrieve a reference to a container.
            var queue = queueServiceClient.GetQueueClient("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = JsonSerializer.Serialize(email, Constants.DefaultJsonSerializerOptions);
            await queue.SendMessageAsync(message, visibilityTimeout: delay);
        }
    }
}
