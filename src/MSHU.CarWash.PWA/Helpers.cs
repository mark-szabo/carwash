using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace MSHU.CarWash.PWA
{
    public class Helpers
    {
        public static bool IsAdmin(IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "admin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }
        public static bool IsCarwashAdmin(IEnumerable<Claim> claims)
        {
            var adminClaim = claims.SingleOrDefault(c => c.Type == "carwashadmin");
            if (adminClaim == null) return false;

            return adminClaim.Value == "true";
        }
    }
}
