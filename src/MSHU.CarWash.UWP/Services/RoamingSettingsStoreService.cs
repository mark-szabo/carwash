using MSHU.CarWash.UWP.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MSHU.CarWash.UWP.Services
{
    class RoamingSettingsStoreService : ISettingsStore
    {
        public Task<T> TryRetrieveSettingAsync<T>(string name)
        {
            object value;
            ApplicationData.Current.RoamingSettings.Values.TryGetValue(name, out value);
            return Task.FromResult((T)value);
        }

        public Task StoreSettingAsync<T>(string name, T setting)
        {
            ApplicationData.Current.RoamingSettings.Values[name] = setting;
            return Task.CompletedTask;
        }
    }
}
