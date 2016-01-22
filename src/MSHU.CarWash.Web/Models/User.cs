using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWash.Models
{
    public class User
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}