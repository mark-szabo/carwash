using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MSHU.CarWash.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationId { get; set; }

        public string EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        public int SelectedServiceId { get; set; }

        [Required]
        [StringLength(7)]
        public string VehiclePlateNumber { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [StringLength(250)]
        public string Comment{ get; set; }

        [Required]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Required]
        public DateTime CreatedOn { get; set; }
    }
}