using System.Collections.Generic;

namespace CarWash.ClassLibrary.Models
{
    public class CarWashConfiguration
    {
        public List<Slot> Slots { get; set; } = new List<Slot>();
        public List<Company> Companies { get; set; } = new List<Company>();
        public ReservationSettings Reservation { get; set; }

        public ConnectionStringsConfiguration ConnectionStrings { get; set; }
        public AzureAdConfiguration AzureAd { get; set; }
        public VapidConfiguration Vapid { get; set; }
        public CalendarServiceConfiguration CalendarService { get; set; }

        public class ReservationSettings
        {
            /// <summary>
            /// Wash time unit in minutes
            /// </summary>
            public int TimeUnit { get; set; } = 12;
            
            /// <summary>
            /// Number of concurrent active reservations permitted
            /// </summary>
            public int UserConcurrentReservationLimit { get; set; } = 2;

            /// <summary>
            /// Number of minutes to allow reserving in past or in a slot after that slot has already started
            /// </summary>
            public int MinutesToAllowReserveInPast { get; set; } = 120;

            /// <summary>
            /// Time of day in hours after reservations for the same day must not be validated against company limit
            /// </summary>
            public int HoursAfterCompanyLimitIsNotChecked { get; set; } = 11;
        }

        public class ConnectionStringsConfiguration
        {
            public string SqlDatabase { get; set; }
            public string StorageAccount { get; set; }
        }

        public class AzureAdConfiguration
        {
            public string Instance { get; set; } = "https://login.microsoftonline.com/common/";
            public string ClientId { get; set; }
            public string ClientSecret { get; set; }
        }

        public class VapidConfiguration
        {
            public string Subject { get; set; }
            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }

        public class CalendarServiceConfiguration
        {
            public string LogicAppUrl { get; set; }
        }
    }
}
