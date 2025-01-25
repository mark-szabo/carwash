using CarWash.ClassLibrary.Models;
using System.Text.Json.Serialization;

namespace CarWash.ClassLibrary.Enums
{
    /// <summary>
    /// Enum representing the role of the comment author.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CommentRole
    {
        /// <summary>
        /// Message was sent by the user.
        /// </summary>
        [JsonStringEnumMemberName("user")]
        User,

        /// <summary>
        /// Message was sent by a CarWash employee. User id is stored in the <see cref="Comment.UserId"/> property.
        /// </summary>
        [JsonStringEnumMemberName("carwash")]
        Carwash
    }
}
