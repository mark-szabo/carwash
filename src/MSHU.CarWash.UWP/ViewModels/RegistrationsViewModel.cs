using MSHU.CarWash.DomainModel;
using MSHU.CarWash.DomainModel.Helpers;
using MSHU.CarWash.UWP.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace MSHU.CarWash.UWP.ViewModels
{
    public class RegistrationsViewModel : BaseViewModel
    {
        private static readonly int NUMBEROFWEEKS = 6;
        private static readonly string LIMITEXCEEDED = "You have reached the reservation limit!";

        public enum StatusValue
        {
            // For past dates and weekends
            NotAvailable,
            // For days that have free slots
            Available,
            // For days that the user has reservation on
            Reserved,
            // No more free slot
            BookedUp
        }

        /// <summary>
        /// Holds a reference to the ReservationViewModel. It is used for looking
        /// up the number of reservations for a given date.
        /// </summary>
        private ReservationViewModel _rmv;

        private object _freeSlots;

        private bool _useDetailsView;

        private ReservationDayDetailsViewModel _selectedDayDetails;
        private string _selectedDate;
        private DateTimeOffset _currentDate;
        // TODO: does this really need to be a string?
        private Dictionary<DateTime, string> _freeSlotsByDate;
        private ObservableCollection<DateTime> _requestedDates;

        private DateTimeOffset _minDate;
        private DateTimeOffset _maxDate;

        private bool _initializing;

        private DateTime _currentMonth;

        private string _feedback;

        /// <summary>
        /// Signals if go back to previous view (could be master view or page) is requested
        /// </summary>
        public event EventHandler SmartGoBackRequested;

        /// <summary>
        /// Private fields holds a reference to the reservation instance.
        /// The field has a value - not null - if the current user has
        /// a reservation for the currently selected day (SelectedDayDetails.Day).
        /// The field is null if the user has no reservation.
        /// 
        /// If the user doesn't have any reservation and he/she creates a new one
        /// then this field is assigned with the new instance of Reservation.
        /// </summary>
        private ReservationDayDetailViewModel _currentReservation;

        /// <summary>
        /// Gets or sets the RequestServiceCommand.
        /// </summary>
        public RelayCommand RequestServiceCommand { get; set; }

        /// <summary>
        /// Gets or sets the CreateReservationsCommand.
        /// </summary>
        public RelayCommand CreateReservationCommand { get; set; }

        /// <summary>
        /// Gets or sets the CancelReservationChangesCommand.
        /// </summary>
        public RelayCommand CancelReservationChangesCommand { get; set; }


        /// <summary>
        /// Gets or sets the SaveReservationChangesCommand.
        /// </summary>
        public RelayCommand SaveReservationChangesCommand { get; set; }

        /// <summary>
        /// Gets or sets the DeleteReservationCommand.
        /// </summary>
        public RelayCommand DeleteReservationCommand { get; set; }

        /// <summary>
        /// Gets or sets the UpdateReservationCommand.
        /// </summary>
        public RelayCommand UpdateReservationCommand { get; set; }

        public object FreeSlots => this;

        public bool UseDetailsView
        {
            get
            {
                return _useDetailsView;
            }

            set
            {
                _useDetailsView = value;
                OnPropertyChanged("UseDetailsView");
                OnPropertyChanged("UseMasterView");
                if (!value)
                {
                    // The user navigated back to the master view so clear the selected
                    // date relating properties!
                    SelectedDayDetails = null;
                    CurrentReservation = null;
                }
            }
        }

        public bool UseMasterView => !_useDetailsView;

        public ReservationDayDetailsViewModel SelectedDayDetails
        {
            get
            {
                return _selectedDayDetails;
            }
            private set
            {
                _selectedDayDetails = value;
            }
        }

        /// <summary>
        /// The command that activates
        /// </summary>
        public RelayCommand ActivateDetailsCommand { get; set; }

        public string SelectedDate
        {
            get
            {
                return _selectedDate;
            }
            private set
            {
                _selectedDate = value;
                OnPropertyChanged("SelectedDate");
            }
        }

        /// <summary>
        /// Private fields holds a reference to the reservation instance.
        /// The field has a value - not null - if the current user has
        /// a reservation for the currently selected day (SelectedDayDetails.Day).
        /// The field is null if the user has no reservation.
        /// 
        /// If the user doesn't have any reservation and he/she creates a new one
        /// then this field is assigned with the new instance of Reservation.
        /// </summary>
        public ReservationDayDetailViewModel CurrentReservation
        {
            get
            {
                return _currentReservation;
            }

            set
            {
                _currentReservation = value;
                OnPropertyChanged(nameof(CurrentReservation));
                OnPropertyChanged(nameof(CanCreateNewReservation));
                OnPropertyChanged(nameof(IsEditable));
                // Set the selected service
                if (_currentReservation != null)
                {
                    if (!string.IsNullOrEmpty(_currentReservation.SelectedServiceName))
                    {
                        var selectedService = Services.Find(
                            x => x.ServiceName.Equals(_currentReservation.SelectedServiceName));
                        ServicesSource.View.MoveCurrentTo(selectedService);
                    }
                    else
                    {
                        ServicesSource.View.MoveCurrentTo(
                            Services.Find(
                                x => x.ServiceId == (int)ServiceEnum.KulsoMosasBelsoTakaritas)
                                );
                    }
                    OnPropertyChanged("Services");
                    OnPropertyChanged(nameof(ServicesSource));

                }
            }
        }

        /// <summary>
        /// Helper property that is used at the visibility check for the create new reservation
        /// button.
        /// </summary>
        public bool CanCreateNewReservation
        {
            get
            {
                return CurrentReservation == null && string.IsNullOrEmpty(Feedback);
            }
        }

        /// <summary>
        /// Gets or sets the available services list.
        /// </summary>
        public List<ServiceViewModel> Services { get; set; }

        public CollectionViewSource ServicesSource { get; set; }

        /// <summary>
        /// Indicates if the current reservation can be edited.
        /// </summary>
        public bool IsEditable
        {
            get
            {
                return CurrentReservation?.IsDeletable ?? false;
            }
        }

        public DateTimeOffset MinDate
        {
            get
            {
                return _minDate;
            }

            set
            {
                _minDate = value;
                OnPropertyChanged(nameof(MinDate));
            }
        }

        public DateTimeOffset MaxDate
        {
            get
            {
                return _maxDate;
            }

            set
            {
                _maxDate = value;
                OnPropertyChanged(nameof(MaxDate));
            }
        }

        /// <summary>
        /// Gets or sets the PreviousPeriodCommand.
        /// </summary>
        public RelayCommand PreviousPeriodCommand { get; set; }

        /// <summary>
        /// Gets or sets the PreviousPeriodCommand.
        /// </summary>
        public RelayCommand NextPeriodCommand { get; set; }

        public string Feedback
        {
            get
            {
                return _feedback;
            }

            set
            {
                _feedback = value;
                OnPropertyChanged(nameof(Feedback));
                OnPropertyChanged(nameof(CanCreateNewReservation));
            }
        }

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
        private bool updatePending;

        /// <summary>
        /// Helper property for the status feedback on the daily level.
        /// </summary>
        public object DayStatus => this;

        public RegistrationsViewModel()
        {
            UseDetailsView = false;

            // Create and execute the RequestService command.
            RequestServiceCommand = new RelayCommand(ExecuteRequestServiceCommand);
            RequestServiceCommand.Execute(this);

            // Create the ActivateDetailsCommand.
            ActivateDetailsCommand = new RelayCommand(ExecuteActivateDetailsCommand,
                (Func<object, bool>)CanExecuteActivateDetailsCommand);

            // Create the CreateReservationCommand.
            CreateReservationCommand = new RelayCommand(ExecuteCreateReservationCommand,
                async param => await CanExecuteCreateReservationCommand(param));

            // Create the CancelReservationChangesCommand.
            CancelReservationChangesCommand = new RelayCommand(ExecuteCancelReservationChangesCommand);

            // Create the SaveReservationChangesCommand.
            SaveReservationChangesCommand = new RelayCommand(ExecuteSaveReservationChangesCommand);

            // Create the DeleteReservationCommand.
            DeleteReservationCommand = new RelayCommand(ExecuteDeleteReservationCommand,
                (Func<object, bool>)CanExecuteDeleteReservationCommand);

            // Create the UpdateReservationChangesCommand.
            UpdateReservationCommand = new RelayCommand(ExecuteUpdateReservationChangesCommand);

            // Create the PreviousPeriodCommand.
            PreviousPeriodCommand = new RelayCommand(ExecutePreviousPeriodCommand);

            // Create the NextPeriodCommand.
            NextPeriodCommand = new RelayCommand(ExecuteNextPeriodCommand);

            Services = GetAvailableServices();

            ServicesSource = new CollectionViewSource();
            ServicesSource.Source = Services;

            // Subscribe to the PropertyChanged event. If the CurrentReservation
            // property changes we must fire the CanExecuteChanged event on the
            // DeleteReservationCommand otherwise the UWP's CommandManager doesn't
            // reevaluate if the DeleteReservationCommand can be executed.
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(CurrentReservation))
                {
                    DeleteReservationCommand.RaiseCanExecuteChanged();
                }
            };

            _freeSlotsByDate = new Dictionary<DateTime, string>();

            DateTime now = DateTime.Now.Date;
            _currentMonth = new DateTime(now.Year, now.Month, 1);
            GetFreeSlotNumberForTimeInterval(now.AddDays(-7 * NUMBEROFWEEKS), now.AddDays(7 * 2 * NUMBEROFWEEKS));

            _initializing = true;
            _requestedDates = new ObservableCollection<DateTime>();
            _requestedDates.CollectionChanged += RequestedDatesCollectionChanged;
        }

        private bool CanExecuteActivateDetailsCommand(object arg)
        {
            DateTimeOffset selectedDate = (DateTimeOffset)arg;

            // don't allow past dates
            if (selectedDate.Date < DateTime.Now.Date)
            {
                return false;
            }

            // make sure there's no update
            if (UpdatePending)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a list of available services, based on parameters.
        /// </summary>
        /// <param name="date">Date (optional)</param>
        /// <param name="freeNormalSlots">Free slots (optional)</param>
        /// <returns>Available services</returns>
        private List<ServiceViewModel> GetAvailableServices(DateTime? date = null, int freeNormalSlots = 2)
        {
            var result = new List<ServiceViewModel>();
            result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosas, ServiceName = ServiceEnum.KulsoMosas.GetDescription(), Selected = false });
            result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.BelsoTakaritas, ServiceName = ServiceEnum.BelsoTakaritas.GetDescription(), Selected = false });
            result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritas.GetDescription(), Selected = false });
            if (freeNormalSlots > 1)
            {
                result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritasKarpittisztitas.GetDescription(), Selected = false });
            }

            if (date == null || date?.DayOfWeek == DayOfWeek.Tuesday)
            {
                result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasGozos, ServiceName = ServiceEnum.KulsoMosasGozos.GetDescription(), Selected = false });
                result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.BelsoTakaritasGozos, ServiceName = ServiceEnum.BelsoTakaritasGozos.GetDescription(), Selected = false });
                result.Add(new ServiceViewModel { ServiceId = (int)ServiceEnum.KulsoMosasBelsoTakaritasGozos, ServiceName = ServiceEnum.KulsoMosasBelsoTakaritasGozos.GetDescription(), Selected = false });
            }
            return result;
        }

        private async void RequestedDatesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_initializing && e.NewItems.Count == 1)
            {
                DateTime requestedDay = (DateTime)e.NewItems[0];
                requestedDay = requestedDay.Date;
                int? slots = await ServiceClient.ServiceClient.GetCapacityByDay(
                    requestedDay,
                    App.AuthenticationManager.BearerAccessToken);
                if (slots.HasValue)
                {
                    if (_freeSlotsByDate.ContainsKey(requestedDay))
                    {
                        _freeSlotsByDate[requestedDay] = slots.ToString();
                    }
                    else
                    {
                        _freeSlotsByDate.Add(requestedDay, slots.ToString());
                    }
                }
                _requestedDates.Remove(requestedDay);
                OnPropertyChanged(nameof(FreeSlots));
                OnPropertyChanged(nameof(DayStatus));
            }
        }


        /// <summary>
        /// Retrieves the number of reservations for the given date.
        /// </summary>
        /// <param name="date">The date the number of reservations requested for.</param>
        /// <returns>Number of reservations.</returns>
        public string GetReservationCountByDay(DateTimeOffset date)
        {
            string result = "?";

            if (_freeSlotsByDate.ContainsKey(date.Date))
            {
                result = _freeSlotsByDate[date.Date];
            }
            else
            {
                if (!_initializing)
                {
                    //_requestedDates.Add(date.Date);
                }
            }

            return result;
        }

        private int GetReservationCountFromList(DateTimeOffset date, List<ReservationDayDetailsViewModel> list)
        {
            int number;
            // Let's check if there are reservations for the specified day at all!
            var day = list.Find(x => x.Day.Equals(date.Date));
            if (day == null)
            {
                // No reservations.
                number = 0;
            }
            else
            {
                // Get the number of reservations.
                var reservations = day.Reservations.Count;
                number = reservations;
            }

            return number;
        }

        private async Task GetFreeSlotNumberForTimeInterval(DateTime start, DateTime end)
        {
            UpdatePending = true;
            List<int> availableSlots = await ServiceClient.ServiceClient.GetCapacityForTimeInterval(
                start,
                end,
                App.AuthenticationManager.BearerAccessToken);
            DateTime key = start;
            for (int index = 0; index < availableSlots?.Count; index++)
            {
                // Update the dictionary
                if (_freeSlotsByDate.ContainsKey(key))
                {
                    _freeSlotsByDate[key] = availableSlots[index].ToString();
                }
                else
                {
                    _freeSlotsByDate.Add(key, availableSlots[index].ToString());
                }
                // Increment the index
                key = key.AddDays(1);
            }
            UpdatePending = false;
            OnPropertyChanged(nameof(FreeSlots));
            OnPropertyChanged(nameof(DayStatus));
            _initializing = false;
        }

        public async Task GetCapacityByDay(DateTime day)
        {
            int? availableSlots = await ServiceClient.ServiceClient.GetCapacityByDay(
                day,
                App.AuthenticationManager.BearerAccessToken);
            if (availableSlots.HasValue)
            {
                _freeSlotsByDate[day] = availableSlots.ToString();
            }
        }

        /// <summary>
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteRequestServiceCommand(object param)
        {
            _rmv = await ServiceClient.ServiceClient.GetReservations(
                App.AuthenticationManager.BearerAccessToken);
            if (_rmv != null)
            {
                OnPropertyChanged("FreeSlots");
            }
        }

        /// <summary>
        /// Event handler for the Executed event of the RequestServiceCommand.
        /// The handler sets the UseDetailsView property to true and also sets
        /// the SelectedDayDetails property, correspondingly. The latter is used
        /// for data binding UI elements.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteActivateDetailsCommand(object param)
        {
            DateTimeOffset selectedDate = (DateTimeOffset)param;
            if (selectedDate.CompareTo(DateTimeOffset.Now.Date) < 0)
            {
                SelectedDayDetails =
                    _rmv.ReservationsByDayHistory.Find(x => x.Day.Equals(selectedDate.Date));
            }
            else
            {
                SelectedDayDetails =
                    _rmv.ReservationsByDayActive.Find(x => x.Day.Equals(selectedDate.Date));
            }
            SelectedDate = selectedDate.ToString("D");
            _currentDate = selectedDate;

            // re-fresh services based on date and free slots
            Services = GetAvailableServices(_currentDate.Date, Convert.ToInt32(_freeSlotsByDate[_currentDate.Date]));
            ServicesSource.Source = Services;

            if (SelectedDayDetails != null)
            {
                // Check if the user has reservation for the selected date.
                CurrentReservation = SelectedDayDetails.Reservations.Find(
                    x => x.EmployeeId.Equals(App.AuthenticationManager.UserData.DisplayableId));
                // If the user doesn't have reservation for the selected date then let's find out
                // if the user can create a new one.
                if (CurrentReservation == null)
                {
                    await CreateNewReservationIfPossible(CreateReservationCommand, selectedDate, this);
                }
            }
            else
            {
                // If there's no reservation for the selected date then let's find out
                // if the user can create a new one.
                if (CurrentReservation == null)
                {
                    await CreateNewReservationIfPossible(CreateReservationCommand, selectedDate, this);
                }
            }

            UseDetailsView = true;
        }

        private static async Task CreateNewReservationIfPossible(
            RelayCommand createReservationCommand,
            DateTimeOffset selectedDate,
            RegistrationsViewModel vm)
        {
            bool? reservationAvailable =
                await ServiceClient.ServiceClient.NewReservationAvailable(App.AuthenticationManager.BearerAccessToken);
            if (reservationAvailable.HasValue && reservationAvailable.Value)
            {
                bool canExecute = await createReservationCommand.CanExecuteAsync(selectedDate);
                // Execute the C
                if (canExecute)
                {
                    createReservationCommand.Execute(selectedDate);
                }
            }
            else
            {
                vm.Feedback = LIMITEXCEEDED;
            }
        }

        private async Task<bool> CanExecuteCreateReservationCommand(object param)
        {
            bool result = false;
            bool? canCreateNewReservation = await ServiceClient.ServiceClient.NewReservationAvailable(
                App.AuthenticationManager.BearerAccessToken);
            if (CurrentReservation == null && canCreateNewReservation.HasValue &&
                canCreateNewReservation.Value)
            {
                result = true;
            }
            return result;
        }

        private async void ExecuteCreateReservationCommand(object param)
        {
            var cr = new ReservationDayDetailViewModel();
            cr.VehiclePlateNumber = App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber;
            if (param is DateTime)
            {
                var date = (DateTime)param;
                _currentDate = new DateTimeOffset(date);
                await GetFreeSlotNumberForTimeInterval(date.Date, date.Date);
            }
            cr.IsDeletable = true;

            // re-fresh services based on date and free slots
            Services = GetAvailableServices(_currentDate.Date, Convert.ToInt32(_freeSlotsByDate[_currentDate.Date]));
            ServicesSource.Source = Services;

            CurrentReservation = cr;
        }

        private void ExecuteCancelReservationChangesCommand(object param)
        {
            CurrentReservation = null;
            SmartGoBackRequested?.Invoke(this, null);
        }

        private async void ExecuteSaveReservationChangesCommand(object param)
        {
            NewReservationViewModel nrvm = new NewReservationViewModel();
            nrvm.EmployeeId = App.AuthenticationManager.UserData.DisplayableId;
            nrvm.EmployeeName = App.AuthenticationManager.CurrentEmployee.Name;
            nrvm.VehiclePlateNumber = CurrentReservation.VehiclePlateNumber;
            nrvm.SelectedServiceId = ((ServiceViewModel)ServicesSource.View.CurrentItem).ServiceId;
            nrvm.Comment = CurrentReservation.Comment;
            //nrvm.SelectedServiceId = ServicesSource.View.CurrentPosition;
            nrvm.Date = _currentDate.Date;
            bool result = await ServiceClient.ServiceClient.SaveReservation(
                nrvm,
                App.AuthenticationManager.BearerAccessToken);
            if (result)
            {
                // Update the slot number cache.
                await UpdateSlotNumberCache(_currentDate.Date);

                // If the user has changed the plate number then refresh the CurrentEmployee instance 
                // at the AuthenticationManager.
                if (CurrentReservation.VehiclePlateNumber != App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber)
                {
                    App.AuthenticationManager.RefreshCurrentUser();
                }
                // Refresh the ReservationViewModel reference.
                _rmv = await ServiceClient.ServiceClient.GetReservations(
                    App.AuthenticationManager.BearerAccessToken);
                if (_rmv != null)
                {
                    OnPropertyChanged(nameof(FreeSlots));
                    OnPropertyChanged(nameof(DayStatus));

                    // request subscribing view to do a smart go back
                    SmartGoBackRequested?.Invoke(this, null);
                }
            }
        }

        private async Task UpdateSlotNumberCache(DateTime date)
        {
            int? count = await ServiceClient.ServiceClient.GetCapacityByDay(
                date,
                App.AuthenticationManager.BearerAccessToken);
            if (count.HasValue)
            {
                _freeSlotsByDate[date] = count.Value.ToString();
            }
        }

        private bool CanExecuteDeleteReservationCommand(object param)
        {
            return CurrentReservation?.IsDeletable ?? false;
        }

        /// <summary>
        /// Event handler for the DeleteReservationCommand.
        /// It deletes the currently selected reservation at the service
        /// and resets properties that the user interface can use to
        /// reflect current state.
        /// </summary>
        /// <param name="param"></param>
        private async void ExecuteDeleteReservationCommand(object param)
        {
            bool result = await ServiceClient.ServiceClient.DeleteReservation(
                CurrentReservation.ReservationId,
                App.AuthenticationManager.BearerAccessToken);
            if (result)
            {
                // Find the reservations made for the selected date.
                ReservationDayDetailsViewModel reservationByCurrentDate =
                    _rmv.ReservationsByDayActive.Find(x => x.Day == _currentDate.Date);
                // Remove the current reservation.
                reservationByCurrentDate.Reservations.RemoveAll(
                    x => x.ReservationId == _currentReservation.ReservationId);

                // Update the slot number cache.
                await UpdateSlotNumberCache(_currentDate.Date);

                // Reset the current reservation instance.
                CurrentReservation = null;
                // Let the user interface update the number of free slots on the
                // master view.
                OnPropertyChanged(nameof(FreeSlots));
                OnPropertyChanged(nameof(DayStatus));
                this.Feedback = string.Empty;
                UseDetailsView = false;
            }
        }

        private async void ExecuteUpdateReservationChangesCommand(object param)
        {
            Reservation updatedReservation = new Reservation();
            updatedReservation.Comment = CurrentReservation.Comment;
            string id = App.AuthenticationManager.CurrentEmployee.EmployeeId;
            updatedReservation.CreatedBy = CurrentReservation.EmployeeId;
            updatedReservation.CreatedOn = DateTime.UtcNow;
            updatedReservation.Date = _currentDate.Date;
            updatedReservation.EmployeeId = CurrentReservation.EmployeeId;
            updatedReservation.ReservationId = CurrentReservation.ReservationId;
            updatedReservation.SelectedServiceId = ((ServiceViewModel)ServicesSource.View.CurrentItem).ServiceId;
            updatedReservation.VehiclePlateNumber = CurrentReservation.VehiclePlateNumber;

            // Update the reservation on the service
            bool result = await ServiceClient.ServiceClient.UpdateReservation(
                updatedReservation,
                App.AuthenticationManager.BearerAccessToken);
            if (result)
            {
                // Update the slot number cache.
                await UpdateSlotNumberCache(_currentDate.Date);

                // If the user has changed the plate number then refresh the CurrentEmployee instance 
                // at the AuthenticationManager.
                if (CurrentReservation.VehiclePlateNumber != App.AuthenticationManager.CurrentEmployee.VehiclePlateNumber)
                {
                    App.AuthenticationManager.RefreshCurrentUser();
                }
                // Refresh the ReservationViewModel reference.
                _rmv = await ServiceClient.ServiceClient.GetReservations(
                    App.AuthenticationManager.BearerAccessToken);
                if (_rmv != null)
                {
                    OnPropertyChanged(nameof(FreeSlots));
                    OnPropertyChanged(nameof(DayStatus));

                    // request subscribing view to do a smart go back
                    SmartGoBackRequested?.Invoke(this, null);
                }
            }
        }

        /// <summary>
        /// Handles the Executed event of the PreviousPeriodCommand.
        /// Actually, it loads the number of free slots for the previous
        /// time period.
        /// </summary>
        /// <param name="param"></param>
        private void ExecutePreviousPeriodCommand(object param)
        {
            _currentMonth = _currentMonth.AddMonths(-1);
            var lastDayOfMonth = _currentMonth.AddMonths(1).AddDays(-1);
            GetFreeSlotNumberForTimeInterval(_currentMonth, lastDayOfMonth);
        }

        /// <summary>
        /// Handles the Executed event of the NextPeriodCommand.
        /// Actually, it loads the number of free slots for the previous
        /// time period.
        /// </summary>
        /// <param name="param"></param>
        private void ExecuteNextPeriodCommand(object param)
        {
            _currentMonth = _currentMonth.AddMonths(1);
            var lastDayOfMonth = _currentMonth.AddMonths(1).AddDays(-1);
            GetFreeSlotNumberForTimeInterval(_currentMonth, lastDayOfMonth);
        }

        public StatusValue GetStatus(DateTimeOffset date)
        {
            StatusValue result = StatusValue.NotAvailable;

            // Check if the date is in the past.
            if (date < DateTimeOffset.Now.Date)
            {
                result = StatusValue.NotAvailable;
            }
            // Check if the date is on a weekend.
            else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                result = StatusValue.NotAvailable;
            }
            // Check if there are available slots.
            else
            {
                // Check if the user has a reservation on the given day.
                //if (_rmv.ReservationsByDayActive.)
                bool hasReservation = false;
                if (_rmv?.ReservationsByDayActive != null)
                {
                    ReservationDayDetailsViewModel selectedDayDetails =
                            _rmv.ReservationsByDayActive.Find(x => x.Day.Equals(date.Date));
                    if (selectedDayDetails != null)
                    {
                        // Check if the user has reservation for the selected date.
                        ReservationDayDetailViewModel currentReservation =
                            selectedDayDetails.Reservations.Find(
                                x => x.EmployeeId.Equals(App.AuthenticationManager.UserData.DisplayableId));
                        // If the user has reservation for the selected date then we return
                        // StatusValue.Reserved
                        if (currentReservation != null)
                        {
                            hasReservation = true;
                            result = StatusValue.Reserved;
                        }
                    }
                }

                if (!hasReservation)
                {
                    // If we don't have the number of available slots for the given date
                    // we return StatusValue.NotAvailable
                    if (!_freeSlotsByDate.ContainsKey(date.Date))
                    {
                        result = StatusValue.NotAvailable;
                    }
                    else
                    {
                        int slotCount;
                        bool success = int.TryParse(_freeSlotsByDate[date.Date], out slotCount);
                        if (success)
                        {
                            // If we have at least 1 free slot...
                            if (slotCount > 0)
                            {
                                result = StatusValue.Available;
                            }
                            else
                            {
                                // No more slots...
                                result = StatusValue.BookedUp;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
