using CarWash.ClassLibrary.Enums;
using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// CarWash app configuration.
    /// </summary>
    /// <remarks>
    /// DO NOT edit these configurations here, but in Application Settings
    /// (appsettings.Development.json / Azure App Service Application Settings)
    /// or Azure Key Vault respectively!
    /// </remarks>
    public class CarWashConfiguration
    {
        /// <summary>
        /// Time zone identifier for the car wash provider (e.g., "Europe/Budapest").
        /// Used to define slot schedules and handle timezone conversions.
        /// </summary>
        /// <remarks>
        /// Location: Application Settings
        /// Key: TimeZone
        /// Default value: "UTC"
        /// </remarks>
        /// <example>"Europe/Budapest"</example>
        public string TimeZone { get; init; } = "UTC";

        /// <summary>
        /// List of bookable slots and their capacity.
        /// </summary>
        /// <remarks>
        /// Capacity is in number of cars per slot, not in minutes!
        /// StartTime and EndTime are specified as time-of-day in the provider's time zone.
        /// Location: Application Settings
        /// Key: Slots
        /// </remarks>
        /// <example>
        /// [
        ///   {
        ///     "StartTime": "08:00:00",
        ///     "EndTime": "11:00:00",
        ///     "Capacity": 12
        ///   },
        ///   {
        ///     "StartTime": "11:00:00",
        ///     "EndTime": "14:00:00",
        ///     "Capacity": 12
        ///   },
        ///   {
        ///     "StartTime": "14:00:00",
        ///     "EndTime": "17:00:00",
        ///     "Capacity": 12
        ///   }
        /// ]
        /// </example>
        public List<Slot> Slots { get; init; } = [];

        /// <summary>
        /// List of parking garages where cars are allowed to be left.
        /// </summary>
        /// <remarks>
        /// Location: Application Settings
        /// Key: Garages
        /// </remarks>
        /// <example>
        /// [
        ///   {
        ///     "Building": "A",
        ///     "Floors": [ "-1", "-2", "-2.5", "-3", "-3.5" ],
        ///     "KeyLockerId": "LOCKER-A1"
        ///   },
        ///   {
        ///     "Building": "B",
        ///     "Floors": [ "-1", "-2", "-3", "outdoor" ],
        ///     "KeyLockerId": "LOCKER-B1"
        ///   },
        /// ]
        /// </example>
        public List<Garage> Garages { get; init; } = [];

        /// <summary>
        /// List of services provided by the CarWash.
        /// </summary>
        /// <remarks>
        /// Location: Application Settings
        /// Key: Services
        /// </remarks>
        /// <example>
        /// [
        ///   {
        ///     "id": 0,
        ///     "name": "exterior",
        ///     "group": "Basics",
        ///     "description": null,
        ///     "timeInMinutes": 12,
        ///     "price": 6311,
        ///     "priceMpv": 7889,
        ///     "hidden": false
        ///   },
        ///   {
        ///     "id": 1,
        ///     "name": "interior",
        ///     "group": "Basics",
        ///     "description": null,
        ///     "timeInMinutes": 12,
        ///     "price": 3610,
        ///     "priceMpv": 5406,
        ///     "hidden": false
        ///   }
        /// ]
        /// </example>
        public List<Service> Services { get; set; } = [];

        /// <summary>
        /// CarWash app settings referring to reservations.
        /// </summary>
        public ReservationSettings Reservation { get; init; } = new ReservationSettings();

        /// <summary>
        /// Connection strings to various dependencies.
        /// </summary>
        public ConnectionStringsConfiguration ConnectionStrings { get; init; } = new ConnectionStringsConfiguration();

        /// <summary>
        /// Azure Service Bus Queue names.
        /// </summary>
        public ServiceBusQueueConfiguration ServiceBusQueues { get; init; } = new ServiceBusQueueConfiguration();

        /// <summary>
        /// Azure Active Directory configuration (used for SSO user authentication).
        /// </summary>
        public AzureAdConfiguration AzureAd { get; init; } = new AzureAdConfiguration();

        /// <summary>
        /// VAPID configuration.
        /// </summary>
        public VapidConfiguration Vapid { get; init; } = new VapidConfiguration();

        /// <summary>
        /// Configuration for the Calendar Service (meeting request sender).
        /// </summary>
        public CalendarServiceConfiguration CalendarService { get; init; } = new CalendarServiceConfiguration();

        /// <summary>
        /// Configuration for the IoT Key Locker boxes.
        /// </summary>
        public KeyLockerConfiguration KeyLocker { get; init; } = new KeyLockerConfiguration();

        /// <summary>
        /// Generated build number during CI/CD.
        /// </summary>
        /// <remarks>
        /// Location: Application Settings
        /// Key: BuildNumber
        /// Default value: ""
        /// </remarks>
        public string BuildNumber { get; set; } = "";

        /// <summary>
        /// Application version number.
        /// </summary>
        /// <remarks>
        /// Location: Application Settings
        /// Key: Version
        /// Default value: ""
        /// </remarks>
        public string Version { get; init; } = "";

        /// <summary>
        /// CarWash app settings referring to reservations.
        /// </summary>
        /// <remarks>
        /// Change these settings by adding a key-value par to you Application Settings
        /// (appsettings.Development.json / Azure App Service Application Settings) with
        /// the keys specified in the setting's remarks.
        /// </remarks>
        public class ReservationSettings
        {
            /// <summary>
            /// Wash time unit in minutes.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:TimeUnit
            /// Default value: 12
            /// </remarks>
            public int TimeUnit { get; init; } = 12;

            /// <summary>
            /// Number of concurrent active reservations permitted.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:UserConcurrentReservationLimit
            /// Default value: 2
            /// </remarks>
            public int UserConcurrentReservationLimit { get; init; } = 2;

            /// <summary>
            /// Number of minutes to allow reserving in past or in a slot after that slot has 
            /// already started.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:MinutesToAllowReserveInPast
            /// Default value: 120
            /// </remarks>
            public int MinutesToAllowReserveInPast { get; init; } = 120;

            /// <summary>
            /// Time of day in hours after reservations for the same day must not be validated 
            /// against company limit.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:HoursAfterCompanyLimitIsNotChecked
            /// Default value: 11
            /// </remarks>
            public int HoursAfterCompanyLimitIsNotChecked { get; init; } = 11;

            /// <summary>
            /// Time requirement multiplier for carpet cleaning.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:CarpetCleaningMultiplier
            /// Default value: 2
            /// </remarks>
            public int CarpetCleaningMultiplier { get; init; } = 2;
        }

        /// <summary>
        /// Connection strings to various dependencies.
        /// </summary>
        /// <remarks>
        /// DO NOT store secrets in Application Settings! Use Azure Key Vault instead.
        /// </remarks>
        public class ConnectionStringsConfiguration
        {
            /// <summary>
            /// Base URL of the application.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ConnectionStrings:BaseUrl
            /// Default value: ""
            /// </remarks>
            public string BaseUrl { get; init; } = "";

            /// <summary>
            /// Azure Service Bus connection string for Bot communication.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--ServiceBus
            /// </remarks>
            public string? ServiceBus { get; init; }

            /// <summary>
            /// Azure Service Bus connection string for Key Locker messages.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--KeyLockerServiceBus
            /// </remarks>
            public string? KeyLockerServiceBus { get; init; }

            /// <summary>
            /// Azure SQL database connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--SqlDatabase
            /// Default value: ""
            /// </remarks>
            public string SqlDatabase { get; init; } = "";

            /// <summary>
            /// Azure Storage Account connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--StorageAccount
            /// Default value: ""
            /// </remarks>
            public string StorageAccount { get; init; } = "";

            /// <summary>
            /// Azure IoT Hub connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--IotHub
            /// </remarks>
            public string? IotHub { get; init; }

            /// <summary>
            /// Azure IoT Hub Event Hub compatible connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--IotEventHub
            /// </remarks>
            public string? IotEventHub { get; init; }

            /// <summary>
            /// Cloudflare API key for cache purging.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--CloudflareApiKey
            /// </remarks>
            public string? CloudflareApiKey { get; init; }

            /// <summary>
            /// Cloudflare Zone ID for cache purging.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--CloudflareZoneId
            /// </remarks>
            public string? CloudflareZoneId { get; init; }
        }

        /// <summary>
        /// Azure Service Bus Queue names.
        /// </summary>
        public class ServiceBusQueueConfiguration
        {
            /// <summary>
            /// Service Bus queue name for the chat bot's drop-off reminders.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ServiceBusQueues:BotDropoffReminderQueue
            /// </remarks>
            public string? BotDropoffReminderQueue { get; init; }

            /// <summary>
            /// Service Bus queue name for the chat bot's wash-started messages.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ServiceBusQueues:BotWashStartedQueue
            /// </remarks>
            public string? BotWashStartedQueue { get; init; }

            /// <summary>
            /// Service Bus queue name for the chat bot's wash-completed messages.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ServiceBusQueues:BotWashCompletedQueue
            /// </remarks>
            public string? BotWashCompletedQueue { get; init; }

            /// <summary>
            /// Service Bus queue name for the chat bot's carwash-comment-left messages.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ServiceBusQueues:BotCarWashCommentLeftQueue
            /// </remarks>
            public string? BotCarWashCommentLeftQueue { get; init; }

            /// <summary>
            /// Service Bus queue name for the chat bot's vehicle-arrived notifications.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: ServiceBusQueues:BotVehicleArrivedNotificationQueue
            /// </remarks>
            public string? BotVehicleArrivedNotificationQueue { get; init; }
        }

        /// <summary>
        /// Azure Active Directory configuration (used for SSO user authentication).
        /// </summary>
        public class AzureAdConfiguration
        {
            /// <summary>
            /// AzureAD instance used as a base url to redirect users to authenticate.
            /// </summary>
            /// <remarks>
            /// URL template: "https://login.microsoftonline.com/{tenantid}/"
            /// If you are using a multi-tenant application just use 'common' as the tenant id.
            /// Location: Application Settings
            /// Key: AzureAd:Instance
            /// Default value: "https://login.microsoftonline.com/common/"
            /// </remarks>
            public string Instance { get; init; } = "https://login.microsoftonline.com/common/";

            /// <summary>
            /// Client id of the registered application in the tenant. Aka Application ID.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: AzureAd:ClientId
            /// Default value: ""
            /// </remarks>
            public string ClientId { get; init; } = "";

            /// <summary>
            /// Azure Active Directory Client Secret.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: AzureAd--ClientSecret
            /// Default value: ""
            /// </remarks>
            public string ClientSecret { get; init; } = "";

            /// <summary>
            /// List of ids of authorized service application which can use the CarWash app programmatically.
            /// </summary>
            public List<string> AuthorizedApplications { get; init; } = [];
        }

        /// <summary>
        /// VAPID configuration.
        /// </summary>
        public class VapidConfiguration
        {
            /// <summary>
            /// VAPID subject. Can be an email address or the url of the app for example.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Vapid:Subject
            /// </remarks>
            public string? Subject { get; init; }

            /// <summary>
            /// VAPID public key.
            /// </summary>
            /// <remarks>
            /// The app will generate a public and prive key pair at startup if you don't specify 
            /// it, which you can use later.
            /// Location: Application Settings
            /// Key: Vapid:PublicKey
            /// </remarks>
            public string? PublicKey { get; init; }

            /// <summary>
            /// VAPID private key.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: Vapid--PrivateKey
            /// </remarks>
            public string? PrivateKey { get; init; }
        }

        /// <summary>
        /// Configuration for the Calendar Service (meeting request sender).
        /// </summary>
        public class CalendarServiceConfiguration
        {
            /// <summary>
            /// URL of the Logic App configured to send out the meeting requests.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: CalendarService:LogicAppUrl
            /// </remarks>
            public string? LogicAppUrl { get; init; }
        }

        /// <summary>
        /// Configuration for the IoT Key Locker boxes.
        /// </summary>
        public class KeyLockerConfiguration
        {
            /// <summary>
            /// Prefix for the IoT Hub Direct method name of each box.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: KeyLocker:BoxIotIdPrefix
            /// Default value: "Relay"
            /// </remarks>
            public string BoxIotIdPrefix { get; init; } = "Relay";
        }
    }
}
