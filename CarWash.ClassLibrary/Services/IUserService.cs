using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to manage user-related operations
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Gets the current user.
        /// </summary>
        User CurrentUser { get; }
    }
}
