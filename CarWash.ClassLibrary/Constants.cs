using CarWash.ClassLibrary.Models;
using System.Text.Json;

namespace CarWash.ClassLibrary
{
    /// <summary>
    /// Class containing constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Named <see cref="Service"/>s used for special business logic.
        /// </summary>
        public static class ServiceType
        {
            /// <summary>
            /// Exterior wash.
            /// </summary>
            public const int Exterior = 0;

            /// <summary>
            /// Interior cleaning.
            /// </summary>
            public const int Interior = 1;

            /// <summary>
            /// Carpet cleaning.
            /// </summary>
            public const int Carpet = 2;
        }

        /// <summary>
        /// SignalR hub method names for backlog-related events.
        /// </summary>
        /// <remarks>
        /// This class contains predefined method names that represent specific SignalR actions. These 
        /// constants can be used to ensure consistency when referencing method names in the hub communication.
        /// </remarks>
        public static class BacklogHubMethods
        {
            /// <summary>
            /// Method name for when a reservation is created.
            /// </summary>
            public const string ReservationCreated  = nameof(ReservationCreated);

            /// <summary>
            /// Method name for when a reservation is updated.
            /// </summary>
            public const string ReservationUpdated = nameof(ReservationUpdated);

            /// <summary>
            /// Method name for when a reservation is deleted.
            /// </summary>
            public const string ReservationDeleted = nameof(ReservationDeleted);

            /// <summary>
            /// Method name for when a reservation dropoff is confirmed.
            /// </summary>
            public const string ReservationDropoffConfirmed = nameof(ReservationDropoffConfirmed);

            /// <summary>
            /// Method name for when a reservation chat message is sent.
            /// </summary>
            public const string ReservationChatMessageSent = nameof(ReservationChatMessageSent);
        }

        /// <summary>
        /// SignalR hub method names for key locker-related events.
        /// </summary>
        /// <remarks>
        /// This class contains predefined method names that represent specific SignalR actions. These 
        /// constants can be used to ensure consistency when referencing method names in the hub communication.
        /// </remarks>
        public static class KeyLockerHubMethods
        {
            /// <summary>
            /// Method name for when a key locker box is opened.
            /// </summary>
            public const string KeyLockerBoxOpened = nameof(KeyLockerBoxOpened);

            /// <summary>
            /// Method name for when a key locker box is closed.
            /// </summary>
            public const string KeyLockerBoxClosed = nameof(KeyLockerBoxClosed);
        }

        /// <summary>
        /// Key locker box door states.
        /// </summary>
        public static class KeyLockerBoxDoorState
        {
            /// <summary>
            /// Door is closed.
            /// </summary>
            public const byte Closed = 1;

            /// <summary>
            /// Door is open.
            /// </summary>
            public const byte Open = 0;
        }

        /// <summary>
        /// The name of the Azure Service Bus queue used for the Key Locker service.
        /// </summary>
        public const string KeyLockerServiceBusQueueName = "sbq-carwash-keylocker";

        /// <summary>
        /// Default <see cref="JsonSerializerOptions"/> used for serialization.
        /// </summary>
        public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
