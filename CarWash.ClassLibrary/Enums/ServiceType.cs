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
        /// Gets or sets the group name of the service.
        /// </summary>
        public string Group { get; set; }

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
