using Microsoft.HockeyApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.Common
{
    /// <summary>
    /// Provides diagnotics services
    /// </summary>
    static class Diagnostics
    {
        /// <summary>
        /// Reports an error and captures call stack.
        /// </summary>
        /// <param name="message">Message to add</param>
        public static void ReportError(string message)
        {
            try
            {
                // haven't found better way in UWP...
                throw new NotImplementedException();
            }
            catch(NotImplementedException e)
            {
                string newMessage = $"{message}.\nStack trace:\n{e.StackTrace}";
                HockeyClient.Current.TrackEvent(newMessage);
            }
        }
    }
}
