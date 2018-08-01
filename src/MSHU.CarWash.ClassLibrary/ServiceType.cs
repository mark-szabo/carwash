using System.Collections.Generic;
using static MSHU.CarWash.ClassLibrary.ServiceType;

namespace MSHU.CarWash.ClassLibrary
{
    public enum ServiceType
    {
        Exterior = 0,
        Interior = 1,
        Carpet = 2,
        SpotCleaning = 3,
        BugRemoval = 4,
        WheelCleaning = 5
    }

    public class ServiceTypes
    {
        public readonly List<Service> Types = new List<Service>
        {
            new Service {Type = Exterior, TimeInMinutes = 10, Hidden = false},
            new Service {Type = Interior, TimeInMinutes = 10, Hidden = false},
            new Service {Type = Carpet, TimeInMinutes = 20, Hidden = false},
            new Service {Type = SpotCleaning, TimeInMinutes = 0, Hidden = false},
            new Service {Type = BugRemoval, TimeInMinutes = 0, Hidden = true},
            new Service {Type = WheelCleaning, TimeInMinutes = 0, Hidden = true}
        };
    }

    public class Service
    {
        public ServiceType Type { get; set; }
        public int TimeInMinutes { get; set; }
        public bool Hidden { get; set; }
    }
}
