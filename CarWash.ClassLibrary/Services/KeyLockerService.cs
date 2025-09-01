using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs.Consumer;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service for managing key lockers and their boxes.
    /// This service is responsible for generating boxes, opening boxes, and listening for box closure events.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="configuration"></param>
    /// <param name="iotHubClient"></param>
    /// <param name="telemetryClient"></param>
    public class KeyLockerService(ApplicationDbContext context, IOptionsMonitor<CarWashConfiguration> configuration, ServiceClient iotHubClient, TelemetryClient telemetryClient) : IKeyLockerService
    {
        /// <inheritdoc />
        public async Task<List<KeyLockerStatusMessage>> ListBoxes()
        {
            var boxes = await context.KeyLockerBox
                .AsNoTracking()
                .ToListAsync();

            var reservations = await context.Reservation
                .AsNoTracking()
                .Where(r => r.KeyLockerBoxId != null)
                .ToListAsync();

            var lockers = boxes
                .GroupBy(b => new { b.LockerId, b.Building, b.Floor })
                .Select(g => new KeyLockerStatusMessage
                (
                    g.Key.LockerId,
                    g.Key.Building,
                    g.Key.Floor,
                    g.Select(b =>
                    {
                        var reservation = reservations.FirstOrDefault(r => r.KeyLockerBoxId == b.Id);
                        return new KeyLockerBoxStatusMessage
                        (
                            b.Id,
                            b.BoxSerial,
                            b.Name,
                            b.State,
                            b.IsDoorClosed,
                            b.IsConnected,
                            b.LastModifiedAt,
                            b.LastActivity,
                            reservation == null ? null : new KeyLockerBoxReservationStatusMessage
                            (
                                reservation.Id,
                                reservation.UserId,
                                reservation.VehiclePlateNumber,
                                reservation.Location,
                                reservation.State,
                                reservation.Services,
                                reservation.Private,
                                reservation.Mpv,
                                reservation.StartDate,
                                reservation.EndDate,
                                reservation.Comments
                            )
                        );
                    }).ToList()
                ))
                .ToList();

            return lockers;
        }

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
        public async Task<KeyLockerBox> OpenBoxByIdAsync(string boxId, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            if (string.IsNullOrEmpty(boxId))
            {
                throw new ArgumentException("Box ID cannot be null or empty.", nameof(boxId));
            }

            var box = await context.KeyLockerBox.SingleOrDefaultAsync(b => b.Id == boxId)
                ?? throw new InvalidOperationException($"Box with ID {boxId} not found.");

            await OpenBoxAsync(box.LockerId, box.BoxSerial, userId, onBoxClosedCallback);

            return box;
        }

        /// <inheritdoc />
        public async Task<KeyLockerBox> OpenBoxBySerialAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            await OpenBoxAsync(lockerId, boxSerial, userId, onBoxClosedCallback);

            // Find the box by lockerId and boxSerial
            var box = await context.KeyLockerBox
                .SingleOrDefaultAsync(b => b.LockerId == lockerId && b.BoxSerial == boxSerial)
                ?? throw new InvalidOperationException($"Box with serial {boxSerial} not found in locker {lockerId}.");

            return box;
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


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public record KeyLockerStatusMessage(string LockerId, string Building, string Floor, List<KeyLockerBoxStatusMessage> Boxes);
        public record KeyLockerBoxStatusMessage(string BoxId, int BoxSerial, string Name, KeyLockerBoxState State, bool IsDoorClosed, bool IsConnected, DateTime LastModifiedAt, DateTime LastActivity, KeyLockerBoxReservationStatusMessage? Reservation);
        public record KeyLockerBoxReservationStatusMessage(string Id, string? UserId, string VehiclePlateNumber, string? Location, State State, List<int>? Services, bool Private, bool Mpv, DateTime StartDate, DateTime? EndDate, List<Comment>? Comments);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private async Task OpenBoxAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            KeyLockerBox? box = null;
            if (onBoxClosedCallback != null)
            {
                // Find the box by lockerId and boxSerial
                box = await context.KeyLockerBox
                    .SingleOrDefaultAsync(b => b.LockerId == lockerId && b.BoxSerial == boxSerial)
                    ?? throw new InvalidOperationException($"Box with serial {boxSerial} not found in locker {lockerId}.");

            }

            var methodInvocation = new CloudToDeviceMethod(configuration.CurrentValue.KeyLocker.BoxIotIdPrefix + (boxSerial - 1))
            {
                ResponseTimeout = TimeSpan.FromSeconds(30)
            };
            methodInvocation.SetPayloadJson("1");

            try
            {
                var response = await iotHubClient.InvokeDeviceMethodAsync(lockerId, methodInvocation);

                if (response.Status == 200)
                {
                    await UpdateBoxStateAsync(lockerId, boxSerial, KeyLockerBoxState.Used, userId);

                    telemetryClient.TrackEvent("KeyLockerBoxOpened", new Dictionary<string, string> {
                        { "LockerId", lockerId },
                        { "BoxSerial", boxSerial.ToString() },
                        { "UserId", userId ?? "" },
                    });

                    if (box != null) _ = ListenForBoxClosureAsync(box, userId, onBoxClosedCallback);
                }
                else
                {
                    telemetryClient.TrackException(new InvalidOperationException($"Failed to open box {boxSerial} in locker {lockerId}. Status: {response.Status}."), new Dictionary<string, string> {
                        { "LockerId", lockerId },
                        { "BoxSerial", boxSerial.ToString() },
                        { "UserId", userId ?? "" },
                    });

                    throw new InvalidOperationException($"Failed to open box {boxSerial} in locker {lockerId}.");
                }
            }
            catch (Microsoft.Azure.Devices.Common.Exceptions.IotHubException ex)
            {
                telemetryClient.TrackException(ex, new Dictionary<string, string> {
                    { "LockerId", lockerId },
                    { "BoxSerial", boxSerial.ToString() },
                    { "UserId", userId ?? "" },
                });

                throw new InvalidOperationException($"Failed to open box {boxSerial} in locker {lockerId}.", ex);
            }
        }

        private async Task ListenForBoxClosureAsync(KeyLockerBox box, string? userId = null, Func<string, Task>? onBoxClosedCallback = null)
        {
            var listeningStartTime = DateTime.UtcNow;

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
                    //Debug.WriteLine($"\nMessage received on partition {partitionEvent.Partition.PartitionId} enqued at {partitionEvent.Data.EnqueuedTime}.");
                    if (partitionEvent.Data.EnqueuedTime < listeningStartTime) continue;

                    string data = Encoding.UTF8.GetString(partitionEvent.Data.Body.ToArray());
                    //Debug.WriteLine($"\tMessage body: {data}");

                    var messageLockerId = partitionEvent.Data.SystemProperties["iothub-connection-device-id"]?.ToString();

                    if (messageLockerId == box.LockerId)
                    {
                        var states = ParseKeyLockerDeviceMessage(data);

                        if (states.Count <= (box.BoxSerial - 1))
                        {
                            throw new InvalidOperationException($"Box serial {box.BoxSerial} is out of range for the received message. Message contains {states.Count} boxes.");
                        }

                        telemetryClient.TrackEvent("IoTDeviceMessageProcessed", new Dictionary<string, string> {
                            { "PartitionId", partitionEvent.Partition.PartitionId},
                            { "EnqueuedTime", partitionEvent.Data.EnqueuedTime.ToString("o") },
                            { "MessageBody", data },
                            { "LockerId", messageLockerId },
                            { "BoxSerial", box.BoxSerial.ToString() },
                            { "BoxState", states[box.BoxSerial - 1].ToString() },
                        });

                        if (states[box.BoxSerial - 1])
                        {
                            telemetryClient.TrackEvent("KeyLockerBoxClosed", new Dictionary<string, string> {
                                { "PartitionId", partitionEvent.Partition.PartitionId},
                                { "EnqueuedTime", partitionEvent.Data.EnqueuedTime.ToString("o") },
                                { "MessageBody", data },
                                { "LockerId", messageLockerId },
                                { "BoxSerial", box.BoxSerial.ToString() },
                                { "UserId", userId ?? "" },
                            });

                            if (onBoxClosedCallback != null)
                            {
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
            int index = RandomNumberGenerator.GetInt32(availableBoxIds.Count);

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
            var deviceMessage = JsonSerializer.Deserialize<KeyLockerDeviceMessage>(message, Constants.DefaultJsonSerializerOptions)
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
