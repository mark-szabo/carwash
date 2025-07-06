using CarWash.ClassLibrary.Models;
using Microsoft.AspNetCore.Http;

namespace CarWash.ClassLibrary.Services
{
    /// <summary>
    /// Defines a service to manage user-related operations
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="UserService"/> class.
    /// </remarks>
    public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
    {
        /// <inheritdoc />
        public User? CurrentUser
        {
            get
            {
                if (httpContextAccessor.HttpContext?.Items.TryGetValue("CurrentUser", out var userObj) == true)
                {
                    return userObj as User;
                }
                return null;
            }
        }
    }
}
