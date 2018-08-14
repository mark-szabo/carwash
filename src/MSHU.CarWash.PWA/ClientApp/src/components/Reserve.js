import React, { Component } from 'react';
import { adalFetch } from '../Auth';
import PropTypes from 'prop-types';
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
import CircularProgress from '@material-ui/core/CircularProgress';
import TextField from '@material-ui/core/TextField';
import InputLabel from '@material-ui/core/InputLabel';
import MenuItem from '@material-ui/core/MenuItem';
import Select from '@material-ui/core/Select';

import InfiniteCalendar from 'react-infinite-calendar';
import 'react-infinite-calendar/styles.css';
import './Reserve.css';

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
        width: 200,
    },
    checkbox: {
        marginLeft: theme.spacing.unit,
        marginRight: theme.spacing.unit,
    },
    formControl: {
        margin: theme.spacing.unit,
        minWidth: 200,
    },
    progress: {
        margin: theme.spacing.unit * 2,
    },
});

function addDays(date, days) {
    var newDate = new Date(date);
    newDate.setDate(newDate.getDate() + days);
    return newDate;
}

class Reserve extends Component {
    displayName = Reserve.name

    constructor(props) {
        super(props);
        this.state = {
            activeStep: 0,
            notAvailableDates: [],
            notAvailableTimes: [],
            loading: true,
            services: [
                { id: 0, name: 'exterior', selected: false },
                { id: 1, name: 'interior', selected: false },
                { id: 2, name: 'carpet', selected: false },
                { id: 3, name: 'spot cleaning', selected: false },
                { id: 4, name: 'vignette removal', selected: false },
                { id: 5, name: 'polishing', selected: false },
                { id: 6, name: 'AC cleaning \'ozon\'', selected: false },
                { id: 7, name: 'AC cleaning \'bomba\'', selected: false }
            ],

            selectedDate: new Date(),
            vehiclePlateNumber: '',
            garage: '',
            floor: '',
            seat: '',
            private: false,
            comment: '',
            disabledSlots: [],
            servicesStepLabel: 'Select services',
            dateStepLabel: 'Choose date',
            timeStepLabel: 'Choose time',
        };
    }

    handleNext = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
        }));
    };

    handleBack = () => {
        this.setState(state => ({
            activeStep: state.activeStep - 1,
        }));
    };

    handleServiceChipClick = service => () => {
        this.setState(state => {
            const services = [...state.services];
            services[service.id].selected = !services[service.id].selected;

            // if carpet, must include exterior and interior too
            if (service.id === 2 && service.selected) {
                services[0].selected = true;
                services[1].selected = true;
            }
            if ((service.id === 0 || service.id === 1) && !service.selected) {
                services[2].selected = false;
            }

            return { services };
        });
    };

    handleServiceSelectionComplete = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
            servicesStepLabel: state.services.filter((service) => { return service.selected }).map((service) => { return service.name }).join(', '),
        }));
    };

    handleDateSelectionComplete = (date) => {
        if (date == null) return;
        const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

        this.setState(state => ({
            activeStep: state.activeStep + 1,
            selectedDate: date,
            disabledSlots: [
                this.isTimeNotAvailable(date, 8),
                this.isTimeNotAvailable(date, 11),
                this.isTimeNotAvailable(date, 14)
            ],
            dateStepLabel: `${months[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`,
        }));
    };

    handleTimeSelectionComplete = event => {
        const time = event.target.value;
        const dateTime = new Date(this.state.selectedDate);
        dateTime.setHours(time);
        this.setState(state => ({
            activeStep: state.activeStep + 1,
            selectedDate: dateTime,
            timeStepLabel: `${dateTime.getHours()}:00`,
        }));
    };

    isTimeNotAvailable = (date, time) => {
        const dateTime = new Date(date);
        dateTime.setHours(time);
        return this.state.notAvailableTimes.filter(notAvailableTime => notAvailableTime.getTime() === dateTime.getTime()).length > 0;
    };

    handlePlateNumberChange = (event) => {
        this.setState({
            vehiclePlateNumber: event.target.value.toUpperCase(),
        });
    };

    handlePrivateChange = () => {
        this.setState(state => ({
            private: !state.private,
        }));
    };

    handleCommentChange = (event) => {
        this.setState({
            comment: event.target.value,
        });
    };

    handleGarageChange = (event) => {
        this.setState({
            garage: event.target.value,
        });
    };

    handleFloorChange = (event) => {
        this.setState({
            floor: event.target.value,
        });
    };

    handleSeatChange = (event) => {
        this.setState({
            seat: event.target.value,
        });
    };

    handleReserve = () => {
        this.setState(state => ({
            activeStep: state.activeStep + 1,
        }));
    };

    componentDidMount() {
        adalFetch('api/reservations/notavailabledates')
            .then(response => response.json())
            .then(data => {
                for (let i in data.dates) {
                    if (data.dates.hasOwnProperty(i)) {
                        data.dates[i] = new Date(data.dates[i]);
                    }
                }
                for (let i in data.times) {
                    if (data.times.hasOwnProperty(i)) {
                        data.times[i] = new Date(data.times[i]);
                    }
                }
                this.setState({
                    notAvailableDates: data.dates,
                    notAvailableTimes: data.times,
                    loading: false
                });
            });
        adalFetch('api/reservations/lastsettings')
            .then(response => response.json())
            .then(data => {
                const [garage, floor] = data.location.split('/');
                this.setState({
                    vehiclePlateNumber: data.vehiclePlateNumber,
                    garage,
                    floor,
                });
            });
    }

    render() {
        const { classes, user } = this.props;
        const { activeStep, loading, notAvailableDates, disabledSlots, vehiclePlateNumber, comment, servicesStepLabel, dateStepLabel, timeStepLabel, garage, floor, seat } = this.state;
        console.log(user);
        const today = new Date();

        const garages = {
            M: [
                '-1',
                '-2',
                '-2.5',
                '-3',
                '-3.5',
                'outdoor'
            ],
            G: [
                '-1',
                'outdoor'
            ],
            S1: [
                '-1',
                '-2',
                '-3'
            ]
        };

        return (
            <Stepper activeStep={activeStep} orientation="vertical" className={classes.stepper}>
                <Step>
                    <StepLabel>{servicesStepLabel}</StepLabel>
                    <StepContent>
                        {this.state.services.map(service =>
                            <span key={service.id}>
                                {service.id === 0 && (<Typography variant="body2">Basic</Typography>)}
                                {service.id === 3 && (<Typography variant="body2">Extras</Typography>)}
                                {service.id === 6 && (<Typography variant="body2">AC</Typography>)}
                                <Chip
                                    key={service.id}
                                    label={service.name}
                                    onClick={this.handleServiceChipClick(service)}
                                    className={service.selected ? classes.selectedChip : classes.chip}
                                />
                                {service.id === 2 && (<br />)}
                                {service.id === 5 && (<br />)}
                            </span>
                        )}
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button
                                    disabled={true}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={this.handleServiceSelectionComplete}
                                    className={classes.button}
                                    disabled={this.state.services.filter(service => service.selected === true).length <= 0}
                                >Next</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{dateStepLabel}</StepLabel>
                    <StepContent>
                        {loading ? (<CircularProgress className={classes.progress} size={50} />) : (
                            <InfiniteCalendar
                                onSelect={(date) => this.handleDateSelectionComplete(date)}
                                selected={null}
                                min={today}
                                minDate={today}
                                max={addDays(today, 365)}
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
                                <Button
                                    onClick={this.handleBack}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    disabled={true}
                                    variant="contained"
                                    color="primary"
                                    className={classes.button}
                                >Next</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>{timeStepLabel}</StepLabel>
                    <StepContent>
                        <FormControl component="fieldset">
                            <RadioGroup
                                aria-label="Time"
                                name="time"
                                className={classes.radioGroup}
                                onChange={this.handleTimeSelectionComplete}
                            >
                                <FormControlLabel value="8" control={<Radio />} label="8:00 AM - 11:00 AM" disabled={disabledSlots[0]} />
                                <FormControlLabel value="11" control={<Radio />} label="11:00 AM - 2:00 PM" disabled={disabledSlots[1]} />
                                <FormControlLabel value="14" control={<Radio />} label="2:00 PM - 5:00 PM" disabled={disabledSlots[2]} />
                            </RadioGroup>
                        </FormControl>
                        <div className={classes.actionsContainer}>
                            <div>
                                <Button
                                    onClick={this.handleBack}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    disabled={true}
                                    variant="contained"
                                    color="primary"
                                    className={classes.button}
                                >Next</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
                <Step>
                    <StepLabel>Reserve</StepLabel>
                    <StepContent>
                        <div>
                            <FormGroup className={classes.checkbox}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            onChange={this.handlePrivateChange}
                                            value="private"
                                        />
                                    }
                                    label="Private"
                                />
                            </FormGroup>
                        </div>
                        <div>
                            <TextField
                                required
                                id="vehiclePlateNumber"
                                label="Plate number"
                                value={vehiclePlateNumber}
                                className={classes.textField}
                                margin="normal"
                                onChange={this.handlePlateNumberChange}
                            />
                        </div>
                        <FormControl className={classes.formControl}>
                            <InputLabel htmlFor="garage">Garage</InputLabel>
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
                                <MenuItem value="G">G</MenuItem>
                                <MenuItem value="S1">S1</MenuItem>
                            </Select>
                        </FormControl>
                        {garage !== '' && (
                            <FormControl className={classes.formControl}>
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
                                    {garages[garage].map((item) => (
                                        <MenuItem value={item} key={item}>{item}</MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                        )}
                        {floor !== '' && (
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
                                <Button
                                    onClick={this.handleBack}
                                    className={classes.button}
                                >Back</Button>
                                <Button
                                    variant="contained"
                                    color="primary"
                                    onClick={this.handleReserve}
                                    className={classes.button}
                                >Reserve</Button>
                            </div>
                        </div>
                    </StepContent>
                </Step>
            </Stepper>
            //activeStep === steps.length && (
            //    <Paper square elevation={0} className={classes.resetContainer}>
            //        <Typography>All steps completed - you&quot;re finished</Typography>
            //        <Button onClick={this.handleReset} className={classes.button}>
            //            Reset
            //        </Button>
            //    </Paper>
            //)
        );
    }
}

Reserve.propTypes = {
    classes: PropTypes.object.isRequired,
};

export default withStyles(styles)(Reserve);