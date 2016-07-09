using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.Services
{
    /// <summary>
    /// Tiny class to hold information about appointments we need to persist
    /// </summary>
    class AppointmentInfo
    {
        public AppointmentInfo(string id, DateTime date)
        {
            ID = id;
            Date = date;
        }

        /// <summary>
        /// Appointment ID as given back from Calendar app
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Data of appointment so that we can purge old items
        /// </summary>
        public DateTime Date { get; set; }
    }
}
