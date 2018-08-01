using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace MSHU.CarWash.ClassLibrary
{
    public class User : IdentityUser
    {
        [Required]
        public string FirstName { get; set; }
        
        public string LastName { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        [Required]
        public string Company { get; set; }

        [Required]
        public bool IsAdmin { get; set; }

        [Required]
        public bool IsCarwashAdmin { get; set; }
    }
}
