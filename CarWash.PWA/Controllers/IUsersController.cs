using CarWash.ClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
