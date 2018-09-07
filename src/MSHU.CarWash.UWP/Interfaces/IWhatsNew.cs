using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.Interfaces
{
    interface IWhatsNew
    {
        Task FetchWhatsNewAsync();
        Task<bool> IsSomethingNewAsync();
        Task ShowWhatsNewAsync();
    }
}
