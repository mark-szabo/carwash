using System.Collections.Generic;
using System.Text.Json.Serialization;
using static CarWash.ClassLibrary.Enums.ServiceType;

namespace CarWash.ClassLibrary.Enums
{
#pragma warning disable CS1591, SA1602
    /// <summary>
    /// Types of services the users can choose from.
    /// </summary>
    public enum ServiceType
    {
        Exterior = 0,
        Interior = 1,
        Carpet = 2,
        SpotCleaning = 3,
        VignetteRemoval = 4,
        Polishing = 5,
        AcCleaningOzon = 6,
        AcCleaningBomba = 7,

        // below are those services that are hidden from the user
        BugRemoval = 8,
        WheelCleaning = 9,
        TireCare = 10,
        LeatherCare = 11,
        PlasticCare = 12,
        PreWash = 13
    }
#pragma warning restore CS1591, SA1602

    /// <summary>
    /// Extension class for ServiceType.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Converts the ServiceType enum to a display-friendly string.
        /// </summary>
        /// <param name="serviceType">The service type to convert.</param>
        /// <returns>A display-friendly string.</returns>
        public static string ToFriendlyString(this ServiceType serviceType)
        {
            switch (serviceType)
            {
                case Exterior:
                    return "exterior";
                case Interior:
                    return "interior";
                case Carpet:
                    return "carpet";
                case SpotCleaning:
                    return "spot cleaning";
                case VignetteRemoval:
                    return "vignette removal";
                case Polishing:
                    return "polishing";
                case AcCleaningOzon:
                    return "AC cleaning 'ozon'";
                case AcCleaningBomba:
                    return "AC cleaning 'bomba'";
                case BugRemoval:
                    return "bug removal";
                case WheelCleaning:
                    return "wheel cleaning";
                case TireCare:
                    return "tire care";
                case LeatherCare:
                    return "leather care";
                case PlasticCare:
                    return "plastic care";
                case PreWash:
                    return "prewash";
                default:
                    return "no info";
            }
        }
    }

    /// <summary>
    /// Services the users can choose from.
    /// </summary>
    public static class ServiceTypes
    {
        /// <summary>
        /// Services the users can choose from.
        /// </summary>
        public static readonly List<Service> Types = new List<Service>
        {
            new Service {
                Type = Exterior,
                Name = "exterior",
                TimeInMinutes = 12,
                Price = 3213,
                PriceMpv = 4017,
                Hidden = false
            },
            new Service {
                Type = Interior,
                Name = "interior",
                TimeInMinutes = 12,
                Price = 1607,
                PriceMpv = 2410,
                Hidden = false},
            new Service {
                Type = Carpet,
                Name = "carpet",
                Description = "whole carpet cleaning, including all the seats",
                TimeInMinutes = 24,
                Price = -1,
                PriceMpv = -1,
                Hidden = false
            },
            new Service {
                Type = SpotCleaning,
                Name = "spot cleaning",
                Description = "partial cleaning of the carpet, only where it is needed (eg. when something is spilled in the car)",
                TimeInMinutes = 0,
                Price = 3534,
                PriceMpv = 3534,
                Hidden = false
            },
            new Service {
                Type = VignetteRemoval,
                Name = "vignette removal",
                Description = "eg. highway vignettes on the windscreen",
                TimeInMinutes = 0,
                Price = 466,
                PriceMpv = 466,
                Hidden = false
            },
            new Service {
                Type = Polishing,
                Name = "polishing",
                Description = "for small scratches",
                TimeInMinutes = 0,
                Price = 4498,
                PriceMpv = 4498,
                Hidden = false
            },
            new Service {
                Type = AcCleaningOzon,
                Name = "AC cleaning 'ozon'",
                Description = "disinfects molecules with ozone",
                TimeInMinutes = 0,
                Price = 8033,
                PriceMpv = 8033,
                Hidden = false
            },
            new Service {
                Type = AcCleaningBomba,
                Name = "AC cleaning 'bomba'",
                Description = "blowing chemical spray in the AC system",
                TimeInMinutes = 0,
                Price = 6426,
                PriceMpv = 6426,
                Hidden = false
            },
            new Service {
                Type = BugRemoval,
                Name = "bug removal",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 804,
                PriceMpv = 804,
                Hidden = true
            },
            new Service {
                Type = WheelCleaning,
                Name = "wheel cleaning",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 964,
                PriceMpv = 964,
                Hidden = true
            },
            new Service {
                Type = TireCare,
                Name = "tire care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 804,
                PriceMpv = 804,
                Hidden = true
            },
            new Service {
                Type = LeatherCare,
                Name = "leather care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 8033,
                PriceMpv = 8033,
                Hidden = true
            },
            new Service {
                Type = PlasticCare,
                Name = "plastic care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 7230,
                PriceMpv = 7230,
                Hidden = true
            },
            new Service {
                Type = PreWash,
                Name = "prewash",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 804,
                PriceMpv = 804,
                Hidden = true
            }
        };
    }

    /// <summary>
    /// Representation of a service which the user can choose.
    /// </summary>
    public class Service
    {
        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        [JsonPropertyName("id")]
        public ServiceType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the service.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the time needed for this service.
        /// </summary>
        public int TimeInMinutes { get; set; }

        /// <summary>
        /// Gets or sets the price of the service.
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// Gets or sets the price of the service for MPVs.
        /// </summary>
        public int PriceMpv { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the service is hidden from normal users on the UI.
        /// </summary>
        public bool Hidden { get; set; }
    }
}
