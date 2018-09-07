using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.Interfaces
{
    interface ISettingsStore
    {
        Task StoreSettingAsync<T>(string name, T setting);
        Task<T> TryRetrieveSettingAsync<T>(string name);
    }
}
