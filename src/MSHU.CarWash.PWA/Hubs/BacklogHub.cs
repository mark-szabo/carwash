using Microsoft.AspNetCore.SignalR;

namespace MSHU.CarWash.PWA.Hubs
{
    /// <summary>
    /// SignalR Hub for Backlog
    /// </summary>
    public class BacklogHub : Hub
    {
        /// <summary>
        /// Method constants for Backlog events
        /// </summary>
        public class Methods
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            public const string ReservationCreated = "ReservationCreated";
            public const string ReservationUpdated = "ReservationUpdated";
            public const string ReservationDeleted = "ReservationDeleted";
            public const string ReservationDropoffConfirmed = "ReservationDropoffConfirmed";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
