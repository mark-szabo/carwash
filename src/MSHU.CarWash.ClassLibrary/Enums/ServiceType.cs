using System.Collections.Generic;
using static MSHU.CarWash.ClassLibrary.Enums.ServiceType;

namespace MSHU.CarWash.ClassLibrary.Enums
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

    public static class ServiceExtensions
    {
        public static string ToFriendlyString(this ServiceType serviceType)
        {
            switch (serviceType)
            {
                case Exterior:
                    return "exterior";
                case Interior:
                    return "interior";
                case Carpet:
                    return "carpet";
                case SpotCleaning:
                    return "spot cleaning";
                case VignetteRemoval:
                    return "vignette removal";
                case Polishing:
                    return "polishing";
                case AcCleaningOzon:
                    return "AC cleaning 'ozon'";
                case AcCleaningBomba:
                    return "AC cleaning 'bomba'";
                case BugRemoval:
                    return "bug removal";
                case WheelCleaning:
                    return "wheel cleaning";
                case TireCare:
                    return "tire care";
                case LeatherCare:
                    return "leather care";
                case PlasticCare:
                    return "plastic care";
                case PreWash:
                    return "prewash";
                default:
                    return "no info";
            }
        }
    }

    public static class ServiceTypes
    {
        public static readonly List<Service> Types = new List<Service>
        {
            new Service {Type = Exterior, TimeInMinutes = 12, Price = 3213, PriceMpv = 4017, Hidden = false},
            new Service {Type = Interior, TimeInMinutes = 12, Price = 1607, PriceMpv = 2410, Hidden = false},
            new Service {Type = Carpet, TimeInMinutes = 24, Price = -1, PriceMpv = -1, Hidden = false},
            new Service {Type = SpotCleaning, TimeInMinutes = 0, Price = 3534, PriceMpv = 3534, Hidden = false},
            new Service {Type = VignetteRemoval, TimeInMinutes = 0, Price = 466, PriceMpv = 466, Hidden = false},
            new Service {Type = Polishing, TimeInMinutes = 0, Price = 4498, PriceMpv = 4498, Hidden = false},
            new Service {Type = AcCleaningOzon, TimeInMinutes = 0, Price = 8033, PriceMpv = 8033, Hidden = false},
            new Service {Type = AcCleaningBomba, TimeInMinutes = 0, Price = 6426, PriceMpv = 6426, Hidden = false},
            new Service {Type = BugRemoval, TimeInMinutes = 0, Price = 804, PriceMpv = 804, Hidden = true},
            new Service {Type = WheelCleaning, TimeInMinutes = 0, Price = 964, PriceMpv = 964, Hidden = true},
            new Service {Type = TireCare, TimeInMinutes = 0, Price = 804, PriceMpv = 804, Hidden = true},
            new Service {Type = LeatherCare, TimeInMinutes = 0, Price = 8033, PriceMpv = 8033, Hidden = true},
            new Service {Type = PlasticCare, TimeInMinutes = 0, Price = 7230, PriceMpv = 7230, Hidden = true},
            new Service {Type = PreWash, TimeInMinutes = 0, Price = 804, PriceMpv = 804, Hidden = true}
        };
    }

    public class Service
    {
        public ServiceType Type { get; set; }
        public int TimeInMinutes { get; set; }
        public int Price { get; set; }
        public int PriceMpv { get; set; }
        public bool Hidden { get; set; }
    }
}
