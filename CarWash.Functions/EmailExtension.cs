using System;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using CarWash.ClassLibrary.Models;
using System.Text.Json;

namespace CarWash.Functions
{
    /// <summary>
    /// Email sender service
    /// </summary>
    public static class EmailExtension
    {
        /// <summary>
        /// Schedule an email to be sent by Azure Logic App
        /// </summary>
        /// <param name="email">Email object containing the email to be sent</param>
        public static async Task Send(this Email email)
        {
            if (email == null) return;

            // Load connection string from appsettings
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings:StorageAccount", EnvironmentVariableTarget.Process);

            // Parse the connection string and return a reference to the storage account.
            var storage = new QueueServiceClient(connectionString);

            // Retrieve a reference to a container.
            var queue = storage.GetQueueClient("email");

            // Create the queue if it doesn't already exist
            await queue.CreateIfNotExistsAsync();

            // Create a message and add it to the queue.
            var message = JsonSerializer.Serialize(email);
            await queue.SendMessageAsync(message);
        }
    }
}
