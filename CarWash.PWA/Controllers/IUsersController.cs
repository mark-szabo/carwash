using CarWash.ClassLibrary.Models;

namespace CarWash.PWA.Controllers
{
    /// <summary>
    /// Defines a controller to manage users.
    /// </summary>
    public interface IUsersController
    {
        /// <summary>
        /// Gets currently signed in user.
        /// </summary>
        /// <returns>The <see cref="User"/> object of the current user.</returns>
        User GetCurrentUser();
    }
}
