using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Enum representing the role of the comment author.
    /// </summary>
    public enum CommentRole
    {
        /// <summary>
        /// Message was sent by the user.
        /// </summary>
        User,

        /// <summary>
        /// Message was sent by a CarWash employee. User id is stored in the <see cref="Comment.UserId"/> property.
        /// </summary>
        Carwash
    }
}
