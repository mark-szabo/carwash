using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using MSHU.CarWash.ClassLibrary;

namespace MSHU.CarWash.PWA.Extensions
{
    public static class EmailExtension
    {
        public static async Task Send(this Email email, IConfiguration configuration)
        {
            if (email == null) return;

            // Parse the connection string and return a reference to the storage account.
            var storage = CloudStorageAccount.Parse(configuration.GetConnectionString("StorageAccount"));

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
