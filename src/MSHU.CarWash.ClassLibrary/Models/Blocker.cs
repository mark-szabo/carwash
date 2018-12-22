using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSHU.CarWash.ClassLibrary.Models
{
    public class Blocker : ApplicationDbContext.IEntity
    {
        public string Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Comment { get; set; }

        public string CreatedById { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }
    }
}
