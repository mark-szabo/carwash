using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.Interfaces
{
    interface ISettingsStoreService
    {
        Task StoreSettingAsync<T>(string name, T setting);
        Task<T> RetriveSettingAsync<T>(string name);
    }
}
