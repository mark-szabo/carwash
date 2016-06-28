using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSHU.CarWash.DomainModel;

namespace MSHU.CarWash.UWP.Services
{
    /// <summary>
    /// Dummy "no-implementation" of the interface for:
    /// 1. Testing
    /// 2. Temporary solution for current platform issues with the built-in Calendar App
    /// </summary>
    class DummyAppointmentService : IAppointmentService
    {
        public async Task<bool> CreateAppointmentAsync(Reservation reservation)
        {
            return true;
        }

        public async Task<bool> RemoveAppointmentAsync(int reservationID)
        {
            return true;
        }

        public async Task<bool> UpdateAppointmentAsync(Reservation reservation)
        {
            return true;
        }
    }
}
