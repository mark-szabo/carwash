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
        /// List of bookable slots and their capacity.
        /// </summary>
        /// <remarks>
        /// Capacity is in number of cars per slot, not in minutes!
        /// Location: Application Settings
        /// Key: Slots
        /// </remarks>
        /// <example>
        /// [
        ///   {
        ///     "StartTime": 8,
        ///     "EndTime": 11,
        ///     "Capacity": 12
        ///   },
        ///   {
        ///     "StartTime": 11,
        ///     "EndTime": 14,
        ///     "Capacity": 12
        ///   },
        ///   {
        ///     "StartTime": 14,
        ///     "EndTime": 17,
        ///     "Capacity": 12
        ///   }
        /// ]
        /// </example>
        public List<Slot> Slots { get; set; } = new List<Slot>();

        /// <summary>
        /// List of companies whose users can use the CarWash app.
        /// </summary>
        /// <remarks>
        /// You MUST include the CarWash as a company!
        /// Location: Application Settings
        /// Key: Companies
        /// </remarks>
        /// <example>
        /// [
        ///   {
        ///     "Name": "carwash",
        ///     "TenantId": "86cf3673-fa54-46ac-8a6c-164633955f8e",
        ///     "DailyLimit": 0
        ///   },
        ///   {
        ///     "Name": "contoso",
        ///     "TenantId": "d31a1195-d2ed-4a04-820c-3e1e455e380b",
        ///     "DailyLimit": 15
        ///   }
        /// ]
        /// </example>
        public List<Company> Companies { get; set; } = new List<Company>();

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
        ///     "Floors": [ "-1", "-2", "-2.5", "-3", "-3.5" ]
        ///   },
        ///   {
        ///     "Building": "B",
        ///     "Floors": [ "-1", "-2", "-3", "outdoor" ]
        ///   },
        /// ]
        /// </example>
        public List<Garage> Garages { get; set; } = new List<Garage>();

        /// <summary>
        /// CarWash app settings referring to reservations.
        /// </summary>
        public ReservationSettings Reservation { get; set; } = new ReservationSettings();

        /// <summary>
        /// Connection strings to various dependencies.
        /// </summary>
        public ConnectionStringsConfiguration ConnectionStrings { get; set; } = new ConnectionStringsConfiguration();

        /// <summary>
        /// Azure Active Directory configuration (used for SSO user authentication).
        /// </summary>
        public AzureAdConfiguration AzureAd { get; set; } = new AzureAdConfiguration();

        /// <summary>
        /// VAPID configuration.
        /// </summary>
        public VapidConfiguration Vapid { get; set; } = new VapidConfiguration();

        /// <summary>
        /// Configuration for the Calendar Service (meeting request sender).
        /// </summary>
        public CalendarServiceConfiguration CalendarService { get; set; } = new CalendarServiceConfiguration();

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
            public int TimeUnit { get; set; } = 12;

            /// <summary>
            /// Number of concurrent active reservations permitted.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:UserConcurrentReservationLimit
            /// Default value: 2
            /// </remarks>
            public int UserConcurrentReservationLimit { get; set; } = 2;

            /// <summary>
            /// Number of minutes to allow reserving in past or in a slot after that slot has 
            /// already started.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:MinutesToAllowReserveInPast
            /// Default value: 120
            /// </remarks>
            public int MinutesToAllowReserveInPast { get; set; } = 120;

            /// <summary>
            /// Time of day in hours after reservations for the same day must not be validated 
            /// against company limit.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: Reservation:HoursAfterCompanyLimitIsNotChecked
            /// Default value: 11
            /// </remarks>
            public int HoursAfterCompanyLimitIsNotChecked { get; set; } = 11;
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
            /// Azure SQL database connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--SqlDatabase
            /// </remarks>
            public string SqlDatabase { get; set; }

            /// <summary>
            /// Azure Storage Account connection string.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: ConnectionStrings--StorageAccount
            /// </remarks>
            public string StorageAccount { get; set; }
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
            public string Instance { get; set; } = "https://login.microsoftonline.com/common/";

            /// <summary>
            /// Client id of the registered application in the tenant. Aka Application ID.
            /// </summary>
            /// <remarks>
            /// Location: Application Settings
            /// Key: AzureAd:ClientId
            /// </remarks>
            public string ClientId { get; set; }

            /// <summary>
            /// Azure Active Directory Client Secret.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: AzureAd--ClientSecret
            /// </remarks>
            public string ClientSecret { get; set; }

            /// <summary>
            /// List of ids of authorized service application which can use the CarWash app programmatically.
            /// </summary>
            public List<string> AuthorizedApplications { get; set; } = new List<string>();
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
            public string Subject { get; set; }

            /// <summary>
            /// VAPID public key.
            /// </summary>
            /// <remarks>
            /// The app will generate a public and prive key pair at startup if you don't specify 
            /// it, which you can use later.
            /// Location: Application Settings
            /// Key: Vapid:PublicKey
            /// </remarks>
            public string PublicKey { get; set; }

            /// <summary>
            /// VAPID private key.
            /// </summary>
            /// <remarks>
            /// Location: Azure Key Vault (DO NOT store secrets in Application Settings!)
            /// Key: Vapid--PrivateKey
            /// </remarks>
            public string PrivateKey { get; set; }
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
            public string LogicAppUrl { get; set; }
        }
    }
}
