using MSHU.CarWash.DomainModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

    public class NewReservationViewModel
    {
        public NewReservationViewModel()
        {
            this.Services = new List<ServiceViewModel>();
            this.Services.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosas, ServiceName =  ServiceEnum.KulsoMosas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.BelsoTakaritas, ServiceName = ServiceEnum.BelsoTakaritas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas.GetDescription(), Selected = false });
        }
        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string EmployeeId { get; set; }

        [Required]
        public string EmployeeName { get; set; }

        [Required]
        public string VehiclePlateNumber { get; set; }

        [Required]
        public int? SelectedServiceId
        {
            set
            {
                var selected = this.Services.FirstOrDefault(s => s.ServiceId == value);
                if (selected != null)
                {
                    selected.Selected = true;
                }
            }

            get
            {
                var selected = this.Services.FirstOrDefault(s => s.Selected);
                if (selected != null)
                {
                    return selected.ServiceId;
                }
                return null;
            }
        }

        public string Comment { get; set; }

        public List<ServiceViewModel> Services { get; set; }

        public bool IsAdmin { get; set; }
    }

    public class ServiceViewModel
    {
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public bool Selected { get; set; }
    }

}
