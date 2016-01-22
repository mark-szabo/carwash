using MSHU.CarWash.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace MSHU.CarWash.Helpers
{
    internal static class UserHelper
    {
        internal static User GetCurrentUser()
        {
            string admins = ConfigurationManager.AppSettings["Admins"];

            var user = new User();
            user.Id = ClaimsPrincipal.Current.Identity.Name;
            user.FullName = string.Format("{0} {1}",
                                    ClaimsPrincipal.Current.FindFirst(ClaimTypes.Surname).Value,
                                    ClaimsPrincipal.Current.FindFirst(ClaimTypes.GivenName).Value).ToUpper();
            user.Email = ClaimsPrincipal.Current.Identity.Name;
            user.IsAdmin = admins.ToLower().Contains(user.Email.ToLower());
            return user;
        }
    }
}