using Microsoft.AspNetCore.Identity;
using MSHU.CarWash.ClassLibrary.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MSHU.CarWash.ClassLibrary.Models
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
        public bool IsAdmin { get; set; } = false;

        [Required]
        public bool IsCarwashAdmin { get; set; } = false;

        [Required]
        public bool CalendarIntegration { get; set; } = true;

        [Required]
        public NotificationChannel NotificationChannel { get; set; } = NotificationChannel.NotSet;
    }
}
