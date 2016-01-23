using Microsoft.Azure.Mobile.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWash.Services.Models
{
    public class User : EntityData
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool IsAdmin { get; set; }
    }
}