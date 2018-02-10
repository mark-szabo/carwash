using System;
using System.ComponentModel.DataAnnotations;

namespace MSHU.CarWash.DomainModel
{
    public class Employee
    {
        [Key]
        public string EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(7)]
        public string VehiclePlateNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string Email { get; set; }

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }

        [Required]
        [StringLength(100)]
        public string ModifiedBy { get; set; }

        [Required]
        public DateTime ModifiedOn { get; set; }

        [Timestamp]
        public Byte[] TimeStamp { get; set; }
    }
}