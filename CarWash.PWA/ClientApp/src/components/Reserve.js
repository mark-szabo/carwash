import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import { Redirect } from 'react-router';
import apiFetch from '../Auth';
import { withStyles } from '@mui/styles';
import Alert from '@mui/material/Alert';
import Autocomplete from '@mui/material/Autocomplete';
import Stepper from '@mui/material/Stepper';
import Step from '@mui/material/Step';
import StepLabel from '@mui/material/StepLabel';
import StepContent from '@mui/material/StepContent';
import Button from '@mui/material/Button';
import Chip from '@mui/material/Chip';
import Typography from '@mui/material/Typography';
import Radio from '@mui/material/Radio';
import RadioGroup from '@mui/material/RadioGroup';
import FormGroup from '@mui/material/FormGroup';
import Checkbox from '@mui/material/Checkbox';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormControl from '@mui/material/FormControl';
import TextField from '@mui/material/TextField';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterMoment } from '@mui/x-date-pickers/AdapterMoment';
import { DateCalendar } from '@mui/x-date-pickers/DateCalendar';
import CloudOffIcon from '@mui/icons-material/CloudOff';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';
import Grid from '@mui/material/Grid';
import * as moment from 'moment';
import { Service, NotificationChannel, getServiceName } from '../Constants';
import './Reserve.css';
import ServiceDetailsTable from './ServiceDetailsTable';
import Spinner from './Spinner';
import LocationSelector from './LocationSelector';

const styles = theme => ({
    stepper: {
        padding: 0,
        backgroundColor: 'inherit',
    },
    button: {
        marginTop: theme.spacing(1),
        marginRight: theme.spacing(1),
    },
    actionsContainer: {
        marginTop: theme.spacing(1),
        marginBottom: theme.spacing(2),
    },
    resetContainer: {
        padding: theme.spacing(3),
    },
    chip: {
        margin: theme.spacing(0.5),
    },
    selectedChip: {
        margin: theme.spacing(0.5),
        backgroundColor: theme.palette.primary.main,
        '&:hover': {
            backgroundColor: theme.palette.primary.dark,
        },
        '&:focus': {
            backgroundColor: theme.palette.primary.main,
        },
        '&:hover:focus': {
            backgroundColor: theme.palette.primary.dark,
        },
    },
    chipGroupTitle: {
        marginTop: theme.spacing(0.5),
    },
    calendar: {
        maxWidth: '400px',
        margin: 0,
    },
    radioGroup: {
        margin: `${theme.spacing(1)} 0`,
    },
    container: {
        display: 'flex',
        flexWrap: 'wrap',
    },
    textField: {
        marginLeft: theme.spacing(1),
        marginRight: theme.spacing(1),
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 200,
        },
    },
    checkbox: {
        marginLeft: theme.spacing(1),
        marginRight: theme.spacing(1),
    },
    formControl: {
        margin: `${theme.spacing(2)} ${theme.spacing(1)}`,
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 200,
        },
    },
    center: {
        display: 'grid',
        placeItems: 'center',
        textAlign: 'center',
        height: '80%',
    },
    errorIcon: {
        margin: theme.spacing(1),
        color: '#BDBDBD',
        width: '100px',
        height: '100px',
    },
    errorText: {
        color: '#9E9E9E',
    },
    infoAlert: {
        width: 'fit-content',
        margin: `${theme.spacing(1)} 0`,
    },
});

class Reserve extends TrackedComponent {
    displayName = 'Reserve';
    isUpdate = false;

    constructor(props) {
        super(props);
        this.state = {
            activeStep: 0,
            notAvailableDates: [],
            notAvailableTimes: [],
            loading: true,
            loadingReservation: false,
            reservationCompleteRedirect: false,
            selectedServices: [],
            validationErrors: {
                vehiclePlateNumber: false,
                garage: false,
                floor: false,
            },
            selectedDate: null,
            vehiclePlateNumber: '',
            garage: '',
            floor: '',
            seat: '',
            private: false,
            comment: '',
            disabledSlots: [],
            reservationPercentage: [],
            reservationPercentageDataArrived: false,
            users: [],
            userId: this.props.user.id,
            servicesStepLabel: 'Select services',
            dateStepLabel: 'Choose date',
            timeStepLabel: 'Choose time',
            locationKnown: false,
            dropoffPreConfirmed: false,
            dateSelected: false,
            timeSelected: false,
        };
    }

    componentDidMount() {
        super.componentDidMount();

        if (this.props.match.params.id) {
            this.isUpdate = true;
            this.setState({
                loadingReservation: true,
            });
            apiFetch(`api/reservations/${this.props.match.params.id}`).then(
                data => {
                    let garage;
                    let floor;
                    let seat;
                    if (data.location) {
                        [garage, floor, seat] = data.location.split('/');
                    }

                    garage = garage || '';
                    floor = floor || '';
                    seat = seat || '';

                    const date = moment(data.startDate);
                    this.setState({
                        selectedServices: data.services,
                        selectedDate: date,
                        vehiclePlateNumber: data.vehiclePlateNumber,
                        garage,
                        floor,
                        seat,
                        private: data.private,
                        comment: data.comment,
                        servicesStepLabel: data.services
                            .map(s => getServiceName(this.props.configuration, s))
                            .join(', '),
                        dateStepLabel: date.format('MMMM D, YYYY'),
                        timeStepLabel: date.format('hh:mm A'),
                        loadingReservation: false,
                        dateSelected: true,
                        timeSelected: true,
                    });
                },
                error => {
                    this.setState({ loading: false });
                    this.props.openSnackbar(error);
                }
            );
        } else {
            const lastSelectedServices = this.props.lastSettings.services || [];

            // if exterior, should include prewash and wheel cleaning too
            if (lastSelectedServices.includes(Service.Exterior)) {
                this.setServiceSelection(lastSelectedServices, Service.Prewash, true);
                this.setServiceSelection(lastSelectedServices, Service.WheelCleaning, true);
            }

            this.setState({
                selectedServices: lastSelectedServices,
                vehiclePlateNumber: this.props.lastSettings.vehiclePlateNumber || '',
                garage: this.props.lastSettings.garage || '',
            });
        }

        if (this.props.user.isCarwashAdmin) {
            this.setState({
                loading: false,
            });
        } else {
            apiFetch('api/reservations/notavailabledates').then(
                data => {
                    const { dates, times } = data;
                    for (const i in dates) {
                        if (dates.hasOwnProperty(i)) {
                            dates[i] = moment(dates[i]).toDate();
                        }
                    }
                    for (const i in times) {
                        if (times.hasOwnProperty(i)) {
                            times[i] = moment(times[i]);
                        }
                    }
                    this.setState({
                        notAvailableDates: dates,
                        notAvailableTimes: times,
                        loading: false,
                    });
                },
                error => {
                    this.setState({ loading: false });
                    this.props.openSnackbar(error);
                }
            );
        }

        if (this.props.user.isAdmin || this.props.user.isCarwashAdmin) {
            apiFetch('api/users/dictionary').then(
                data => {
                    this.setState({
                        users: data,
                    });
                },
                error => {
                    this.setState({ loading: false });
                    this.props.openSnackbar(error);
                }
            );
        }
    }

    isUserConcurrentReservationLimitMet = () => {
        if (this.props.match.params.id) return false;
        if (this.props.user.isAdmin || this.props.user.isCarwashAdmin) return false;
        return this.props.reservations.filter(r => r.state !== 5).length >= 2;
    };

    handleNext = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
        }));
    };

    handleNextFromDateSelection = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
            reservationPercentageDataArrived: true,
        }));
    };

    handleBack = () => {
        this.setState(state => ({
            activeStep: state.activeStep - 1,
        }));
    };

    handleBackFromTimeSelection = () => {
        this.setState(state => ({
            activeStep: state.activeStep - 1,
            reservationPercentageDataArrived: false,
        }));
    };

    toggleServiceSelection(services, selectedServiceId) {
        const i = services.indexOf(selectedServiceId);

        // remove if found in array
        if (i >= 0) {
            services.splice(i, 1);
        } else {
            services.push(selectedServiceId);
        }

        return services;
    }

    setServiceSelection(services, selectedServiceId, shouldContain) {
        const i = services.indexOf(selectedServiceId);

        // remove if found in array
        if (i >= 0 && !shouldContain) {
            services.splice(i, 1);
        } else if (i < 0 && shouldContain) {
            services.push(selectedServiceId);
        }

        return services;
    }

    handleServiceChipClick = service => () => {
        this.setState(state => {
            const selectedServices = [...state.selectedServices];
            this.toggleServiceSelection(selectedServices, service.id);

            // if exterior, must include prewash and wheel cleaning too
            if (service.id === Service.Exterior && selectedServices.includes(service.id)) {
                this.setServiceSelection(selectedServices, Service.Prewash, true);
                this.setServiceSelection(selectedServices, Service.WheelCleaning, true);
            }
            if (
                (service.id === Service.Prewash || service.id === Service.WheelCleaning) &&
                !selectedServices.includes(service.id)
            ) {
                this.setServiceSelection(selectedServices, Service.Exterior, false);
            }

            // if carpet, must include exterior and interior too
            if (service.id === Service.Carpet && selectedServices.includes(service.id)) {
                this.setServiceSelection(selectedServices, Service.Exterior, true);
                this.setServiceSelection(selectedServices, Service.Prewash, true);
                this.setServiceSelection(selectedServices, Service.WheelCleaning, true);
                this.setServiceSelection(selectedServices, Service.Interior, true);
            }
            if (
                (service.id === Service.Exterior || service.id === Service.Interior) &&
                !selectedServices.includes(service.id)
            ) {
                this.setServiceSelection(selectedServices, Service.Carpet, false);
            }

            // cannot have both AC cleaning
            if (service.id === Service.AcCleaningBomba)
                this.setServiceSelection(selectedServices, Service.AcCleaningOzon, false);
            if (service.id === Service.AcCleaningOzon)
                this.setServiceSelection(selectedServices, Service.AcCleaningBomba, false);

            return { selectedServices };
        });
    };

    handleServiceSelectionComplete = () => {
        this.setState(state => ({
            activeStep: 1,
            servicesStepLabel: state.selectedServices.map(s => getServiceName(this.props.configuration, s)).join(', '),
        }));
    };

    handleDateSelectionComplete = date => {
        if (!date) return;
        const selectedDate = moment(date);

        this.setState({
            activeStep: 2,
            selectedDate,
            disabledSlots: [
                this.isTimeNotAvailable(selectedDate, 8),
                this.isTimeNotAvailable(selectedDate, 11),
                this.isTimeNotAvailable(selectedDate, 14),
            ],
            dateStepLabel: selectedDate.format('MMMM D, YYYY'),
            dateSelected: true,
        });

        apiFetch(`api/reservations/reservationcapacity?date=${selectedDate.toJSON()}`).then(
            data => {
                this.setState({
                    reservationPercentage: data,
                    reservationPercentageDataArrived: true,
                });
            },
            error => {
                this.setState({ loading: false });
                this.props.openSnackbar(error);
            }
        );
    };

    handleTimeSelectionComplete = event => {
        const time = event.target.value;
        const dateTime = moment(this.state.selectedDate);
        dateTime.hours(time);
        dateTime.minutes(0);
        dateTime.seconds(0);
        dateTime.milliseconds(0);
        this.setState({
            activeStep: 3,
            selectedDate: dateTime,
            timeStepLabel: dateTime.format('hh:mm A'),
            timeSelected: true,
        });

        // Should delay loading from lastsettings as much as possible, because if the user reloads the page,
        // it can happen that the lastsettings object is not yet populated in props.
        if (!this.props.match.params.id) {
            this.setState({
                vehiclePlateNumber: this.props.lastSettings.vehiclePlateNumber || '',
                garage: this.props.lastSettings.garage || '',
            });
        }
    };

    isTimeNotAvailable = (date, time) => {
        date.hours(time);
        return (
            this.state.notAvailableTimes.filter(notAvailableTime => notAvailableTime.isSame(date, 'hour')).length > 0
        );
    };

    handlePlateNumberChange = event => {
        this.setState({
            vehiclePlateNumber: event.target.value.toUpperCase(),
        });
    };

    handlePrivateChange = () => {
        this.setState(state => ({
            private: !state.private,
        }));
    };

    handleCommentChange = event => {
        this.setState({
            comment: event.target.value,
        });
    };

    handleLocationKnown = () => {
        this.setState(state => ({
            locationKnown: !state.locationKnown,
        }));
    };

    handleDropoffPreConfirmed = () => {
        this.setState(state => ({
            dropoffPreConfirmed: !state.dropoffPreConfirmed,
        }));
    };

    handleGarageChange = event => {
        this.setState({
            garage: event.target.value,
        });
    };

    handleFloorChange = event => {
        this.setState({
            floor: event.target.value,
        });
    };

    handleSeatChange = event => {
        this.setState({
            seat: event.target.value,
        });
    };

    handleUserChange = (event, newInputValue) => {
        this.setState({
            userId: newInputValue,
        });
    };

    handleReserve = () => {
        const validationErrors = {
            vehiclePlateNumber: this.state.vehiclePlateNumber === '' && this.state.vehiclePlateNumber.length > 8,
            garage: this.state.garage === '' && this.state.locationKnown,
            floor: this.state.floor === '' && this.state.locationKnown,
        };

        if (validationErrors.vehiclePlateNumber || validationErrors.garage || validationErrors.floor) {
            this.setState({ validationErrors });
            return;
        }

        this.setState({ loading: true });

        const payload = {
            id: this.props.match.params.id,
            userId: this.state.userId,
            vehiclePlateNumber: this.state.vehiclePlateNumber,
            location: this.state.locationKnown ? `${this.state.garage}/${this.state.floor}/${this.state.seat}` : null,
            services: this.state.selectedServices,
            private: this.state.private,
            startDate: this.state.selectedDate,
        };

        if (this.state.comment) {
            if (!payload.comments) payload.comments = [];
            payload.comments.push({
                message: this.state.comment,
            });
        }

        let apiUrl = 'api/reservations';
        let apiMethod = 'POST';
        if (payload.id) {
            apiUrl = `api/reservations/${payload.id}`;
            apiMethod = 'PUT';
        }

        if (this.state.dropoffPreConfirmed) apiUrl += '?dropoffconfirmed=true';

        apiFetch(apiUrl, {
            method: apiMethod,
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            data => {
                if (this.props.user.notificationChannel === NotificationChannel.NotSet)
                    this.props.openNotificationDialog();

                this.setState({
                    loading: false,
                    reservationCompleteRedirect: true,
                });
                this.props.openSnackbar('Reservation successfully saved.');

                if (apiMethod === 'PUT') {
                    // Update reservation locally
                    this.props.removeReservation(data.id);
                    this.props.addReservation(data);
                } else {
                    // Add new reservation locally
                    this.props.addReservation(data);
                }

                // Refresh last settings
                // Delete cached response for /api/reservations/lastsettings
                // Not perfect solution as it seems Safari does not support this
                // https://developer.mozilla.org/en-US/docs/Web/API/Cache/delete#Browser_compatibility
                try {
                    caches.open('api-cache').then(cache => {
                        cache.delete('/api/reservations/lastsettings');
                    });
                    this.props.loadLastSettings();
                } catch (error) {
                    console.error(`Cannot delete user data from cache: ${error}`);
                }
            },
            error => {
                this.setState({ loading: false });
                this.props.openSnackbar(error);
            }
        );
    };

    getServiceListComponent = (services, selectedServices, classes) => {
        const jsx = [];
        const serviceGroups = Object.groupBy(services, ({ group }) => group);

        for (const serviceGroup in serviceGroups) {
            if (serviceGroups && typeof serviceGroups === 'object' && Object.hasOwn(serviceGroups, serviceGroup)) {
                jsx.push(
                    <div key={serviceGroup}>
                        <Typography variant="caption">{serviceGroup}</Typography>
                    </div>
                );
                jsx.push(
                    serviceGroups[serviceGroup]
                        .filter(s => s.hidden === false)
                        .map(service => (
                            <span key={service.id}>
                                <Chip
                                    key={service.id}
                                    label={service.name}
                                    onClick={this.handleServiceChipClick(service)}
                                    className={
                                        selectedServices.includes(service.id) ? classes.selectedChip : classes.chip
                                    }
                                    id={`reserve-${service.name}-service-chip`}
                                />
                            </span>
                        ))
                );
                jsx.push(<br />);
            }
        }

        return jsx;
    };

    getSlotsComponent = (slots, disabledSlots) => {
        const jsx = [];
        let i = 0;

        for (const slot of slots) {
            let freeSlots = 0;
            let freeSlotsText = '';
            if (this.state.reservationPercentageDataArrived) {
                if (!this.state.reservationPercentage[i]) freeSlotsText = '(unknown free slots)';
                else {
                    freeSlots = this.state.reservationPercentage[i].freeCapacity;
                    freeSlotsText = `(~${freeSlots} free slots)`;
                }
            }

            jsx.push(
                <FormControlLabel
                    value={slot.startTime}
                    control={<Radio />}
                    label={`${slot.startTime}:00 - ${slot.endTime}:00 ${freeSlotsText}`}
                    disabled={disabledSlots[i] && freeSlots === 0}
                    id={`reserve-slot-${i}`}
                />
            );
            i++;
        }

        return jsx;
    };

    render() {
        const { classes, user, configuration } = this.props;
        const {
            activeStep,
            selectedServices,
            servicesStepLabel,
            dateStepLabel,
            timeStepLabel,
            validationErrors,
            loading,
            loadingReservation,
            notAvailableDates,
            disabledSlots,
            users,
            userId,
            vehiclePlateNumber,
            selectedDate,
            comment,
            garage,
            floor,
            seat,
            locationKnown,
            dropoffPreConfirmed,
            dateSelected,
            timeSelected,
        } = this.state;
        const today = moment();
        const yearFromToday = moment().add(1, 'year');
        const isDateToday = selectedDate?.isSame(today, 'day');

        const shouldDisableDate = date => {
            const dayOfWeek = date.day();
            if (dayOfWeek === 0 || dayOfWeek === 6) return true;

            return notAvailableDates.some(d => date.isSame(d, 'day'));
        };

        if (this.state.reservationCompleteRedirect) {
            return <Redirect to="/" />;
        }

        if (!navigator.onLine) {
            return (
                <div className={classes.center}>
                    <div>
                        <CloudOffIcon className={classes.errorIcon} />
                        <Typography variant="h6" gutterBottom className={classes.errorText}>
                            Connect to the Internet
                        </Typography>
                        <Typography className={classes.errorText}>
                            You must be connected to make a new reservation.
                        </Typography>
                    </div>
                </div>
            );
        }

        if (this.isUserConcurrentReservationLimitMet()) {
            return (
                <div className={classes.center}>
                    <div>
                        <ErrorOutlineIcon className={classes.errorIcon} />
                        <Typography variant="h6" gutterBottom className={classes.errorText}>
                            Limit reached
                        </Typography>
                        <Typography className={classes.errorText}>
                            You cannot have more than two concurrent active reservations.
                        </Typography>
                    </div>
                </div>
            );
        }

        return (
            <Stepper activeStep={activeStep} orientation="vertical" className={classes.stepper}>
                <Step>
                    <StepLabel>{servicesStepLabel}</StepLabel>
                    <StepContent>
                        {loadingReservation ? (
                            <Spinner />
                        ) : (
                            <Grid container spacing={2}>
                                <Grid item xs={12} md={6}>
                                    {this.getServiceListComponent(configuration.services, selectedServices, classes)}
                                    <div className={classes.actionsContainer}>
                                        <div>
                                            <Button disabled className={classes.button}>
                                                Back
                                            </Button>
                                            <Button
                                                variant="contained"
                                                color="primary"
                                                onClick={this.handleServiceSelectionComplete}
                                                className={classes.button}
                                                disabled={selectedServices.length <= 0}
                                                id="reserve-services-next-button"
                                            >
                                                Next
                                            </Button>
                                        </div>
                                    </div>
                                </Grid>
                                <Grid item xs={12} md={6}>
                                    <ServiceDetailsTable configuration={configuration} />
                                </Grid>
                            </Grid>
                        )}
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{dateStepLabel}</StepLabel>
                    <StepContent>
                        <LocalizationProvider dateAdapter={AdapterMoment}>
                            <DateCalendar
                                onChange={date => this.handleDateSelectionComplete(date)}
                                value={selectedDate}
                                referenceDate={today}
                                loading={loading}
                                shouldDisableDate={shouldDisableDate}
                                disablePast
                                maxDate={yearFromToday}
                                views={['day']}
                                className={classes.calendar}
                            />
                        </LocalizationProvider>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button onClick={this.handleBack} className={classes.button}>
                                    Back
                                </Button>
                                <Button
                                    disabled={!dateSelected}
                                    onClick={this.handleNextFromDateSelection}
                                    variant="contained"
                                    color="primary"
                                    className={classes.button}
                                    id="reserve-date-next-button"
                                >
                                    Next
                                </Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{timeStepLabel}</StepLabel>
                    <StepContent>
                        <Alert variant="outlined" severity="info" className={classes.infoAlert}>
                            Drop the car key before your slot starts to ensure timely completion. Ending times are
                            indicative. For special requests, use the comment field.
                        </Alert>
                        <FormControl component="fieldset">
                            <RadioGroup
                                aria-label="Time"
                                name="time"
                                className={classes.radioGroup}
                                value={`${timeSelected && selectedDate.hour()}`}
                                onChange={this.handleTimeSelectionComplete}
                            >
                                {this.getSlotsComponent(configuration.slots, disabledSlots)}
                            </RadioGroup>
                        </FormControl>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button onClick={this.handleBackFromTimeSelection} className={classes.button}>
                                    Back
                                </Button>
                                <Button
                                    disabled={!timeSelected}
                                    onClick={this.handleNext}
                                    variant="contained"
                                    color="primary"
                                    className={classes.button}
                                    id="reserve-time-next-button"
                                >
                                    Next
                                </Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>Reserve</StepLabel>
                    <StepContent>
                        {loading ? (
                            <Spinner />
                        ) : (
                            <div>
                                <Alert variant="outlined" severity="warning" className={classes.infoAlert}>
                                    Failure to specify a private reservation will result in the associated company being
                                    billed. Users will be charged for overhead accounting costs incurred to correct
                                    errors.
                                </Alert>
                                <div>
                                    <FormGroup className={classes.checkbox}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={this.state.private}
                                                    onChange={this.handlePrivateChange}
                                                    value="private"
                                                />
                                            }
                                            label="Private (car is not company-owned)"
                                        />
                                    </FormGroup>
                                </div>
                                {(user.isAdmin || user.isCarwashAdmin) && (
                                    <FormControl className={classes.formControl}>
                                        <Autocomplete
                                            required
                                            value={userId}
                                            onChange={this.handleUserChange}
                                            disablePortal
                                            selectOnFocus
                                            clearOnBlur
                                            handleHomeEndKeys
                                            id="user"
                                            options={Object.keys(users)}
                                            getOptionLabel={option => users[option]}
                                            renderOption={(props, option) => {
                                                const { key, ...optionProps } = props;
                                                return (
                                                    <li key={option} {...optionProps}>
                                                        {users[option]}
                                                    </li>
                                                );
                                            }}
                                            renderInput={params => <TextField {...params} label="User" />}
                                        />
                                    </FormControl>
                                )}
                                <div>
                                    <TextField
                                        required
                                        error={validationErrors.vehiclePlateNumber}
                                        id="reserve-vehiclePlateNumber"
                                        label="Plate number"
                                        value={vehiclePlateNumber}
                                        className={classes.textField}
                                        margin="normal"
                                        onChange={this.handlePlateNumberChange}
                                        inputProps={{ maxLength: 8 }}
                                    />
                                </div>
                                <div>
                                    <FormGroup className={classes.checkbox} style={{ marginTop: 8 }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={locationKnown}
                                                    onChange={this.handleLocationKnown}
                                                    value="locationKnown"
                                                    color="primary"
                                                />
                                            }
                                            label="I have already parked the car"
                                        />
                                    </FormGroup>
                                </div>
                                {locationKnown && (
                                    <>
                                        <div style={{ marginLeft: 8 }}>
                                            <LocationSelector
                                                configuration={configuration}
                                                garage={garage}
                                                floor={floor}
                                                spot={seat}
                                                validationErrors={validationErrors}
                                                onGarageChange={this.handleGarageChange}
                                                onFloorChange={this.handleFloorChange}
                                                onSpotChange={this.handleSeatChange}
                                                textFieldWidth={200}
                                            />
                                        </div>
                                        {isDateToday && (
                                            <>
                                                <FormGroup className={classes.checkbox} style={{ marginTop: 8 }}>
                                                    <FormControlLabel
                                                        control={
                                                            <Checkbox
                                                                checked={dropoffPreConfirmed}
                                                                onChange={this.handleDropoffPreConfirmed}
                                                                value="dropoffPreConfirmed"
                                                                color="primary"
                                                            />
                                                        }
                                                        label="I have already left the key at the reception"
                                                    />
                                                </FormGroup>
                                                {dropoffPreConfirmed && (
                                                    <Alert
                                                        variant="outlined"
                                                        severity="warning"
                                                        className={classes.infoAlert}
                                                    >
                                                        You won't be able to modify your reservation after you click
                                                        Reserve!
                                                    </Alert>
                                                )}
                                            </>
                                        )}
                                    </>
                                )}
                                {!this.isUpdate && (
                                    <div>
                                        <TextField
                                            id="reserve-comment"
                                            label="Comment"
                                            multiline
                                            maxRows="4"
                                            value={comment}
                                            onChange={this.handleCommentChange}
                                            className={classes.textField}
                                            margin="normal"
                                        />
                                    </div>
                                )}
                                <div className={classes.actionsContainer}>
                                    <div>
                                        <Button onClick={this.handleBack} className={classes.button}>
                                            Back
                                        </Button>
                                        <Button
                                            variant="contained"
                                            color="primary"
                                            onClick={this.handleReserve}
                                            className={classes.button}
                                            id="reserve-submit-button"
                                        >
                                            {!this.isUpdate ? 'Reserve' : 'Update'}
                                        </Button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </StepContent>
                </Step>
            </Stepper>
        );
    }
}

Reserve.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    user: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    addReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func,
    lastSettings: PropTypes.object, // eslint-disable-line react/forbid-prop-types
    loadLastSettings: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    openNotificationDialog: PropTypes.func.isRequired,
};

export default withStyles(styles)(Reserve);
