using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class ServiceDto
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public bool Selected { get; set; }
    }
}