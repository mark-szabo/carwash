using Microsoft.IdentityModel.Clients.ActiveDirectory;
using MSHU.CarWash.DomainModel;
using MSHU.CarWash.UWP.Views;
using System;
using System.Globalization;
using System.Text;
using Windows.ApplicationModel;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private string _givenName;
        private string _familyName;
        private string _displayableID;
        private string m_RegistrationInfo;

        /// <summary>
        /// Indicates if user has a resevartion
        /// </summary>
        public bool ReservationAvailable { get
            {
                return reservationAvailable;
            }
            set
            {
                reservationAvailable = value;
                OnPropertyChanged(nameof(ReservationAvailable));
            }
        }
        private bool reservationAvailable;

        /// <summary>
        /// Holds car's numberplate (of first reservation)
        /// </summary>
        public string NumberPlate { get
            {
                return numberPlate;
            }
            set
            {
                numberPlate = value;
                OnPropertyChanged(nameof(NumberPlate));
            }
        }
        private string numberPlate;

        /// <summary>
        /// Holds date of first reservation
        /// </summary>
        public string ReservationDateString
        {
            get
            {
                return reservationDateString;
            }
            set
            {
                reservationDateString = value;
                OnPropertyChanged(nameof(ReservationDateString));
            }
        }
        private string reservationDateString;

        /// <summary>
        /// Holds next reservation
        /// </summary>
        private ReservationDayDetailViewModel upcomingReservation { get; set; }

        /// <summary>
        /// Holds textual representation of next free slot's date
        /// </summary>
        public string NextFreeSlotDateString
        {
            get
            {
                return nextFreeSlotDateString;

            }
            set
            {
                nextFreeSlotDateString = value;
                OnPropertyChanged(nameof(NextFreeSlotDateString));
            }
        }
        private string nextFreeSlotDateString;

        /// <summary>
        /// Actual date for next free slot
        /// </summary>
        private DateTime? nextFreeSlotDate;

        /// <summary>
        /// Is a next free slot available at all?
        /// </summary>
        public bool NextFreeSlotAvailable
        {
            get { return nextFreeSlotAvailable; }
            set
            {
                nextFreeSlotAvailable = value;
                OnPropertyChanged(nameof(NextFreeSlotAvailable));
            }
        }
        private bool nextFreeSlotAvailable;

        /// <summary>
        /// Gets the given name of the user.
        /// </summary>
        public string GivenName
        {
            get
            {
                return _givenName;
            }
            set
            {
                _givenName = value;
                OnPropertyChanged(nameof(GivenName));
            }
        }

        /// <summary>
        /// Gets the family name of the user.
        /// </summary>
        public string FamilyName
        {
            get
            {
                return _familyName;
            }
            private set
            {
                _familyName = value;
                OnPropertyChanged(nameof(FamilyName));
            }
        }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public string Email
        {
            get
            {
                return _displayableID;
            }
            private set
            {
                _displayableID = value;
                OnPropertyChanged(nameof(Email));
            }
        }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public string RegistrationInfo
        {
            get
            {
                return this.m_RegistrationInfo;
            }
            private set
            {
                this.m_RegistrationInfo = value;
                OnPropertyChanged(nameof(RegistrationInfo));
            }
        }

        /// <summary>
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

        /// <summary>
        /// Gets or sets the DeleteReservationCommand.
        /// </summary>
        public RelayCommand DeleteReservationCommand { get; set; }

        /// <summary>
        /// Gets or sets the GetNextFreeSlotCommand.
        /// </summary>
        public RelayCommand GetNextFreeSlotCommand { get; set; }

        /// <summary>
        /// Gets or sets the QuickReserveCommand.
        /// </summary>
        public RelayCommand QuickReserveCommand { get; set; }

        private bool showQuickReservationSuccess;
        public bool ShowQuickReservationSuccess
        {
            get { return showQuickReservationSuccess; }
            set
            {
                showQuickReservationSuccess = value;
                OnPropertyChanged(nameof(ShowQuickReservationSuccess));
            }
        }

        public bool ShowReservationLimitReached
        {
            get
            {
                // we depend on two other properties, so let's subscribe to them
                if (!showReservationLimitReachedSetup)
                {
                    PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(ShowQuickReservationSuccess))
                        {
                            OnPropertyChanged(nameof(ShowReservationLimitReached));
                        }
                    };
                    showReservationLimitReachedSetup = true;
                }
                return showReservationLimitReached && !ShowQuickReservationSuccess;
            }
            set
            {
                showReservationLimitReached = value;
                OnPropertyChanged(nameof(ShowReservationLimitReached));
            }
        }
        private bool showReservationLimitReached;
        private bool showReservationLimitReachedSetup;

        public bool ShowQuickReservationControls
        {
            get
            {
                // we depend on two other properties, so let's subscribe to them
                if(!showQuickReservationControlsSetup)
                {
                    PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(ShowReservationLimitReached) ||
                            args.PropertyName == nameof(ShowReservationLimitReached))
                        {
                            OnPropertyChanged(nameof(ShowQuickReservationControls));
                        }
                    };
                    showQuickReservationControlsSetup = true;
                }
                return !ShowReservationLimitReached && !ShowQuickReservationSuccess;
            }
        }
        private bool showQuickReservationControlsSetup;


        /// <summary>
        /// Gets or sets the QuickReserveCommand.
        /// </summary>
        public RelayCommand QuickReserveExtraCommand { get; set; }


        /// <summary>
        /// Default constructor initializes instance state.
        /// </summary>
        public HomeViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                GivenName = "Béla";
                FamilyName = "Példa";
                Email = "bpelda@microsoft.com";

                ReservationAvailable = true;
                reservationDateString = (DateTime.Now + TimeSpan.FromDays(1)).ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
                NumberPlate = "MS-0001";
                nextFreeSlotDateString = (DateTime.Now + TimeSpan.FromDays(4)).ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
                nextFreeSlotAvailable = true;

                ShowQuickReservationSuccess = false;
                return;
            }
            if (App.AuthenticationManager.IsUserAuthenticated)
            {
                UserInfo info = App.AuthenticationManager.UserData;
                GivenName = info.GivenName;
                FamilyName = info.FamilyName;
                Email = info.DisplayableId;

            }

            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);

            GetNextFreeSlotCommand = new RelayCommand(HandleGetNextFreeSlotCommand);
            GetNextFreeSlotCommand.Execute(this);

            QuickReserveCommand = new RelayCommand(HandleQuickReserveCommand);
            QuickReserveExtraCommand = new RelayCommand(HandleQuickReserveExtraCommand);

            DeleteReservationCommand = new RelayCommand(HandleDeleteReservationCommand, o => ReservationAvailable && upcomingReservation.IsDeletable);
            // make sure we get notified about changes in our dependency (ReservationAvailable)
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ReservationAvailable))
                {
                    DeleteReservationCommand.RaiseCanExecuteChanged();
                }
            };
        }

        private async void HandleDeleteReservationCommand(object obj)
        {
            bool result = await ServiceClient.ServiceClient.DeleteReservation(upcomingReservation.ReservationId, App.AuthenticationManager.BearerAccessToken);
            ReservationAvailable = false;
            GetNextFreeSlotCommand.Execute(null);
            RequestServiceCommand.Execute(null);

            // in case we still had the success feedback there - let's remove it to make
            // a new quick reservation possible
            ShowQuickReservationSuccess = false;
        }

        private void HandleQuickReserveExtraCommand(object obj)
        {
            AppShell.Current.AppFrame.Navigate(typeof(ReservationsPage), nextFreeSlotDate.Value);
        }

        private async void HandleQuickReserveCommand(object obj)
        {
            var reservation = new NewReservationViewModel()
            {
                VehiclePlateNumber = App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber,
                EmployeeId = App.AuthenticationManager.UserData.DisplayableId,
                EmployeeName = App.AuthenticationManager.CurrentEmployee.Name,
                // TODO: this is weird here... need to fix the domain model
                SelectedServiceId = new Nullable<int>((int)ServiceEnum.KulsoMosasBelsoTakaritas),
                Date = nextFreeSlotDate.Value
            };

            bool result = await ServiceClient.ServiceClient.SaveReservation(reservation, App.AuthenticationManager.BearerAccessToken);
            if(result)
            {
                ShowQuickReservationSuccess = true;
                RequestServiceCommand.Execute(null);
                GetNextFreeSlotCommand.Execute(null);
            }
        }

        private async void HandleGetNextFreeSlotCommand(object param)
        {
            nextFreeSlotDate = await ServiceClient.ServiceClient.GetNextFreeSlotDate(App.AuthenticationManager.BearerAccessToken);
            if (nextFreeSlotDate.HasValue)
            {
                NextFreeSlotDateString = GetSmartDateString(nextFreeSlotDate.Value);
                NextFreeSlotAvailable = true;
            }
            else
            {
                NextFreeSlotDateString = "No free slots found";
                NextFreeSlotAvailable = false;
            }
        }

        /// <summary>
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteRequestServiceCommand(object param)
        {
            ReservationViewModel result = await
               ServiceClient.ServiceClient.GetReservations(App.AuthenticationManager.BearerAccessToken);

            if (result != null)
            {
                StringBuilder builder = new StringBuilder();
                if (result.ReservationsByDayActive.Count > 0)
                {
                    NumberPlate = result.ReservationsByDayActive[0].Reservations[0].VehiclePlateNumber;
                    ReservationDateString = GetSmartDateString(result.ReservationsByDayActive[0].Day);
                    upcomingReservation = result.ReservationsByDayActive[0].Reservations[0];
                    if (result.ReservationsByDayActive.Count >= 2)
                    {
                        ShowReservationLimitReached = true;
                    }
                    else
                    {
                        ShowReservationLimitReached = false;
                    }
                    ReservationAvailable = true;
                }
            }

        }

        private string GetSmartDateString(DateTime date)
        {
            var dateString = date.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
            var days = (date.Date - DateTime.Now.Date).Days;
            if (days == 0)
            {
                return String.Concat(dateString, " (today)");
            }
            if (days == 1)
            {
                return String.Concat(dateString, " (tomorrow)");
            }
            if (days < 7)
            {
                return String.Concat(dateString, $" ({date.DayOfWeek})");
            }

            return dateString;
        }
    }
}
