namespace MSHU.CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a company which users can use the CarWash app.
    /// </summary>
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

        /// <summary>
        /// Gets or sets the name of the company.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the tenant id of the company.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the company's daily reservation limit.
        /// </summary>
        public int DailyLimit { get; set; }
    }
}
