using MSHU.CarWash.DomainModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.UI.Xaml;

namespace MSHU.CarWash.UWP.Services
{
    /// <summary>
    /// Defines a service to manage car wash appointments.
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// Create appointment for based on a reservation.
        /// </summary>
        /// <param name="reservation">Reservation to get the details from</param>
        /// <returns>True if succeeded</returns>
        Task<bool> CreateAppointmentAsync(Reservation reservation);

        /// <summary>
        /// Update appointment for based on a reservation.
        /// </summary>
        /// <param name="reservation">Reservation to get the details from</param>
        /// <returns>True if succeeded</returns>
        Task<bool> UpdateAppointmentAsync(Reservation reservation);

        /// <summary>
        /// Delete appointment for a certain date.
        /// </summary>
        /// <param name="reservation">Date of reservation</param>
        /// <returns>True if succeeded</returns>
        Task<bool> RemoveAppointmentAsync(int reservationID);
    }
}
