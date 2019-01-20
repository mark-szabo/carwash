namespace CarWash.ClassLibrary.Models
{
    /// <summary>
    /// Representation of a company whose users can use the CarWash app.
    /// </summary>
    public class Company
    {
        /// <summary>
        /// Constant name of the CarWash "company".
        /// </summary>
        public const string Carwash = "carwash";
        
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
