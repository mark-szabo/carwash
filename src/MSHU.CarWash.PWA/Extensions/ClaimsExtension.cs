using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MSHU.CarWash.PWA.Extensions
{
    public static class ClaimsExtension
    {
        public static bool IsAdmin(this IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "admin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }

        public static bool IsCarwashAdmin(this IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "carwashadmin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }
    }
}
