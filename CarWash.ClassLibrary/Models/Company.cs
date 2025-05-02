using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a company whose users can use the CarWash app.
    /// DB mapped entity.
    /// </summary>
    public class Company : ApplicationDbContext.IEntity
    {
        /// <inheritdoc />
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// Constant name of the CarWash "company".
        /// </summary>
        [NotMapped]
        public const string Carwash = "carwash";

        /// <summary>
        /// Gets or sets the name of the company.
        /// </summary>
        [Required]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the tenant id of the company.
        /// </summary>
        [Required]
        [StringLength(36)]
        public required string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the company's daily reservation limit.
        /// </summary>
        [Required]
        public int DailyLimit { get; set; }

        /// <summary>
        /// Gets or sets the company's color in HEX.
        /// </summary>
        public string? Color { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the company was added.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the company was last updated.
        /// </summary>
        public DateTime UpdatedOn { get; set; }
    }
}
