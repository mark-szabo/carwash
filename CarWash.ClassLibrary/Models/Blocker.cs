using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Database representation of a time blocker object.
    /// Blockers are used by the CarWash to block out time (eg. on holidays or when they are closed for some other reason).
    /// DB mapped entity.
    /// </summary>
    public class Blocker : ApplicationDbContext.IEntity
    {
        /// <inheritdoc />
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the start date and time of the blocker.
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date and time of the blocker.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the comment of the blocker for the UI of the admins (normal users won't be able to see this).
        /// </summary>
        public string? Comment { get; set; }

        /// <summary>
        /// Gets or sets the id of the user who created the blocker.
        /// </summary>
        public string? CreatedById { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the blocker was created.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedOn { get; set; }
    }
}
