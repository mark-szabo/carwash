using Azure.Messaging.EventHubs.Consumer;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.Azure.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service for managing key lockers and their boxes.
    /// This service is responsible for generating boxes, opening boxes, and listening for box closure events.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <param name="iotHubClient"></param>
    public class KeyLockerService(ApplicationDbContext context, IOptionsMonitor<CarWashConfiguration> configuration, ServiceClient iotHubClient) : IKeyLockerService
    {
        /// <inheritdoc />
        public async Task GenerateBoxesToLocker(string namePrefix, int numberOfBoxes, string building, string floor, string? lockerId = null)
        {
            var numberOfExistingBoxes = 0;

            if (lockerId == null)
            {
                lockerId = Guid.NewGuid().ToString();
            }
            else
            {
                numberOfExistingBoxes = (await context.KeyLockerBox
                    .Where(x => x.LockerId == lockerId)
                    .MaxAsync(x => (int?)x.BoxSerial)) ?? 0;
            }

            for (int i = numberOfExistingBoxes; i < numberOfBoxes; i++)
            {
                var box = new KeyLockerBox
                {
                    LockerId = lockerId,
                    Building = building,
                    Floor = floor,
                    Name = namePrefix + (i + 1),
                    BoxSerial = (i + 1),
                };
                await context.KeyLockerBox.AddAsync(box);
                await context.KeyLockerBoxHistory.AddAsync(new KeyLockerBoxHistory(box));
            }

            await context.SaveChangesAsync();
        }

        /// <inheritdoc />
        public async Task<KeyLockerBox> OpenRandomAvailableBoxAsync(string lockerId, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            var boxId = await GetRandomAvailableBoxSerialAsync(lockerId)
                ?? throw new InvalidOperationException("No available boxes.");

            await OpenBoxAsync(lockerId, boxId, userId, onBoxClosedCallback);

            var box = await context.KeyLockerBox
                .SingleOrDefaultAsync(b => b.LockerId == lockerId && b.BoxSerial == boxId)
                ?? throw new InvalidOperationException($"Box with serial {boxId} not found in locker {lockerId}.");

            return box;
        }

        /// <inheritdoc />
        public async Task OpenBoxByIdAsync(string boxId, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            if (string.IsNullOrEmpty(boxId))
            {
                throw new ArgumentException("Box ID cannot be null or empty.", nameof(boxId));
            }

            var box = await context.KeyLockerBox.SingleOrDefaultAsync(b => b.Id == boxId)
                ?? throw new InvalidOperationException($"Box with ID {boxId} not found.");

            await OpenBoxAsync(box.LockerId, box.BoxSerial, userId, onBoxClosedCallback);
        }

        /// <inheritdoc />
        public async Task OpenBoxBySerialAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            await OpenBoxAsync(lockerId, boxSerial, userId, onBoxClosedCallback);
        }

        /// <inheritdoc />
        public async Task FreeUpBoxAsync(string boxId, string? userId = null)
        {
            if (string.IsNullOrEmpty(boxId))
            {
                throw new ArgumentException("Box ID cannot be null or empty.", nameof(boxId));
            }

            await UpdateBoxStateAsync(boxId, KeyLockerBoxState.Empty, userId);
        }

        private async Task OpenBoxAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            var methodInvocation = new CloudToDeviceMethod(configuration.CurrentValue.KeyLocker.BoxIotIdPrefix + boxSerial)
            {
                ResponseTimeout = TimeSpan.FromSeconds(30)
            };
            methodInvocation.SetPayloadJson("1");

            var response = await iotHubClient.InvokeDeviceMethodAsync(lockerId, methodInvocation);

            if (response.Status == 200)
            {
                await UpdateBoxStateAsync(lockerId, boxSerial, KeyLockerBoxState.Used, userId);

                _ = ListenForBoxClosureAsync(lockerId, boxSerial, userId, onBoxClosedCallback);
            }
            else
            {
                throw new InvalidOperationException($"Failed to open box {boxSerial}. Status: {response.Status}");
            }
        }

        private async Task ListenForBoxClosureAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            // Create the consumer using the default consumer group using a direct connection to the service.
            await using var consumer = new EventHubConsumerClient(
                EventHubConsumerClient.DefaultConsumerGroupName,
                configuration.CurrentValue.ConnectionStrings.IotEventHub);

            try
            {
                // Begin reading events for all partitions, starting with the first event in each partition and waiting indefinitely for
                // events to become available. Reading can be canceled by breaking out of the loop when an event is processed or by
                // signaling the cancellation token.
                //
                // The "ReadEventsAsync" method on the consumer is a good starting point for consuming events for prototypes
                // and samples. For real-world production scenarios, it is strongly recommended that you consider using the
                // "EventProcessorClient" from the "Azure.Messaging.EventHubs.Processor" package.
                //
                // More information on the "EventProcessorClient" and its benefits can be found here:
                //   https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/README.md
                await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync())
                {
                    Console.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId}:");

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    Console.WriteLine($"\tMessage body: {data}");

                    var messageLockerId = partitionEvent.Data.Properties["iothub-connection-device-id"]?.ToString();

                    if (messageLockerId == lockerId)
                    {
                        var states = ParseKeyLockerDeviceMessage(data);

                        if (states.Count <= (boxSerial - 1))
                        {
                            throw new InvalidOperationException($"Box serial {boxSerial} is out of range for the received message. Message contains {states.Count} boxes.");
                        }

                        if (states[boxSerial - 1])
                        {
                            Console.WriteLine($"Box {boxSerial} has been closed by the user.");

                            if (onBoxClosedCallback != null)
                            {
                                var box = context.KeyLockerBox
                                    .SingleOrDefault(b => b.LockerId == lockerId && b.BoxSerial == boxSerial)
                                    ?? throw new InvalidOperationException($"Box with serial {boxSerial} not found in locker {lockerId}.");

                                await onBoxClosedCallback(box.Id);
                            }

                            // Break out of the loop to stop listening for further events.
                            break;
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // This is expected when the token is signaled; it should not be considered an
                // error in this scenario.
            }
        }

        private async Task<int?> GetRandomAvailableBoxSerialAsync(string lockerId)
        {
            var availableBoxIds = await context.KeyLockerBox
                .Where(box => box.LockerId == lockerId && box.State == KeyLockerBoxState.Empty)
                .Select(box => box.BoxSerial)
                .ToListAsync();

            if (availableBoxIds == null || availableBoxIds.Count == 0)
            {
                return null;
            }

            // Randomly select one available box ID
            int index = new Random().Next(availableBoxIds.Count);

            return availableBoxIds[index];

        }

        private async Task UpdateBoxStateAsync(string boxId, KeyLockerBoxState newState, string? modifiedById = null)
        {
            var box = await context.KeyLockerBox.SingleOrDefaultAsync(b => b.Id == boxId)
                ?? throw new InvalidOperationException($"Box with ID {boxId} not found.");

            box.State = newState;
            box.LastModifiedAt = DateTime.UtcNow;
            box.LastModifiedBy = modifiedById;

            await context.KeyLockerBoxHistory.AddAsync(new KeyLockerBoxHistory(box));

            await context.SaveChangesAsync();
        }

        private async Task UpdateBoxStateAsync(string lockerId, int boxSerial, KeyLockerBoxState newState, string? modifiedById = null)
        {
            var box = await context.KeyLockerBox
                .SingleOrDefaultAsync(box => box.LockerId == lockerId && box.BoxSerial == boxSerial)
                ?? throw new InvalidOperationException($"Box with serial {boxSerial} not found in locker {lockerId}.");

            box.State = newState;
            box.LastModifiedAt = DateTime.UtcNow;
            box.LastModifiedBy = modifiedById;

            await context.KeyLockerBoxHistory.AddAsync(new KeyLockerBoxHistory(box));

            await context.SaveChangesAsync();
        }

        private static List<bool> ParseKeyLockerDeviceMessage(string message)
        {
            var deviceMessage = JsonSerializer.Deserialize<KeyLockerDeviceMessage>(message)
                ?? throw new InvalidOperationException("Failed to parse key locker device message.");

            // Get the box states from the device message
            var boxStates = deviceMessage.GetBoxStates();
            if (boxStates == null || boxStates.Count == 0)
            {
                throw new InvalidOperationException("No box states found in the device message.");
            }

            return boxStates;
        }
    }
}
