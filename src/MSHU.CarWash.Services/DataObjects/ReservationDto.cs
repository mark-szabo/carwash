using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MSHU.CarWashService.DataObjects
{
    public class ReservationDto
    {
        public List<ReservationDayDetailsDto> ReservationsByDayActive { get; set; }
        public List<ReservationDayDetailsDto> ReservationsByDayHistory { get; set; }
    }
}