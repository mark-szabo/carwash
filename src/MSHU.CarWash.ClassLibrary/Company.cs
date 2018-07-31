namespace MSHU.CarWash.ClassLibrary
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

        public string Name { get; set; }

        public string TenantId { get; set; }
    }
}
