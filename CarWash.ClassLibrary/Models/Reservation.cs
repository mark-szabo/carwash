using CarWash.ClassLibrary.Enums;
using Newtonsoft.Json;
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
        [StringLength(7)]
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
        /// List of <see cref="ServiceType"/>s deserialized from <see cref="ServicesJson"/>.
        /// </value>
        [NotMapped]
        public List<ServiceType> Services
        {
            get => ServicesJson == null ? null : JsonConvert.DeserializeObject<List<ServiceType>>(ServicesJson);
            set => ServicesJson = JsonConvert.SerializeObject(value);
        }

        /// <summary>
        /// Gets or sets the reservation services serialized in JSON.
        /// </summary>
        /// <value>
        /// List of <see cref="ServiceType"/>s serialized in JSON.
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
        /// Gets or sets the reservation comment.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the comment from the carwash.
        /// </summary>
        public string CarwashComment { get; set; }

        /// <summary>
        /// Gets or sets the user id of the reservation creator.
        /// </summary>
        public string CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the date and time whenthe reservation was created.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets Outlook's even id of the reservation.
        /// </summary>
        public string OutlookEventId { get; set; }

        /// <summary>
        /// Gets reservation's costs.
        /// </summary>
        [NotMapped]
        public int Price
        {
            get
            {
                var sum = 0;

                foreach (var service in Services)
                {
                    var serviceCosts = ServiceTypes.Types.SingleOrDefault(s => s.Type == service);
                    if (serviceCosts == null) throw new Exception("Invalid service. No price found.");

                    sum += Mpv ? serviceCosts.PriceMpv : serviceCosts.Price;
                }

                return sum;
            }
        }
    }
}
