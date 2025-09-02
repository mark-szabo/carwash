using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarWash.ClassLibrary.Models;
using static CarWash.ClassLibrary.Services.KeyLockerService;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service for managing key lockers and their boxes.
    /// This service is responsible for generating boxes, opening boxes, and listening for box closure events.
    /// </summary>
    public interface IKeyLockerService
    {
        /// <summary>
        /// Lists the state of all lockers and their boxes, including reservation info if connected.
        /// </summary>
        /// <returns></returns>
        Task<List<KeyLockerStatusMessage>> ListBoxes();

        /// <summary>
        /// Generates a number of boxes in the database with the specified prefix.
        /// The boxes are assigned to the specified lockerId, building, and floor.
        /// If lockerId is null, a new GUID will be generated for it marking a brand new locker.
        /// </summary>
        /// <param name="namePrefix">Prefix for the friendly name of each box.</param>
        /// <param name="numberOfBoxes">Number of bexes to generate.</param>
        /// <param name="building">Building where the locker is located.</param>
        /// <param name="floor">Floor where the locker is located.</param>
        /// <param name="lockerId">Optional id of an existing locker to add boxes to.</param>
        /// <returns></returns>
        Task GenerateBoxesToLocker(string namePrefix, int numberOfBoxes, string building, string floor, string? lockerId = null);

        /// <summary>
        /// Opens a specific box in the locker by its unique ID.
        /// This method retrieves the box by its ID, validates its existence, and sends a command to open it.
        /// Optionally, it listens for the box closure event and executes a callback when the box is closed.
        /// </summary>
        /// <param name="boxId">The unique ID of the box to open.</param>
        /// <param name="userId">Optional ID of the user requesting the box to be opened.</param>
        /// <param name="onBoxClosedCallback">Optional callback to execute when the box is closed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<KeyLockerBox> OpenBoxByIdAsync(string boxId, string? userId = null, Func<string, Task>? onBoxClosedCallback = null);

        /// <summary>
        /// Opens a specific box in the locker by its locker id and serial number.
        /// This method sends a command to the IoT device to open the box and optionally listens for its closure.
        /// </summary>
        /// <param name="lockerId">The ID of the locker containing the box.</param>
        /// <param name="boxSerial">The serial number of the box to open.</param>
        /// <param name="userId">Optional ID of the user requesting the box to be opened.</param>
        /// <param name="onBoxClosedCallback">Optional callback to execute when the box is closed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<KeyLockerBox> OpenBoxBySerialAsync(string lockerId, int boxSerial, string? userId = null, Func<string, Task>? onBoxClosedCallback = null);

        /// <summary>
        /// Opens a random available box in the specified locker.
        /// This method retrieves a random box that is marked as available (empty) in the database,
        /// sends a command to open it, and optionally listens for the box closure event.
        /// </summary>
        /// <param name="lockerId">The ID of the locker containing the boxes.</param>
        /// <param name="userId">Optional ID of the user requesting the box to be opened.</param>
        /// <param name="onBoxClosedCallback">Optional callback to execute when the box is closed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task<KeyLockerBox> OpenRandomAvailableBoxAsync(string lockerId, string? userId = null, Func<string, Task>? onBoxClosedCallback = null);

        /// <summary>
        /// Frees up a box by setting its state to empty.
        /// </summary>
        /// <param name="boxId">The unique ID of the box to free up.</param>
        /// <param name="userId">Optional ID of the user requesting the box to be freed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task FreeUpBoxAsync(string boxId, string? userId = null);
    }
}