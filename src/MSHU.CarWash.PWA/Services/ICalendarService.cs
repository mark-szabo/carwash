using System.Threading.Tasks;
using MSHU.CarWash.ClassLibrary;

namespace MSHU.CarWash.PWA.Services
{
    /// <summary>
    /// Defines a service to create, update and remove Outlook events using Microsoft Graph
    /// </summary>
    public interface ICalendarService
    {
        /// <summary>
        /// Create an Outlook event based on a reservation
        /// </summary>
        /// <param name="reservation">Reservation to get the details from</param>
        /// <returns>Outlook event id</returns>
        Task<string> CreateEventAsync(Reservation reservation);

        /// <summary>
        /// Update an Outlook event based on a reservation
        /// </summary>
        /// <param name="reservation">Reservation to get the details from</param>
        /// <returns>Outlook event id</returns>
        Task<string> UpdateEventAsync(Reservation reservation);

        /// <summary>
        /// Delete an Outlook event by id
        /// </summary>
        /// <param name="outlookEventId">Outlook event id</param>
        Task DeleteEventAsync(string outlookEventId);
    }
}
