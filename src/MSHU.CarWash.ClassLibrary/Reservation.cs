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
        
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        [Required]
        [StringLength(7)]
        public string VehiclePlateNumber { get; set; }

        public string Location { get; set; }
    
        public State State { get; set; }

        [NotMapped]
        public List<ServiceType> Services
        {
            get => ServicesJson == null ? null : JsonConvert.DeserializeObject<List<ServiceType>>(ServicesJson);
            set => ServicesJson = JsonConvert.SerializeObject(value);
        }
        
        public string ServicesJson { get; set; }
        
        public bool Private { get; set; }

        public bool Mpv { get; set; }
        
        public int? TimeRequirement { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
        
        public string Comment { get; set; }
        
        public string CarwashComment { get; set; }
        
        public string CreatedById { get; set; }
        
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        public string OutlookEventId { get; set; }
    }
}
