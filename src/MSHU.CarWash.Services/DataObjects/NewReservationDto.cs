using MSHU.CarWash.Services.Helpers;
using MSHU.CarWash.Services.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class NewReservationDto
    {
        public NewReservationDto()
        {
            this.Services = new List<ServiceDto>();
            this.Services.Add(new ServiceDto { ServiceId = (int)ServiceEnum.KulsoMosas, ServiceName = ServiceEnum.KulsoMosas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceDto { ServiceId = (int)ServiceEnum.BelsoTakaritas, ServiceName = ServiceEnum.BelsoTakaritas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceDto { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritas.GetDescription(), Selected = false });
            this.Services.Add(new ServiceDto { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas.GetDescription(), Selected = false });
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

        public List<ServiceDto> Services { get; set; }

        public bool IsAdmin { get; set; }
    }
}