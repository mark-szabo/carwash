using CarWash.ClassLibrary.Enums;
using System;

namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Represents a comment in the reservation system.
    /// </summary>
    public class Comment
    {
        /// <summary>
        /// Gets or sets the message of the comment.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the role of the comment author.
        /// </summary>
        public CommentRole Role { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the comment.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the user ID of the comment author.
        /// </summary>
        public string UserId { get; set; }
    }
}
