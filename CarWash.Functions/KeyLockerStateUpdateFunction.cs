using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CarWash.ClassLibrary;
using CarWash.ClassLibrary.Enums;
using CarWash.ClassLibrary.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarWash.Functions
{
    public class KeyLockerStateUpdateFunction(ILogger<KeyLockerStateUpdateFunction> logger, FunctionsDbContext context)
    {
        [Function(nameof(ServiceBusListenerFunction))]
        public async Task ServiceBusListenerFunction([ServiceBusTrigger(Constants.KeyLockerServiceBusQueueName, Connection = "ConnectionStrings:KeyLockerServiceBus", AutoCompleteMessages = true)] ServiceBusReceivedMessage message)
        {
            try
            {
                // Extract locker ID from message properties
                if (!message.ApplicationProperties.TryGetValue("iothub-connection-device-id", out var lockerIdObj) || lockerIdObj is not string lockerId)
                {
                    throw new ArgumentException("Message does not contain a valid 'iothub-connection-device-id' property.");
                }

                // Parse the message body
                var messageBody = System.Text.Encoding.UTF8.GetString(message.Body);
                KeyLockerDeviceMessage? deviceMessage;
                try
                {
                    deviceMessage = JsonSerializer.Deserialize<KeyLockerDeviceMessage>(messageBody, Constants.DefaultJsonSerializerOptions);
                }
                catch (Exception ex) when (ex is FormatException || ex is JsonException)
                {
                    logger.LogError(ex, $"Failed to deserialize message body: {messageBody}");
                    throw;
                }

                if (deviceMessage == null)
                {
                    logger.LogError("Failed to deserialize message body.");
                    return;
                }

                logger.LogInformation("Received message for {LockerId}: {OriginalMessage} > {MessageBinary}\n{Visualization}", lockerId, messageBody, Convert.ToString(deviceMessage.Inputs, 2), deviceMessage.ToString());

                // Update database
                var lockerBoxes = await context.KeyLockerBox
                    .Where(box => box.LockerId == lockerId)
                    .ToListAsync();

                var boxStates = deviceMessage.GetBoxStates();

                foreach (var box in lockerBoxes)
                {
                    box.IsConnected = true;
                    box.LastActivity = DateTime.UtcNow;

                    if (boxStates.Count < box.BoxSerial)
                    {
                        logger.LogWarning("Box serial {BoxSerial} is out of range for locker {LockerId}.", box.BoxSerial, lockerId);
                        box.IsConnected = false;

                        continue;
                    }

                    if (box.IsDoorClosed != boxStates[box.BoxSerial - 1])
                    {
                        box.IsDoorClosed = boxStates[box.BoxSerial - 1];
                        box.LastModifiedAt = DateTime.UtcNow;
                    }
                }

                await context.SaveChangesAsync();

                logger.LogInformation("Successfully updated locker states for Locker ID: {LockerId}", lockerId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message.");
            }
        }

        [Function(nameof(KeyLockerAvailabilityChecker))]
        public async Task KeyLockerAvailabilityChecker([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
        {
            try
            {
                var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
                var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);

                var disconnectedLockerBoxes = await context.KeyLockerBox
                    .Where(box => box.LastActivity < tenMinutesAgo)
                    .ToListAsync();

                foreach (var box in disconnectedLockerBoxes)
                {
                    box.IsConnected = false;
                }

                // Free up boxes that are used, have no reservation, and were modified more than 5 minutes ago
                var usedBoxesToFree = await context.KeyLockerBox
                    .Where(box => box.State == KeyLockerBoxState.Used && box.LastModifiedAt < fiveMinutesAgo &&
                        !context.Reservation.Any(r => r.KeyLockerBoxId == box.Id))
                    .ToListAsync();

                foreach (var box in usedBoxesToFree)
                {
                    box.State = KeyLockerBoxState.Empty;
                }

                await context.SaveChangesAsync();

                logger.LogInformation("Updated availability for {NumberOfDisconnectedBoxes} disconnected locker boxes at {Time}", disconnectedLockerBoxes.Count, DateTime.UtcNow);
                logger.LogInformation("Freed up {NumberOfFreedBoxes} used locker boxes with no active reservation at {Time}", usedBoxesToFree.Count, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating locker availability.");
            }
        }
    }
}