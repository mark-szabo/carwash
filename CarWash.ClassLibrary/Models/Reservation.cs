using CarWash.ClassLibrary.Enums;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database representation of a reservation object.
    /// DB mapped entity.
    /// </summary>
    public class Reservation : ApplicationDbContext.IEntity
    {
        /// <inheritdoc />
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the reservation user id.
        /// </summary>
        [ForeignKey("User")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the virtual user object for the reservation.
        /// </summary>
        public virtual User User { get; set; }

        /// <summary>
        /// Gets or sets the reservation vehicle plate number.
        /// </summary>
        [Required]
        [StringLength(8)]
        public string VehiclePlateNumber { get; set; }

        /// <summary>
        /// Gets or sets the reservation location.
        /// </summary>
        /// <value>
        /// A concatenation of the building, floor and seat separated by '/'.
        /// </value>
        public string Location { get; set; }

        /// <summary>
        /// Gets or sets the reservation state.
        /// </summary>
        public State State { get; set; }

        /// <summary>
        /// Gets or sets the reservation services.
        /// </summary>
        /// <value>
        /// List of service ids deserialized from <see cref="ServicesJson"/>.
        /// </value>
        [NotMapped]
        public List<int> Services
        {
            get => ServicesJson == null ? null : JsonSerializer.Deserialize<List<int>>(ServicesJson, Constants.DefaultJsonSerializerOptions);
            set => ServicesJson = JsonSerializer.Serialize(value, Constants.DefaultJsonSerializerOptions);
        }

        /// <summary>
        /// Gets or sets the reservation services serialized in JSON.
        /// </summary>
        /// <value>
        /// List of service ids serialized in JSON.
        /// </value>
        public string ServicesJson { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the reservation is private.
        /// </summary>
        /// <value>
        /// A boolean indicating whether the reservation is private.
        /// </value>
        public bool Private { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the car is an MPV.
        /// </summary>
        /// <value>
        /// A boolean indicating whether the car is an MPV.
        /// </value>
        public bool Mpv { get; set; }

        /// <summary>
        /// Gets or sets the time requirement of the reservation.
        /// </summary>
        public int TimeRequirement { get; set; }

        /// <summary>
        /// Gets or sets the reservation start date and time.
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// (Optional) Gets or sets the reservation end date and time.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the user id of the reservation creator.
        /// </summary>
        public string CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the reservation was created.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets Outlook's even id of the reservation.
        /// </summary>
        public string OutlookEventId { get; set; }

        /// <summary>
        /// Gets or sets the reservation comments.
        /// </summary>
        /// <value>
        /// List of comments deserialized from <see cref="CommentsJson"/>.
        /// </value>
        [NotMapped]
        public List<Comment> Comments
        {
            get => CommentsJson == null ? [] : JsonSerializer.Deserialize<List<Comment>>(CommentsJson, Constants.DefaultJsonSerializerOptions);
            set => CommentsJson = JsonSerializer.Serialize(value, Constants.DefaultJsonSerializerOptions);
        }

        /// <summary>
        /// Gets or sets the reservation comments serialized in JSON.
        /// </summary>
        /// <value>
        /// List of comments serialized in JSON.
        /// </value>
        public string CommentsJson { get; set; }

        /// <summary>
        /// Gets reservation's costs.
        /// </summary>
        /// <param name="configuration">The car wash configuration containing service prices.</param>
        /// <returns>The total price of the reservation based on selected services and vehicle type.</returns>
        public int GetPrice(CarWashConfiguration configuration)
        {
            var sum = 0;

            foreach (var service in Services)
            {
                var serviceCosts = configuration.Services.SingleOrDefault(s => s.Id == service)
                    ?? throw new Exception("Invalid service. No price found.");

                sum += Mpv ? serviceCosts.PriceMpv : serviceCosts.Price;
            }

            return sum;
        }

        /// <summary>
        /// Gets the concatenated names of the services.
        /// </summary>
        /// <param name="configuration">The car wash configuration.</param>
        /// <returns>A string of service names separated by commas and spaces.</returns>
        public string GetServiceNames(CarWashConfiguration configuration)
        {
            var serviceNames = Services
                .Select(serviceId => configuration.Services.SingleOrDefault(s => s.Id == serviceId)?.Name)
                .Where(name => !string.IsNullOrEmpty(name));

            return string.Join(", ", serviceNames);
        }

        /// <summary>
        /// Adds a comment to the reservation.
        /// </summary>
        /// <param name="comment">The comment to add.</param>
        public void AddComment(Comment comment)
        {
            var comments = Comments ?? [];
            comments.Add(comment);
            Comments = comments;
        }
    }
}
