using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CarWash.PWA.Hubs
{
    /// <summary>
    /// SignalR Hub for Backlog
    /// </summary>
    public class BacklogHub : Hub
    {
        /// <summary>
        /// Broadcast ReservationCreated message to all devices except caller's
        /// </summary>
        /// <param name="id">reservation id</param>
        public async Task ReservationCreated(string id)
        {
            await Clients.Others.SendAsync(nameof(ReservationCreated), id);
        }

        /// <summary>
        /// Broadcast ReservationUpdated message to all devices except caller's
        /// </summary>
        /// <param name="id">reservation id</param>
        public async Task ReservationUpdated(string id)
        {
            await Clients.Others.SendAsync(nameof(ReservationUpdated), id);
        }

        /// <summary>
        /// Broadcast ReservationDeleted message to all devices except caller's
        /// </summary>
        /// <param name="id">reservation id</param>
        public async Task ReservationDeleted(string id)
        {
            await Clients.Others.SendAsync(nameof(ReservationDeleted), id);
        }

        /// <summary>
        /// Broadcast ReservationDropoffConfirmed message to all devices except caller's
        /// </summary>
        /// <param name="id">reservation id</param>
        public async Task ReservationDropoffConfirmed(string id)
        {
            await Clients.Others.SendAsync(nameof(ReservationDropoffConfirmed), id);
        }

        /// <summary>
        /// Broadcast ReservationChatMessageSent message to all devices except caller's
        /// </summary>
        /// <param name="id">reservation id</param>
        public async Task ReservationChatMessageSent(string id)
        {
            await Clients.Others.SendAsync(nameof(ReservationChatMessageSent), id);
        }
    }
}
