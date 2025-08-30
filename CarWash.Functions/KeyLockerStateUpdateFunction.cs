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
                var deviceMessage = JsonSerializer.Deserialize<KeyLockerDeviceMessage>(messageBody, Constants.DefaultJsonSerializerOptions);

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

                var disconnectedLockerBoxes = await context.KeyLockerBox
                    .Where(box => box.LastModifiedAt < tenMinutesAgo)
                    .ToListAsync();

                foreach (var box in disconnectedLockerBoxes)
                {
                    box.IsConnected = false;
                }

                await context.SaveChangesAsync();

                logger.LogInformation("Updated availability for {NumberOfDisconnectedBoxes} disconnected locker boxes at {Time}", disconnectedLockerBoxes.Count, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating locker availability.");
            }
        }
    }
}