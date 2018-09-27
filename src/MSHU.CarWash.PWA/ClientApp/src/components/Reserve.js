import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import { Redirect } from 'react-router';
import apiFetch from '../Auth';
import { withStyles } from '@material-ui/core/styles';
import Stepper from '@material-ui/core/Stepper';
import Step from '@material-ui/core/Step';
import StepLabel from '@material-ui/core/StepLabel';
import StepContent from '@material-ui/core/StepContent';
import Button from '@material-ui/core/Button';
import Chip from '@material-ui/core/Chip';
import Typography from '@material-ui/core/Typography';
import Radio from '@material-ui/core/Radio';
import RadioGroup from '@material-ui/core/RadioGroup';
import FormGroup from '@material-ui/core/FormGroup';
import Checkbox from '@material-ui/core/Checkbox';
import FormControlLabel from '@material-ui/core/FormControlLabel';
import FormControl from '@material-ui/core/FormControl';
import TextField from '@material-ui/core/TextField';
import InputLabel from '@material-ui/core/InputLabel';
import MenuItem from '@material-ui/core/MenuItem';
import Select from '@material-ui/core/Select';
import InfiniteCalendar from 'react-infinite-calendar';
import CloudOffIcon from '@material-ui/icons/CloudOff';
import ErrorOutlineIcon from '@material-ui/icons/ErrorOutline';
import Grid from '@material-ui/core/Grid';
import * as moment from 'moment';
import { Garages, Service, NotificationChannel } from '../Constants';
import 'react-infinite-calendar/styles.css';
import './Reserve.css';
import ServiceDetailsTable from './ServiceDetailsTable';
import Spinner from './Spinner';

const styles = theme => ({
    stepper: {
        padding: 0,
        backgroundColor: 'inherit',
    },
    button: {
        marginTop: theme.spacing.unit,
        marginRight: theme.spacing.unit,
    },
    actionsContainer: {
        marginTop: theme.spacing.unit,
        marginBottom: theme.spacing.unit * 2,
    },
    resetContainer: {
        padding: theme.spacing.unit * 3,
    },
    chip: {
        margin: theme.spacing.unit / 2,
    },
    selectedChip: {
        margin: theme.spacing.unit / 2,
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
        marginTop: theme.spacing.unit / 2,
    },
    calendar: {
        maxWidth: '400px',
    },
    radioGroup: {
        margin: `${theme.spacing.unit}px 0`,
    },
    container: {
        display: 'flex',
        flexWrap: 'wrap',
    },
    textField: {
        marginLeft: theme.spacing.unit,
        marginRight: theme.spacing.unit,
        [theme.breakpoints.down('sm')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 200,
        },
    },
    checkbox: {
        marginLeft: theme.spacing.unit,
        marginRight: theme.spacing.unit,
    },
    formControl: {
        margin: theme.spacing.unit,
        [theme.breakpoints.down('sm')]: {
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
        margin: theme.spacing.unit,
        color: '#BDBDBD',
        width: '100px',
        height: '100px',
    },
    errorText: {
        color: '#9E9E9E',
    },
});

class Reserve extends TrackedComponent {
    displayName = Reserve.name;

    constructor(props) {
        super(props);
        this.state = {
            activeStep: 0,
            notAvailableDates: [],
            notAvailableTimes: [],
            loading: true,
            loadingReservation: false,
            reservationCompleteRedirect: false,
            services: [
                { id: 0, name: 'exterior', selected: false },
                { id: 1, name: 'interior', selected: false },
                { id: 2, name: 'carpet', selected: false },
                { id: 3, name: 'spot cleaning', selected: false },
                { id: 4, name: 'vignette removal', selected: false },
                { id: 5, name: 'polishing', selected: false },
                { id: 6, name: "AC cleaning 'ozon'", selected: false },
                { id: 7, name: "AC cleaning 'bomba'", selected: false },
            ],
            validationErrors: {
                vehiclePlateNumber: false,
                garage: false,
                floor: false,
            },
            selectedDate: moment(),
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
        };
    }

    componentDidMount() {
        super.componentDidMount();

        if (this.props.match.params.id) {
            this.setState({
                loadingReservation: true,
            });
            apiFetch(`api/reservations/${this.props.match.params.id}`).then(
                data => {
                    const services = this.state.services;
                    data.services.forEach(s => {
                        services[s].selected = true;
                    });

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
                        services,
                        selectedDate: date,
                        vehiclePlateNumber: data.vehiclePlateNumber,
                        garage,
                        floor,
                        seat,
                        private: data.private,
                        comment: data.comment,
                        servicesStepLabel: services
                            .filter(s => s.selected)
                            .map(s => s.name)
                            .join(', '),
                        dateStepLabel: date.format('MMMM D, YYYY'),
                        timeStepLabel: date.format('hh:mm A'),
                        loadingReservation: false,
                    });
                },
                error => {
                    this.setState({ loading: false });
                    this.props.openSnackbar(error);
                }
            );
        } else {
            this.setState({
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

        if (this.props.user.isAdmin) {
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

    handleBack = () => {
        this.setState(state => ({
            activeStep: state.activeStep - 1,
            reservationPercentageDataArrived: false,
        }));
    };

    handleServiceChipClick = service => () => {
        this.setState(state => {
            const services = [...state.services];
            services[service.id].selected = !services[service.id].selected;

            // if carpet, must include exterior and interior too
            if (service.id === Service.Carpet && service.selected) {
                services[Service.Exterior].selected = true;
                services[Service.Interior].selected = true;
            }
            if ((service.id === Service.Exterior || service.id === Service.Interior) && !service.selected) {
                services[Service.Carpet].selected = false;
            }

            // cannot have both AC cleaning
            if (service.id === Service.AcCleaningBomba) services[Service.AcCleaningOzon].selected = false;
            if (service.id === Service.AcCleaningOzon) services[Service.AcCleaningBomba].selected = false;

            return { services };
        });
    };

    handleServiceSelectionComplete = () => {
        this.setState(state => ({
            activeStep: 1,
            servicesStepLabel: state.services
                .filter(service => service.selected)
                .map(service => service.name)
                .join(', '),
        }));
    };

    handleDateSelectionComplete = date => {
        if (!date) return;
        const selectedDate = moment(date);

        this.setState({
            activeStep: 2,
            selectedDate,
            disabledSlots: [this.isTimeNotAvailable(selectedDate, 8), this.isTimeNotAvailable(selectedDate, 11), this.isTimeNotAvailable(selectedDate, 14)],
            dateStepLabel: selectedDate.format('MMMM D, YYYY'),
        });

        apiFetch(`api/reservations/reservationpercentage?date=${selectedDate.toJSON()}`).then(
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

    getSlotReservationPercentage = slot => {
        if (!this.state.reservationPercentageDataArrived) return '';
        if (!this.state.reservationPercentage[slot]) return '(0%)';
        return `(${this.state.reservationPercentage[slot].percentage * 100}%)`;
    };

    handleTimeSelectionComplete = event => {
        const time = event.target.value;
        const dateTime = moment(this.state.selectedDate);
        dateTime.hours(time);
        this.setState({
            activeStep: 3,
            selectedDate: dateTime,
            timeStepLabel: dateTime.format('hh:mm A'),
        });
    };

    isTimeNotAvailable = (date, time) => {
        date.hours(time);
        return this.state.notAvailableTimes.filter(notAvailableTime => notAvailableTime.isSame(date, 'hour')).length > 0;
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

    handleUserChange = event => {
        this.setState({
            userId: event.target.value,
        });
    };

    handleReserve = () => {
        const validationErrors = {
            vehiclePlateNumber: this.state.vehiclePlateNumber === '',
            garage: this.state.garage === '' && this.state.locationKnown,
            floor: this.state.floor === '' && this.state.locationKnown,
        };

        if (validationErrors.vehiclePlateNumber || validationErrors.garage || validationErrors.floor) {
            this.setState({ validationErrors });
            return;
        }

        this.setState({
            loading: true,
        });

        const payload = {
            id: this.props.match.params.id,
            userId: this.state.userId,
            vehiclePlateNumber: this.state.vehiclePlateNumber,
            location: this.state.locationKnown ? `${this.state.garage}/${this.state.floor}/${this.state.seat}` : null,
            services: this.state.services.filter(s => s.selected).map(s => s.id),
            private: this.state.private,
            startDate: this.state.selectedDate,
            comment: this.state.comment,
        };

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
                if (this.props.user.notificationChannel === NotificationChannel.NotSet) this.props.openNotificationDialog();

                this.setState({
                    loading: false,
                    reservationCompleteRedirect: true,
                });
                this.props.openSnackbar('Reservation successfully saved.');

                // Add new / update reservation
                if (apiMethod === 'PUT') this.props.removeReservation(data.id);
                this.props.addReservation(data);

                // Refresh last settings
                // Delete cached response for /api/reservations/lastsettings
                // Not perfect solution as it seems Safari does not supports this
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

    render() {
        const { classes, user } = this.props;
        const {
            activeStep,
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
            comment,
            garage,
            floor,
            seat,
            locationKnown,
            dropoffPreConfirmed,
        } = this.state;
        const today = moment();

        if (this.state.reservationCompleteRedirect) {
            return <Redirect to="/" />;
        }

        if (!navigator.onLine) {
            return (
                <div className={classes.center}>
                    <div>
                        <CloudOffIcon className={classes.errorIcon} />
                        <Typography variant="title" gutterBottom className={classes.errorText}>
                            Connect to the Internet
                        </Typography>
                        <Typography className={classes.errorText}>You must be connected to make a new reservation.</Typography>
                    </div>
                </div>
            );
        }

        if (this.isUserConcurrentReservationLimitMet()) {
            return (
                <div className={classes.center}>
                    <div>
                        <ErrorOutlineIcon className={classes.errorIcon} />
                        <Typography variant="title" gutterBottom className={classes.errorText}>
                            Limit reached
                        </Typography>
                        <Typography className={classes.errorText}>You cannot have more than two concurrent active reservations.</Typography>
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
                            <Grid container spacing={24}>
                                <Grid item xs={12} md={6}>
                                    {this.state.services.map(service => (
                                        <span key={service.id}>
                                            {service.id === 0 && <Typography variant="body2">Basic</Typography>}
                                            {service.id === 3 && <Typography variant="body2">Extras</Typography>}
                                            {service.id === 6 && <Typography variant="body2">AC</Typography>}
                                            <Chip
                                                key={service.id}
                                                label={service.name}
                                                onClick={this.handleServiceChipClick(service)}
                                                className={service.selected ? classes.selectedChip : classes.chip}
                                            />
                                            {service.id === 2 && <br />}
                                            {service.id === 5 && <br />}
                                        </span>
                                    ))}
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
                                                disabled={this.state.services.filter(service => service.selected === true).length <= 0}
                                            >
                                                Next
                                            </Button>
                                        </div>
                                    </div>
                                </Grid>
                                <Grid item xs={12} md={6}>
                                    <ServiceDetailsTable />
                                </Grid>
                            </Grid>
                        )}
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{dateStepLabel}</StepLabel>
                    <StepContent>
                        {loading ? (
                            <Spinner />
                        ) : (
                            <InfiniteCalendar
                                onSelect={date => this.handleDateSelectionComplete(date)}
                                selected={null}
                                min={today.toDate()}
                                minDate={today.toDate()}
                                max={today.add(365, 'days').toDate()}
                                locale={{ weekStartsOn: 1 }}
                                disabledDays={[0, 6, 7]}
                                disabledDates={notAvailableDates}
                                displayOptions={{ showHeader: false, showTodayHelper: false }}
                                width={'100%'}
                                height={350}
                                className={classes.calendar}
                                theme={{
                                    selectionColor: '#80d8ff',
                                    weekdayColor: '#80d8ff',
                                }}
                            />
                        )}
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button onClick={this.handleBack} className={classes.button}>
                                    Back
                                </Button>
                                <Button disabled variant="contained" color="primary" className={classes.button}>
                                    Next
                                </Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{timeStepLabel}</StepLabel>
                    <StepContent>
                        <FormControl component="fieldset">
                            <RadioGroup aria-label="Time" name="time" className={classes.radioGroup} onChange={this.handleTimeSelectionComplete}>
                                <FormControlLabel
                                    value="8"
                                    control={<Radio />}
                                    label={`8:00 AM - 11:00 AM ${this.getSlotReservationPercentage(0)}`}
                                    disabled={disabledSlots[0]}
                                />
                                <FormControlLabel
                                    value="11"
                                    control={<Radio />}
                                    label={`11:00 AM - 2:00 PM ${this.getSlotReservationPercentage(1)}`}
                                    disabled={disabledSlots[1]}
                                />
                                <FormControlLabel
                                    value="14"
                                    control={<Radio />}
                                    label={`2:00 PM - 5:00 PM ${this.getSlotReservationPercentage(2)}`}
                                    disabled={disabledSlots[2]}
                                />
                            </RadioGroup>
                        </FormControl>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button onClick={this.handleBack} className={classes.button}>
                                    Back
                                </Button>
                                <Button disabled variant="contained" color="primary" className={classes.button}>
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
                                <div>
                                    <FormGroup className={classes.checkbox}>
                                        <FormControlLabel
                                            control={<Checkbox checked={this.state.private} onChange={this.handlePrivateChange} value="private" />}
                                            label="Private"
                                        />
                                    </FormGroup>
                                </div>
                                {user.isAdmin && (
                                    <FormControl className={classes.formControl}>
                                        <InputLabel htmlFor="user">User</InputLabel>
                                        <Select
                                            required
                                            value={userId}
                                            onChange={this.handleUserChange}
                                            inputProps={{
                                                name: 'user',
                                                id: 'user',
                                            }}
                                        >
                                            {Object.keys(users).map(id => (
                                                <MenuItem value={id} key={id}>
                                                    {users[id]}
                                                </MenuItem>
                                            ))}
                                        </Select>
                                    </FormControl>
                                )}
                                <div>
                                    <TextField
                                        required
                                        error={validationErrors.vehiclePlateNumber}
                                        id="vehiclePlateNumber"
                                        name="vehiclePlateNumber"
                                        label="Plate number"
                                        value={vehiclePlateNumber}
                                        className={classes.textField}
                                        margin="normal"
                                        onChange={this.handlePlateNumberChange}
                                    />
                                </div>
                                <div>
                                    <FormGroup className={classes.checkbox} style={{ marginTop: 8 }}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox checked={locationKnown} onChange={this.handleLocationKnown} value="locationKnown" color="primary" />
                                            }
                                            label="I have already parked the car"
                                        />
                                    </FormGroup>
                                </div>
                                {locationKnown && (
                                    <React.Fragment>
                                        <FormControl className={classes.formControl} error={validationErrors.garage}>
                                            <InputLabel htmlFor="garage">Building</InputLabel>
                                            <Select
                                                required
                                                value={garage}
                                                onChange={this.handleGarageChange}
                                                inputProps={{
                                                    name: 'garage',
                                                    id: 'garage',
                                                }}
                                            >
                                                <MenuItem value="M">M</MenuItem>
                                                <MenuItem value="S1">S1</MenuItem>
                                                <MenuItem value="GS">GS</MenuItem>
                                                <MenuItem value="HX">HX</MenuItem>
                                            </Select>
                                        </FormControl>
                                        {garage && (
                                            <FormControl className={classes.formControl} error={validationErrors.floor}>
                                                <InputLabel htmlFor="floor">Floor</InputLabel>
                                                <Select
                                                    required
                                                    value={floor}
                                                    onChange={this.handleFloorChange}
                                                    inputProps={{
                                                        name: 'floor',
                                                        id: 'floor',
                                                    }}
                                                >
                                                    {Garages[garage].map(item => (
                                                        <MenuItem value={item} key={item}>
                                                            {item}
                                                        </MenuItem>
                                                    ))}
                                                </Select>
                                            </FormControl>
                                        )}
                                        {floor && (
                                            <TextField
                                                id="seat"
                                                label="Seat (optional)"
                                                value={seat}
                                                className={classes.textField}
                                                margin="normal"
                                                onChange={this.handleSeatChange}
                                            />
                                        )}
                                        <div>
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
                                        </div>
                                    </React.Fragment>
                                )}
                                <div>
                                    <TextField
                                        id="comment"
                                        label="Comment"
                                        multiline
                                        rowsMax="4"
                                        value={comment}
                                        onChange={this.handleCommentChange}
                                        className={classes.textField}
                                        margin="normal"
                                    />
                                </div>
                                <div className={classes.actionsContainer}>
                                    <div>
                                        <Button onClick={this.handleBack} className={classes.button}>
                                            Back
                                        </Button>
                                        <Button variant="contained" color="primary" onClick={this.handleReserve} className={classes.button}>
                                            {!this.props.match.params.id ? 'Reserve' : 'Update'}
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
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    addReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func,
    lastSettings: PropTypes.object, // eslint-disable-line react/forbid-prop-types
    loadLastSettings: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    openNotificationDialog: PropTypes.func.isRequired,
};

export default withStyles(styles)(Reserve);
