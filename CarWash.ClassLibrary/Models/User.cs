using Microsoft.AspNetCore.Identity;
using CarWash.ClassLibrary.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database representation of a user object.
    /// DB mapped entity.
    /// </summary>
    public class User : IdentityUser
    {
        /// <summary>
        /// Gets or sets the oid of the user from the IDP.
        /// </summary>
        //[Required]
        public string? Oid { get; set; }

        /// <summary>
        /// Gets or sets the email of the user, as-is in AD.
        /// </summary>
        [Required]
        public override string? Email { get; set; }

        /// <summary>
        /// Gets or sets the given name of the user, as-is in AD.
        /// </summary>
        [Required]
        public required string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the surname of the user, as-is in AD.
        /// </summary>
        public string? LastName { get; set; }

        /// <summary>
        /// Gets the full name of the user concatenated from the first and last names.
        /// </summary>
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        /// <summary>
        /// Gets or sets the company of the user, determined by tenant id at first login.
        /// </summary>
        [Required]
        public required string Company { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is an admin.
        /// </summary>
        [Required]
        public bool IsAdmin { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the user is a CarWash admin.
        /// </summary>
        [Required]
        public bool IsCarwashAdmin { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the calendar integration feature is enabled for the user.
        /// </summary>
        [Required]
        public bool CalendarIntegration { get; set; } = true;

        /// <summary>
        /// Gets or sets the notification channel set by the user.
        /// </summary>
        [Required]
        public NotificationChannel NotificationChannel { get; set; } = NotificationChannel.NotSet;

        /// <summary>
        /// Gets or sets the billing name for the user.
        /// </summary>
        public string? BillingName { get; set; }

        /// <summary>
        /// Gets or sets the billing address for the user.
        /// </summary>
        public string? BillingAddress { get; set; }

        /// <summary>
        /// Gets or sets the payment method for the user.
        /// </summary>
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.WireTransfer;
    }
}
