using System.Text.Json.Serialization;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a service which the user can choose.
    /// </summary>
    public class Service
    {
        /// <summary>
        /// Gets or sets the type of the service.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the group name of the service.
        /// </summary>
        public required string Group { get; set; }

        /// <summary>
        /// Gets or sets the description of the service.
        /// </summary>
        public string? Description { get; set; }

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
