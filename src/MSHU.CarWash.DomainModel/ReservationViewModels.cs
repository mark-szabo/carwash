using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.DomainModel
{
    public class ReservationViewModel
    {
        public List<ReservationDayDetailsViewModel> ReservationsByDayActive { get; set; }
        public List<ReservationDayDetailsViewModel> ReservationsByDayHistory { get; set; }
    }


    public class ReservationDayDetailsViewModel
    {
        public DateTime Day { get; set; }
        public string DayName { get; set; }
        public string MonthName { get; set; }

        public int DayNumber { get; set; }
        
        public List<ReservationDayDetailViewModel> Reservations { get; set; }
    }

    public class ReservationDayDetailViewModel
    {
        public int ReservationId { get; set; }

        public string EmployeeId { get; set; }

        public string EmployeeName { get; set; }

        public string VehiclePlateNumber { get; set; }

        public string SelectedServiceName { get; set; }

        public string Comment { get; set; }
        public bool IsDeletable { get; set; }
    }

}
