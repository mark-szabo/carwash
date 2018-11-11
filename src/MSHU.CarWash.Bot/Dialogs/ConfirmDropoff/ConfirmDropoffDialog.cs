using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MSHU.CarWash.Bot.Dialogs.ConfirmDropoff
{
    /// <summary>
    /// User state properties for Dropoff confirmation.
    /// </summary>
    public class ConfirmDropoffDialog
    {
        public string ReservationId { get; set; }

        public string Building { get; set; }

        public string Floor { get; set; }

        public string Seat { get; set; }
    }
}
