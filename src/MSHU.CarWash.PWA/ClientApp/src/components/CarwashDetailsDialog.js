import React from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import { withStyles } from '@material-ui/core/styles';
import apiFetch from '../Auth';
import Button from '@material-ui/core/Button';
import Dialog from '@material-ui/core/Dialog';
import DialogContent from '@material-ui/core/DialogContent';
import DialogActions from '@material-ui/core/DialogActions';
import withMobileDialog from '@material-ui/core/withMobileDialog';
import Typography from '@material-ui/core/Typography';
import Chip from '@material-ui/core/Chip';
import IconButton from '@material-ui/core/IconButton';
import Input from '@material-ui/core/Input';
import InputLabel from '@material-ui/core/InputLabel';
import InputAdornment from '@material-ui/core/InputAdornment';
import FormControl from '@material-ui/core/FormControl';
import TextField from '@material-ui/core/TextField';
import MenuItem from '@material-ui/core/MenuItem';
import Select from '@material-ui/core/Select';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import LocalShippingIcon from '@material-ui/icons/LocalShipping';
import LocalShippingOutlinedIcon from '@material-ui/icons/LocalShippingOutlined';
import CloseIcon from '@material-ui/icons/Close';
import DoneIcon from '@material-ui/icons/Done';
import EditIcon from '@material-ui/icons/Edit';
import MoneyOffIcon from '@material-ui/icons/MoneyOff';
import SendIcon from '@material-ui/icons/Send';
import SaveIcon from '@material-ui/icons/Save';
import { State, getServiceName, getAdminStateName, Garages, Service } from './Constants';
import { formatLocation } from '../Helpers';
import Comments from './Comments';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
    },
    unselectedChip: {
        margin: '8px 8px 0 0',
        border: '1px solid rgba(0, 0, 0, 0.23)',
        backgroundColor: 'transparent',
        '&:hover': {
            backgroundColor: 'rgba(0, 0, 0, 0.08)',
        },
        '&:focus': {
            backgroundColor: 'rgba(0, 0, 0, 0.08)',
        },
        '&:hover:focus': {
            backgroundColor: 'rgba(0, 0, 0, 0.08)',
        },
    },
    details: {
        [theme.breakpoints.down('sm')]: {
            minWidth: '100%',
            maxWidth: '100%',
        },
        [theme.breakpoints.up('md')]: {
            minWidth: 600,
            maxWidth: 600,
        },
    },
    button: {
        margin: theme.spacing.unit,
        position: 'absolute',
        bottom: theme.spacing.unit * 2,
        right: theme.spacing.unit * 2,
        '&$pushActionsUp': {
            bottom: theme.spacing.unit * 2 + 48,
        },
    },
    closeButton: {
        margin: theme.spacing.unit,
        position: 'absolute',
        top: 0,
        right: 0,
    },
    actions: {
        justifyContent: 'initial',
        height: 36,
        '&$pushActionsUp': {
            marginBottom: 56,
        },
    },
    notSelectedMpv: {
        color: theme.palette.grey[300],
    },
    commentTextfield: {
        width: '100%',
    },
    subheader: {
        marginTop: theme.spacing.unit * 4,
    },
    comments: {
        maxWidth: 300,
        [theme.breakpoints.down('sm')]: {
            maxWidth: '100%',
        },
    },
    formControl: {
        marginRight: theme.spacing.unit,
        marginBottom: theme.spacing.unit,
        [theme.breakpoints.down('sm')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            maxWidth: 60,
        },
    },
    pushActionsUp: {},
});

function getDate(reservation) {
    const from = new Intl.DateTimeFormat('en-US', {
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(reservation.startDate));

    const to = new Intl.DateTimeFormat('en-US', {
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(reservation.endDate));

    const date = new Intl.DateTimeFormat('en-US', {
        month: 'long',
        day: 'numeric',
    }).format(new Date(reservation.startDate));

    return `${from} - ${to} • ${date}`;
}

class CarwashDetailsDialog extends React.Component {
    state = {
        commentTextfield: '',
        editLocation: false,
        garage: '',
        floor: '',
        seat: '',
        validationErrors: {
            garage: false,
            floor: false,
        },
        editServices: false,
        oldServices: [],
    };

    componentDidMount() {
        if (this.props.reservation.location) {
            const [garage, floor, seat] = this.props.reservation.location.split('/');
            this.setState({
                garage,
                floor,
                seat,
            });
        }
    }

    getFab = state => {
        switch (state) {
            case State.SubmittedNotActual:
            case State.ReminderSentWaitingForKey:
            case State.CarKeyLeftAndLocationConfirmed:
                return (
                    <Button
                        onClick={this.handleStartWash}
                        variant="fab"
                        color="primary"
                        aria-label="Start wash"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <LocalCarWashIcon />
                    </Button>
                );
            case State.WashInProgress:
                return (
                    <Button
                        onClick={this.handleCompleteWash}
                        variant="fab"
                        color="primary"
                        aria-label="Complete wash"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <DoneIcon />
                    </Button>
                );
            case State.NotYetPaid:
                return (
                    <Button
                        onClick={this.handleConfirmPayment}
                        variant="fab"
                        color="primary"
                        aria-label="Paid"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <MoneyOffIcon />
                    </Button>
                );
            default:
                return null;
        }
    };

    getActions = state => {
        switch (state) {
            case State.SubmittedNotActual:
            case State.ReminderSentWaitingForKey:
            case State.CarKeyLeftAndLocationConfirmed:
                return null;
            case State.WashInProgress:
                return (
                    <React.Fragment>
                        <Button onClick={this.handleBackToWaiting}>Back to waiting</Button>
                    </React.Fragment>
                );
            case State.NotYetPaid:
            case State.Done:
                return (
                    <React.Fragment>
                        <Button onClick={this.handleBackToWash}>Back to wash</Button>
                    </React.Fragment>
                );
            default:
                return null;
        }
    };

    getUnselectedServices = services => {
        const defaultServices = Object.values(Service);
        return defaultServices.filter(s => services.filter(z => z === s).length <= 0);
    };

    handleStartWash = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = State.WashInProgress;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/startwash`, {
            method: 'POST',
        }).then(
            () => {
                this.props.openSnackbar('Wash started.');
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleCompleteWash = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = reservation.private ? State.NotYetPaid : State.Done;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/completewash`, {
            method: 'POST',
        }).then(
            () => {
                this.props.openSnackbar('Wash completed.');
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleConfirmPayment = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = State.Done;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/confirmpayment`, {
            method: 'POST',
        }).then(
            () => {
                this.props.openSnackbar('Payment confirmed.');
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleBackToWaiting = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = State.CarKeyLeftAndLocationConfirmed;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/state/${reservation.state}`, {
            method: 'POST',
        }).then(
            () => {
                this.props.openSnackbar('Wash canceled.');
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleBackToWash = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = State.WashInProgress;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/state/${reservation.state}`, {
            method: 'POST',
        }).then(
            () => {
                this.props.openSnackbar('Wash in progress.');
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleAddComment = () => {
        const reservation = this.props.reservation;
        const oldComment = reservation.carwashComment;
        if (reservation.carwashComment !== null) reservation.carwashComment += `\n ${this.state.commentTextfield}`;
        else reservation.carwashComment = this.state.commentTextfield;
        this.props.updateReservation(reservation);

        const oldTextfield = this.state.commentTextfield;
        this.setState({ commentTextfield: '' });

        apiFetch(`api/reservations/${this.props.reservation.id}/carwashcomment`, {
            method: 'POST',
            body: JSON.stringify(this.state.commentTextfield),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Comment saved.');
            },
            error => {
                reservation.carwashComment = oldComment;
                this.props.updateReservation(reservation);

                this.setState({ commentTextfield: oldTextfield });

                this.props.openSnackbar(error);
            }
        );
    };

    handleToggleMpv = () => {
        const reservation = this.props.reservation;
        const oldMpv = reservation.mpv;
        reservation.mpv = !reservation.mpv;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/mpv`, {
            method: 'POST',
            body: JSON.stringify(reservation.mpv),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar(reservation.mpv ? 'Saved as MPV.' : 'Saved as not MPV.');
            },
            error => {
                reservation.mpv = oldMpv;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleUpdateServices = services => () => {
        this.setState({ editServices: false });
        // if (this.state.oldServices.length === services.length && this.state.oldServices.every(s => services.some(z => z === s))) return;

        apiFetch(`api/reservations/${this.props.reservation.id}/services`, {
            method: 'POST',
            body: JSON.stringify(services),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.setState({ oldServices: [] });
                this.props.openSnackbar('Updated selected services.');
            },
            error => {
                const reservation = this.props.reservation;
                reservation.services = this.state.oldServices;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleEditServices = () => {
        this.setState({
            editServices: true,
            oldServices: this.props.reservation.services,
        });
    };

    handleAddService = service => () => {
        const reservation = this.props.reservation;
        reservation.services.push(service);

        // if carpet, must include exterior and interior too
        if (service === Service.Carpet) {
            if (reservation.services.filter(s => s === Service.Exterior).length <= 0) reservation.services.push(Service.Exterior);
            if (reservation.services.filter(s => s === Service.Interior).length <= 0) reservation.services.push(Service.Interior);
        }

        // cannot have both AC cleaning
        if (service === Service.AcCleaningBomba && reservation.services.filter(s => s === Service.AcCleaningOzon).length > 0) {
            const serviceToRemove = reservation.services.indexOf(Service.AcCleaningOzon);
            if (serviceToRemove !== -1) reservation.services.splice(serviceToRemove, 1);
        }
        if (service === Service.AcCleaningOzon && reservation.services.filter(s => s === Service.AcCleaningBomba).length > 0) {
            const serviceToRemove = reservation.services.indexOf(Service.AcCleaningBomba);
            if (serviceToRemove !== -1) reservation.services.splice(serviceToRemove, 1);
        }

        this.props.updateReservation(reservation);
    };

    handleRemoveService = service => () => {
        const reservation = this.props.reservation;
        const serviceToDelete = reservation.services.indexOf(service);
        reservation.services.splice(serviceToDelete, 1);

        // if carpet, must include exterior and interior too
        if (service === Service.Exterior || service === Service.Interior) {
            const carpet = reservation.services.indexOf(Service.Carpet);
            if (carpet !== -1) reservation.services.splice(carpet, 1);
        }

        this.props.updateReservation(reservation);
    };

    handleUpdateLocation = () => {
        const validationErrors = {
            garage: this.state.garage === '',
            floor: this.state.floor === '',
        };

        if (validationErrors.vehiclePlateNumber || validationErrors.garage || validationErrors.floor) {
            this.setState({ validationErrors });
            return;
        }

        const reservation = this.props.reservation;
        const oldLocation = reservation.location;
        reservation.location = `${this.state.garage}/${this.state.floor}/${this.state.seat}`;

        this.setState({ editLocation: false });

        if (oldLocation === reservation.location) return;

        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/location`, {
            method: 'POST',
            body: JSON.stringify(reservation.location),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Updated vehicle location.');
            },
            error => {
                reservation.location = oldLocation;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleCommentChange = event => {
        this.setState({ commentTextfield: event.target.value });
    };

    handleCommentKeyPress = event => {
        if (event.key === 'Enter') this.handleAddComment();
    };

    handleEditLocation = () => {
        this.setState({ editLocation: true });
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

    handleClose = () => {
        this.setState({ editLocation: false, editServices: false });
        this.props.handleClose();
    };

    preventDefault = event => {
        event.preventDefault();
    };

    render() {
        const { editLocation, garage, floor, seat, validationErrors, editServices } = this.state;
        const { reservation, open, snackbarOpen, fullScreen, classes } = this.props;

        return (
            <Dialog open={open} onClose={this.handleClose} fullScreen={fullScreen}>
                <DialogContent className={classes.details}>
                    <div className={classes.closeButton}>
                        <IconButton onClick={this.handleToggleMpv} aria-label="MPV">
                            {reservation.mpv ? <LocalShippingIcon /> : <LocalShippingOutlinedIcon />}
                        </IconButton>
                        <IconButton onClick={this.handleClose} aria-label="Close">
                            <CloseIcon />
                        </IconButton>
                    </div>
                    <Typography variant="display2">{reservation.vehiclePlateNumber}</Typography>
                    <Typography variant="body1" color="textSecondary" component="span" style={{ margin: '8px 0' }}>
                        {getAdminStateName(reservation.state)} • {getDate(reservation)} • {reservation.user.firstName} {reservation.user.lastName} •{' '}
                        {reservation.user.company}
                    </Typography>
                    {!editLocation ? (
                        <Typography variant="subheading" gutterBottom>
                            {reservation.location ? formatLocation(reservation.location) : 'Location not set'}
                            <IconButton onClick={this.handleEditLocation} aria-label="Edit location">
                                <EditIcon />
                            </IconButton>
                        </Typography>
                    ) : (
                        <React.Fragment>
                            <FormControl className={classes.formControl} error={validationErrors.garage}>
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
                            {floor !== '' && (
                                <React.Fragment>
                                    <TextField
                                        id="seat"
                                        label="Seat (optional)"
                                        value={seat}
                                        className={classes.textField}
                                        margin="normal"
                                        onChange={this.handleSeatChange}
                                    />
                                    <IconButton onClick={this.handleUpdateLocation} aria-label="Save location">
                                        <SaveIcon />
                                    </IconButton>
                                </React.Fragment>
                            )}
                        </React.Fragment>
                    )}
                    <div className={classes.comments}>
                        <Comments
                            commentOutgoing={reservation.carwashComment}
                            commentIncoming={reservation.comment}
                            commentIncomingName={reservation.user.firstName}
                            incomingFirst
                        />
                        <FormControl className={classes.commentTextfield}>
                            <InputLabel htmlFor="comment">Reply</InputLabel>
                            <Input
                                id="comment"
                                type="text"
                                value={this.state.commentTextfield}
                                onChange={this.handleCommentChange}
                                onKeyPress={this.handleCommentKeyPress}
                                endAdornment={
                                    <InputAdornment position="end">
                                        {this.state.commentTextfield && (
                                            <IconButton aria-label="Save comment" onClick={this.handleAddComment} onMouseDown={this.preventDefault}>
                                                <SendIcon />
                                            </IconButton>
                                        )}
                                    </InputAdornment>
                                }
                            />
                        </FormControl>
                    </div>
                    <Typography variant="subheading" className={classes.subheader}>
                        Selected services
                    </Typography>
                    {!editServices ? (
                        <React.Fragment>
                            {reservation.services.map(service => (
                                <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                            ))}
                            <IconButton onClick={this.handleEditServices} aria-label="Add service">
                                <EditIcon />
                            </IconButton>
                        </React.Fragment>
                    ) : (
                        <React.Fragment>
                            {reservation.services.map(service => (
                                <Chip label={getServiceName(service)} className={classes.chip} key={service} onDelete={this.handleRemoveService(service)} />
                            ))}
                            {this.getUnselectedServices(reservation.services).map(service => (
                                <Chip
                                    label={getServiceName(service)}
                                    className={classes.unselectedChip}
                                    key={service}
                                    variant="outlined"
                                    onClick={this.handleAddService(service)}
                                />
                            ))}
                            <IconButton onClick={this.handleUpdateServices(reservation.services)} aria-label="Save services">
                                <SaveIcon />
                            </IconButton>
                        </React.Fragment>
                    )}
                    {this.getFab(reservation.state)}
                </DialogContent>
                <DialogActions
                    className={classNames(classes.actions, {
                        [classes.pushActionsUp]: fullScreen && snackbarOpen,
                    })}
                >
                    {this.getActions(reservation.state)}
                </DialogActions>
            </Dialog>
        );
    }
}

CarwashDetailsDialog.propTypes = {
    classes: PropTypes.object.isRequired,
    reservation: PropTypes.object.isRequired,
    fullScreen: PropTypes.bool.isRequired,
    open: PropTypes.bool.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
};

export default withStyles(styles)(withMobileDialog()(CarwashDetailsDialog));
