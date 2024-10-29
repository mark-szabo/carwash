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
        PreWash = 13,
        PetHairRemoval = 14,
        BikeRack = 15,
        RoofBox = 16,
        ChildSeat = 17
    }
#pragma warning restore CS1591, SA1602

    /// <summary>
    /// Services the users can choose from.
    /// </summary>
    public static class ServiceTypes
    {
        /// <summary>
        /// Services the users can choose from.
        /// </summary>
        public static readonly List<Service> Types =
        [
            new Service {
                Type = Exterior,
                Name = "exterior",
                TimeInMinutes = 12,
                Price = 6311,
                PriceMpv = 10728,
                Hidden = false
            },
            new Service {
                Type = Interior,
                Name = "interior",
                TimeInMinutes = 12,
                Price = 3610,
                PriceMpv = 6137,
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
                Price = 4474,
                PriceMpv = 4474,
                Hidden = false
            },
            new Service {
                Type = VignetteRemoval,
                Name = "vignette removal",
                Description = "eg. highway vignettes on the windscreen",
                TimeInMinutes = 0,
                Price = 1008,
                PriceMpv = 1008,
                Hidden = false
            },
            new Service {
                Type = Polishing,
                Name = "polishing",
                Description = "for small scratches",
                TimeInMinutes = 0,
                Price = 9679,
                PriceMpv = 9679,
                Hidden = false
            },
            new Service {
                Type = AcCleaningOzon,
                Name = "AC cleaning 'ozon'",
                Description = "disinfects molecules with ozone",
                TimeInMinutes = 0,
                Price = 10091,
                PriceMpv = 10091,
                Hidden = false
            },
            new Service {
                Type = AcCleaningBomba,
                Name = "AC cleaning 'bomba'",
                Description = "blowing chemical spray in the AC system",
                TimeInMinutes = 0,
                Price = 13822,
                PriceMpv = 13822,
                Hidden = false
            },
            new Service {
                Type = BugRemoval,
                Name = "bug removal",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 1732,
                PriceMpv = 1732,
                Hidden = true
            },
            new Service {
                Type = WheelCleaning,
                Name = "wheel cleaning",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 2073,
                PriceMpv = 2073,
                Hidden = true
            },
            new Service {
                Type = TireCare,
                Name = "tire care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 1732,
                PriceMpv = 1732,
                Hidden = true
            },
            new Service {
                Type = LeatherCare,
                Name = "leather care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 17283,
                PriceMpv = 17283,
                Hidden = true
            },
            new Service {
                Type = PlasticCare,
                Name = "plastic care",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 9149,
                PriceMpv = 9149,
                Hidden = true
            },
            new Service {
                Type = PreWash,
                Name = "prewash",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 1732,
                PriceMpv = 1732,
                Hidden = true
            },
            new Service {
                Type = PetHairRemoval,
                Name = "pet hair removal",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 5080,
                PriceMpv = 5080,
                Hidden = true
            },
            new Service {
                Type = BikeRack,
                Name = "bike rack",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 2332,
                PriceMpv = 2332,
                Hidden = true
            },
            new Service {
                Type = RoofBox,
                Name = "roof box",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 6801,
                PriceMpv = 6801,
                Hidden = true
            },
            new Service {
                Type = ChildSeat,
                Name = "child seat",
                Description = "we'll add this if it's needed",
                TimeInMinutes = 0,
                Price = 8744,
                PriceMpv = 8744,
                Hidden = true
            }
        ];
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
