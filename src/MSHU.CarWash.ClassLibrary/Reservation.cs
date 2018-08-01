using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace MSHU.CarWash.ClassLibrary
{
    public class Reservation : ApplicationDbContext.IEntity
    {
        public string Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        [StringLength(7)]
        public string VehiclePlateNumber { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public State State { get; set; }

        [NotMapped]
        public List<ServiceType> Services
        {
            get => ServicesJson == null ? null : JsonConvert.DeserializeObject<List<ServiceType>>(ServicesJson);
            set => ServicesJson = JsonConvert.SerializeObject(value);
        }

        [Required]
        public string ServicesJson { get; set; }

        [Required]
        public bool Private { get; set; }

        public bool Mpv { get; set; }

        [Required]
        public int TimeRequirement { get; set; }

        [Required]
        public DateTime DateFrom { get; set; }

        [Required]
        public DateTime DateTo { get; set; }
        
        public string Comment { get; set; }
        
        public string CarwashComment { get; set; }

        [Required]
        public string CreatedById { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }
    }
}
