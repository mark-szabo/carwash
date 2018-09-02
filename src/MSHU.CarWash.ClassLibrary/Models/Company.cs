namespace MSHU.CarWash.ClassLibrary.Models
{
    public class Company
    {
        public const string Carwash = "carwash";
        public const string Microsoft = "microsoft";
        public const string Sap = "sap";
        public const string Graphisoft = "graphisoft";

        public Company(string name, string tenantId)
        {
            Name = name;
            TenantId = tenantId;
        }

        public Company(string name, int dailyLimit)
        {
            Name = name;
            DailyLimit = dailyLimit;
        }

        public string Name { get; set; }

        public string TenantId { get; set; }

        public int DailyLimit { get; set; }
    }
}
