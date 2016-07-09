using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSHU.CarWash.UWP.ViewModels
{
    /// <summary>
    /// ViewModel for the items in the calendar view.
    /// </summary>
    public class CalendarDayViewModel : Bindable
    {
        private bool updatePending;

        private int freeSlots;

        /// <summary>
        /// Signals if a service call is pending.
        /// </summary>
        public bool UpdatePending
        {
            get { return updatePending; }
            set
            {
                updatePending = value;
                OnPropertyChanged(nameof(UpdatePending));
            }
        }

        /// <summary>
        /// Gets or sets the number of free slots for the date represented by the
        /// current viewmodel object.
        /// </summary>
        public int FreeSlots
        {
            get
            {
                return freeSlots;
            }

            set
            {
                freeSlots = value;
                OnPropertyChanged(nameof(FreeSlots));
            }
        }

    }
}
