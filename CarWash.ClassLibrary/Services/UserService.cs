using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to manage user-related operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </remarks>
    /// <param name="currentUser">The current user.</param>
    public class UserService(User currentUser) : IUserService
    {
        /// <inheritdoc />
        public User CurrentUser { get; } = currentUser;
    }
}
