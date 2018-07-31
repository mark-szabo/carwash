using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public DateTime Date { get; set; }
        
        public string Comment { get; set; }

        [Required]
        public string CreatedById { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }
    }
}
