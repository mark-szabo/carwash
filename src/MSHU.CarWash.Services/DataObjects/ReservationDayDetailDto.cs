using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class ReservationDayDetailDto
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