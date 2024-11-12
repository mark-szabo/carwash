using CarWash.ClassLibrary.Models;

namespace CarWash.ClassLibrary
{
    /// <summary>
    /// Class containing constants.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// Named <see cref="Service"/>s used for special business logic.
        /// </summary>
        public class ServiceType
        {
            /// <summary>
            /// Exterior wash.
            /// </summary>
            public const int Exterior = 0;

            /// <summary>
            /// Interior cleaning.
            /// </summary>
            public const int Interior = 1;

            /// <summary>
            /// Carpet cleaning.
            /// </summary>
            public const int Carpet = 2;
        }
    }
}
