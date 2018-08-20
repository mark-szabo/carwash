using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MSHU.CarWash.PWA.Extensions
{
    /// <summary>
    /// Extension class to User.Claims
    /// </summary>
    public static class ClaimsExtension
    {
        /// <summary>
        /// Checks whether user is admin
        /// usage: httpContextAccessor.HttpContext.User.Claims.IsAdmin()
        /// </summary>
        /// <param name="claims">User.Claims</param>
        /// <returns>true if admin</returns>
        public static bool IsAdmin(this IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "admin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }

        /// <summary>
        /// Checks whether user is carwash admin
        /// usage: httpContextAccessor.HttpContext.User.Claims.IsCarwashAdmin()
        /// </summary>
        /// <param name="claims">User.Claims</param>
        /// <returns>true if carwash admin</returns>
        public static bool IsCarwashAdmin(this IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "carwashadmin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }
    }
}
