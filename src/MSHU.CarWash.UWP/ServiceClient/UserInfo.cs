using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ServiceClient
{
    /// <summary>
    /// User Info
    /// </summary>
    public class UserInfo
    {
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string DisplayableId { get; set;}


        /// <summary>
        /// Default constructor
        /// </summary>
        public UserInfo()
        {

        }
    }
}
