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
        VignetteRemoval = 4,
        Polishing = 5,
        AcCleaningOzon = 6,
        AcCleaningBomba = 7,
        // below are those services that are hidden from the user
        BugRemoval = 8,
        WheelCleaning = 9,
        TireCare = 10,
        LeatherCare = 11,
        PlasticCare = 12,
        PreWash = 13
    }

    public class ServiceTypes
    {
        public readonly List<Service> Types = new List<Service>
        {
            new Service {Type = Exterior, TimeInMinutes = 12, Hidden = false},
            new Service {Type = Interior, TimeInMinutes = 12, Hidden = false},
            new Service {Type = Carpet, TimeInMinutes = 24, Hidden = false},
            new Service {Type = SpotCleaning, TimeInMinutes = 0, Hidden = false},
            new Service {Type = VignetteRemoval, TimeInMinutes = 0, Hidden = false},
            new Service {Type = Polishing, TimeInMinutes = 0, Hidden = false},
            new Service {Type = AcCleaningOzon, TimeInMinutes = 0, Hidden = false},
            new Service {Type = AcCleaningBomba, TimeInMinutes = 0, Hidden = false},
            new Service {Type = BugRemoval, TimeInMinutes = 0, Hidden = true},
            new Service {Type = WheelCleaning, TimeInMinutes = 0, Hidden = true},
            new Service {Type = TireCare, TimeInMinutes = 0, Hidden = true},
            new Service {Type = LeatherCare, TimeInMinutes = 0, Hidden = true},
            new Service {Type = PlasticCare, TimeInMinutes = 0, Hidden = true},
            new Service {Type = PreWash, TimeInMinutes = 0, Hidden = true}
        };
    }

    public class Service
    {
        public ServiceType Type { get; set; }
        public int TimeInMinutes { get; set; }
        public bool Hidden { get; set; }
    }
}
