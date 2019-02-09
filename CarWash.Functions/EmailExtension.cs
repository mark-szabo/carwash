using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using CarWash.ClassLibrary;
using CarWash.ClassLibrary.Models;
using Newtonsoft.Json;

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
            var connectionString = Environment.GetEnvironmentVariable("StorageAccount", EnvironmentVariableTarget.Process);

            // Parse the connection string and return a reference to the storage account.
            var storage = CloudStorageAccount.Parse(connectionString);

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
