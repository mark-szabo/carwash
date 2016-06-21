using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSHU.CarWash.DomainModel;
using Windows.ApplicationModel.Appointments;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Popups;
using Windows.Storage;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using MSHU.CarWash.DomainModel.Helpers;

namespace MSHU.CarWash.UWP.Services
{
    /// <summary>
    /// Provides a simple way to manage car wash appointments via the registered calendar app.
    /// Uses roaming settings to store IDs of existing appointments.
    /// </summary>
    class CalendarAppAppointmentService : IAppointmentService
    {
        private const string appointmentsKey = "Appointments";

        async public Task<bool> CreateAppointmentAsync(Reservation reservation)
        {
            return await CreateOrUpdateAppointmentInternalAsync(reservation);
        }

        /// <summary>
        /// Created or updates an appointment.
        /// </summary>
        /// <param name="reservation">Holds details</param>
        /// <param name="id">ID of appointment to update</param>
        /// <returns>True if successful</returns>
        async public Task<bool> CreateOrUpdateAppointmentInternalAsync(Reservation reservation, string id = null)
        {
            var appointment = new Appointment();

            appointment.Subject = "Car wash";
            appointment.StartTime = GetStartTime(reservation);
            appointment.Duration = TimeSpan.FromMinutes(10);

            appointment.Location = "Microsoft Hungary";

            var service = (ServiceEnum)reservation.SelectedServiceId;
            appointment.Details = $"<span><b>Selected service:</b> {service.GetDescription()}";

            if (!String.IsNullOrWhiteSpace(reservation.Comment))
            {
                appointment.Details += $"<br><b>Comment:</b> {reservation.Comment} ";
            }
            appointment.Details += "</span>";

            appointment.Reminder = TimeSpan.FromHours(1);
            appointment.BusyStatus = AppointmentBusyStatus.Busy;

            try
            {
                if (id == null)
                {
                    id = await AppointmentManager.ShowAddAppointmentAsync(appointment, new Rect(), Placement.Default);
                }
                else
                {
                    await AppointmentManager.ShowReplaceAppointmentAsync(id, appointment, new Rect());
                }
            }
            catch(Exception)
            {
                // AppointmentManager can actually throw for many reasons, and we can't do much about it
                return false;
            }

            if (!String.IsNullOrEmpty(id))
            {
                // success
                await StoreAppointmentInfoForReservationIDAsync(reservation.ReservationId, new AppointmentInfo(id, reservation.Date));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes an appointment.
        /// </summary>
        /// <param name="reservationID">ID of reservation for which the appointment was created</param>
        /// <returns>True if successful</returns>
        async public Task<bool> RemoveAppointmentAsync(int reservationID)
        {
            var id = await RetrieveAppointmentIDForReservationIDAsync(reservationID);

            if (id == null)
            {
                return false;
            }
            else
            {
                try
                {
                    return await AppointmentManager.ShowRemoveAppointmentAsync(id, new Rect());
                }
                catch(Exception)
                {
                    // AppointmentManager can actually throw for many reasons, and we can't do much about it
                    return false;
                }
            }
        }

        public async Task<bool> UpdateAppointmentAsync(Reservation reservation)
        {
            var id = await RetrieveAppointmentIDForReservationIDAsync(reservation.ReservationId);
            if (!String.IsNullOrEmpty(id))
            {
                return await CreateOrUpdateAppointmentInternalAsync(reservation, id);
            }

            return false;
        }

        /// <summary>
        /// Gets start time for an appointment based on reservation.
        /// </summary>
        /// <param name="reservation">Reservation details</param>
        /// <returns>Start time</returns>
        private static DateTimeOffset GetStartTime(Reservation reservation)
        {
            return new DateTimeOffset(reservation.Date).AddHours(8).AddMinutes(30);
        }

        private async Task StoreAppointmentInfoForReservationIDAsync(int reservationID, AppointmentInfo info)
        {
            var store = await GetRoamingStoreAsync();
            PurgeOldItemsInStore(store);
            
            // protect us from ilegally existing duplicate entries - delete previous
            // also useful for updates
            if (store.Keys.Contains(reservationID))
            {
                store.Remove(reservationID);
            }

            store.Add(reservationID, info);

            await SaveStore(store);
        }

        /// <summary>
        /// Saves our appointment store to RoamingSettings.
        /// </summary>
        /// <param name="store">Store to persist</param>
        private async Task SaveStore(Dictionary<int, AppointmentInfo> store)
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;

            var writer = new StringWriter();
            var serializer = new JsonSerializer();
            var jsonTextWriter = new JsonTextWriter(writer);
            serializer.Serialize(jsonTextWriter, store);
            jsonTextWriter.Flush();
            
            roamingSettings.Values[appointmentsKey] = writer.ToString();
        }

        /// <summary>
        /// We can't let old appointments lie around even if they weren't deleted.
        /// </summary>
        /// <param name="store">Store to clean up</param>
        private void PurgeOldItemsInStore(Dictionary<int, AppointmentInfo> store)
        {
            // ToList is important because we modify the original list while enumerating a derived LINQ query
            // AddDays is there to account for time zone differences
            var itemsToRemove = store.Where(pair => pair.Value.Date < DateTime.Now.Date.AddDays(-1)).ToList();
            foreach(var item in itemsToRemove)
            {
                store.Remove(item.Key);
            }
        }
        
        /// <summary>
        /// Returns the appointment store from RoamingSettings.
        /// </summary>
        /// <returns>The store</returns>
        private async static Task<Dictionary<int, AppointmentInfo>> GetRoamingStoreAsync()
        {
            var roamingSettings = ApplicationData.Current.RoamingSettings;
            var data = roamingSettings.Values[appointmentsKey] as string;

            Dictionary<int, AppointmentInfo> store = null;

            var serializer = JsonSerializer.Create();
            try
            {
                if (data != null)
                {
                    var reader = new StringReader(data);
                    store = serializer.Deserialize<Dictionary<int, AppointmentInfo>>(new JsonTextReader(reader));
                }
            }
            catch (JsonReaderException)
            {
                // just ignore invalid data
            }

            if (store == null)
            {
                store = new Dictionary<int, AppointmentInfo>();
            }

            return store;
        }

        /// <summary>
        /// Retrieves appointment ID for a reservation.
        /// </summary>
        /// <param name="reservationID">ID of reservation</param>
        /// <returns>Appointment ID or null if not found</returns>
        private async Task<string> RetrieveAppointmentIDForReservationIDAsync(int reservationID)
        {
            var store = await GetRoamingStoreAsync();
            AppointmentInfo info = null;
            if(store.TryGetValue(reservationID, out info))
            {
                store.Remove(reservationID);
            }

            await SaveStore(store);

            return info?.ID;
        }
    }
}
