import React from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import { withStyles } from '@mui/styles';
import { useMediaQuery } from '@mui/material';
import apiFetch from '../Auth';
import Button from '@mui/material/Button';
import Fab from '@mui/material/Fab';
import Dialog from '@mui/material/Dialog';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import red from '@mui/material/colors/red';
import Typography from '@mui/material/Typography';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import LocalCarWashIcon from '@mui/icons-material/LocalCarWash';
import LocalShippingIcon from '@mui/icons-material/LocalShipping';
import LocalShippingOutlinedIcon from '@mui/icons-material/LocalShippingOutlined';
import CloseIcon from '@mui/icons-material/Close';
import DoneIcon from '@mui/icons-material/Done';
import EditIcon from '@mui/icons-material/Edit';
import MoneyOffIcon from '@mui/icons-material/MoneyOff';
import SaveIcon from '@mui/icons-material/Save';
import DeleteForeverIcon from '@mui/icons-material/DeleteForever';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import LockIcon from '@mui/icons-material/Lock';
import { State, getServiceName, getAdminStateName, Service } from '../Constants';
import { formatLocation, formatDate } from '../Helpers';
import Chat from './Chat';
import LocationSelector from './LocationSelector';
import CircularProgress from '@mui/material/CircularProgress';

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
        [theme.breakpoints.down('md')]: {
            minWidth: '100%',
            maxWidth: '100%',
        },
        [theme.breakpoints.up('md')]: {
            minWidth: 600,
            maxWidth: 600,
        },
    },
    button: {
        margin: theme.spacing(1),
        position: 'absolute',
        bottom: theme.spacing(2),
        right: theme.spacing(2),
        '&$pushActionsUp': {
            bottom: theme.spacing(2) + 48,
        },
    },
    closeButton: {
        margin: theme.spacing(1),
        position: 'absolute',
        top: 0,
        right: 0,
    },
    actions: {
        justifyContent: 'initial',
        padding: 16,
    },
    notSelectedMpv: {
        color: theme.palette.grey[300],
    },
    commentTextfield: {
        width: '100%',
    },
    subheader: {
        marginTop: theme.spacing(4),
    },
    saveButton: {
        margin: `${theme.spacing(2)} 0`,
    },
    dangerButton: {
        color: red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
    dialogLine: {
        display: 'flex',
        flexDirection: 'row',
        alignItems: 'center',
        marginTop: theme.spacing(2),
        '& > *:not(:last-child)': {
            marginRight: theme.spacing(2),
        },
    },
});

class CarwashDetailsDialog extends React.Component {
    displayName = 'CarwashDetailsDialog';

    state = {
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
        cancelDialogOpen: false,
        lockerOpening: false,
        waitingForClosure: false,
        lockerBoxId: '',
        lockerBoxName: '',
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

    componentDidUpdate(prevProps, prevState) {
        // Listen for door closure events
        if (
            this.state.waitingForClosure &&
            this.state.lockerBoxId &&
            this.props.closedKeyLockerBoxIds &&
            this.props.closedKeyLockerBoxIds.includes(this.state.lockerBoxId) &&
            (!prevProps.closedKeyLockerBoxIds || !prevProps.closedKeyLockerBoxIds.includes(this.state.lockerBoxId))
        ) {
            // Door just closed
            this.setState({ waitingForClosure: false });
        }
    }

    getFab = state => {
        switch (state) {
            case State.SubmittedNotActual:
            case State.ReminderSentWaitingForKey:
            case State.CarKeyLeftAndLocationConfirmed:
                return (
                    <Fab
                        onClick={this.handleStartWash}
                        color="primary"
                        aria-label="Start wash"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <LocalCarWashIcon />
                    </Fab>
                );
            case State.WashInProgress:
                return (
                    <Fab
                        onClick={this.handleCompleteWash}
                        color="primary"
                        aria-label="Complete wash"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <DoneIcon />
                    </Fab>
                );
            case State.NotYetPaid:
                return (
                    <Fab
                        onClick={this.handleConfirmPayment}
                        color="primary"
                        aria-label="Paid"
                        className={classNames(this.props.classes.button, {
                            [this.props.classes.pushActionsUp]: this.props.fullScreen && this.props.snackbarOpen,
                        })}
                        autoFocus
                    >
                        <MoneyOffIcon />
                    </Fab>
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
                    <>
                        <Button onClick={this.handleBackToWaiting}>Back to waiting</Button>
                    </>
                );
            case State.NotYetPaid:
            case State.Done:
                return (
                    <>
                        <Button onClick={this.handleBackToWash}>Back to wash</Button>
                    </>
                );
            default:
                return null;
        }
    };

    getUnselectedServices = services => {
        const defaultServices = this.props.configuration.services.map(s => s.id);
        return defaultServices.filter(s => services.filter(z => z === s).length <= 0);
    };

    handleStartWash = () => {
        const reservation = this.props.reservation;
        const oldState = reservation.state;
        reservation.state = State.WashInProgress;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/startwash`, { method: 'POST' }, true).then(
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

        apiFetch(`api/reservations/${this.props.reservation.id}/completewash`, { method: 'POST' }, true).then(
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

        apiFetch(`api/reservations/${this.props.reservation.id}/confirmpayment`, { method: 'POST' }, true).then(
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

        apiFetch(
            `api/reservations/${this.props.reservation.id}/state/${reservation.state}`,
            { method: 'POST' },
            true
        ).then(
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

        apiFetch(
            `api/reservations/${this.props.reservation.id}/state/${reservation.state}`,
            { method: 'POST' },
            true
        ).then(
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

    handleToggleMpv = () => {
        const reservation = this.props.reservation;
        const oldMpv = reservation.mpv;
        reservation.mpv = !reservation.mpv;
        this.props.updateReservation(reservation);

        apiFetch(
            `api/reservations/${this.props.reservation.id}/mpv`,
            {
                method: 'POST',
                body: JSON.stringify(reservation.mpv),
                headers: {
                    'Content-Type': 'application/json',
                },
            },
            true
        ).then(
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

        apiFetch(
            `api/reservations/${this.props.reservation.id}/services`,
            {
                method: 'POST',
                body: JSON.stringify(services),
                headers: {
                    'Content-Type': 'application/json',
                },
            },
            true
        ).then(
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
            if (reservation.services.filter(s => s === Service.Exterior).length <= 0)
                reservation.services.push(Service.Exterior);
            if (reservation.services.filter(s => s === Service.Interior).length <= 0)
                reservation.services.push(Service.Interior);
        }

        // cannot have both AC cleaning
        if (
            service === Service.AcCleaningBomba &&
            reservation.services.filter(s => s === Service.AcCleaningOzon).length > 0
        ) {
            const serviceToRemove = reservation.services.indexOf(Service.AcCleaningOzon);
            if (serviceToRemove !== -1) reservation.services.splice(serviceToRemove, 1);
        }
        if (
            service === Service.AcCleaningOzon &&
            reservation.services.filter(s => s === Service.AcCleaningBomba).length > 0
        ) {
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

        apiFetch(
            `api/reservations/${this.props.reservation.id}/location`,
            {
                method: 'POST',
                body: JSON.stringify(reservation.location),
                headers: {
                    'Content-Type': 'application/json',
                },
            },
            true
        ).then(
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

    handleCancelDialogOpen = () => {
        this.setState({ cancelDialogOpen: true });
    };

    handleCancelDialogClose = () => {
        this.setState({ cancelDialogOpen: false });
    };

    handleCancelConfirmed = () => {
        this.setState({ cancelDialogOpen: false });

        apiFetch(`api/reservations/${this.props.reservation.id}`, {
            method: 'DELETE',
        }).then(
            () => {
                this.props.openSnackbar('Reservation successfully canceled.');

                // Remove deleted reservation from reservations
                this.props.removeReservation(this.props.reservation.id);
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    handleOpenLocker = async () => {
        const { reservation, updateReservation, openSnackbar } = this.props;
        this.setState({ lockerOpening: true, lockerBoxName: reservation.keyLockerBox?.name || '' });

        try {
            // Open locker by reservation
            await apiFetch(`api/keylocker/pick-up/by-reservation?reservationId=${reservation.id}&updateState=false`, {
                method: 'POST',
            });

            reservation.keyLockerBox = null;
            updateReservation(reservation);

            openSnackbar('Locker opened.');
        } catch (error) {
            openSnackbar(error?.message || 'Failed to open locker.');
        } finally {
            this.setState({ lockerOpening: false });
        }
    };

    handleDropoffKey = async () => {
        const { reservation, updateReservation, openSnackbar } = this.props;
        this.setState({ lockerOpening: true, waitingForClosure: false });
        try {
            // Open available locker
            const response = await apiFetch(`api/keylocker/open/available?reservationId=${reservation.id}`, {
                method: 'POST',
            });
            if (response && response.name && response.boxId) {
                reservation.keyLockerBox = { name: response.name, boxId: response.boxId };
                updateReservation(reservation);
                this.setState({
                    lockerBoxName: response.name,
                    lockerBoxId: response.boxId,
                    waitingForClosure: true,
                });
                openSnackbar('Locker opened. Place your key inside.');
            }
        } catch (error) {
            openSnackbar(error?.message || 'Failed to open locker.');
        } finally {
            this.setState({ lockerOpening: false });
        }
    };

    render() {
        const {
            editLocation,
            garage,
            floor,
            seat,
            validationErrors,
            editServices,
            lockerOpening,
            waitingForClosure,
            lockerBoxName,
        } = this.state;
        const { reservation, configuration, open, snackbarOpen, classes } = this.props;

        return (
            <>
                <Dialog open={open} onClose={this.handleClose} fullScreen={this.props.fullScreen}>
                    <DialogTitle>{reservation.vehiclePlateNumber}</DialogTitle>
                    <DialogContent className={classes.details}>
                        <div className={classes.closeButton}>
                            <IconButton onClick={this.handleCancelDialogOpen} aria-label="Delete" size="large">
                                <DeleteForeverIcon />
                            </IconButton>
                            <IconButton onClick={this.handleToggleMpv} aria-label="MPV" size="large">
                                {reservation.mpv ? <LocalShippingIcon /> : <LocalShippingOutlinedIcon />}
                            </IconButton>
                            <IconButton onClick={this.handleClose} aria-label="Close" size="large">
                                <CloseIcon />
                            </IconButton>
                        </div>
                        <Typography color="textSecondary" component="span" style={{ margin: '8px 0 32px 0' }}>
                            {getAdminStateName(reservation.state)} • {formatDate(reservation)} •{' '}
                            {reservation.user.firstName} {reservation.user.lastName} • {reservation.user.company}
                        </Typography>
                        <br />
                        {!editLocation ? (
                            <div className={classes.dialogLine}>
                                <Typography variant="subtitle1" gutterBottom>
                                    {reservation.location
                                        ? `Location: ${formatLocation(reservation.location)}`
                                        : 'Location not set'}
                                </Typography>
                                <Button onClick={this.handleEditLocation} variant="outlined" startIcon={<EditIcon />}>
                                    Edit location
                                </Button>
                            </div>
                        ) : (
                            <>
                                <LocationSelector
                                    configuration={configuration}
                                    garage={garage}
                                    floor={floor}
                                    spot={seat}
                                    validationErrors={validationErrors}
                                    onGarageChange={this.handleGarageChange}
                                    onFloorChange={this.handleFloorChange}
                                    onSpotChange={this.handleSeatChange}
                                    textFieldWidth={132}
                                />
                                {floor && (
                                    <IconButton
                                        onClick={this.handleUpdateLocation}
                                        aria-label="Save location"
                                        size="large"
                                        className={classes.saveButton}
                                    >
                                        <SaveIcon />
                                    </IconButton>
                                )}
                            </>
                        )}
                        <div className={classes.dialogLine}>
                            <Typography variant="subtitle1" gutterBottom>
                                {reservation.keyLockerBox
                                    ? `Key locker: ${reservation.keyLockerBox.name}`
                                    : `Key not dropped off ${lockerBoxName ? `(was in ${lockerBoxName})` : ''}`}
                            </Typography>
                            {waitingForClosure ? (
                                <>
                                    <CircularProgress size={32} color="primary" style={{ marginRight: 8 }} />
                                    <Typography variant="body2">Waiting for door closure</Typography>
                                </>
                            ) : reservation.keyLockerBox ? (
                                <Button
                                    onClick={this.handleOpenLocker}
                                    variant="outlined"
                                    startIcon={<LockOpenIcon />}
                                    disabled={lockerOpening}
                                >
                                    Open locker
                                </Button>
                            ) : reservation.state === State.WashInProgress ? (
                                <Button
                                    onClick={this.handleDropoffKey}
                                    variant="outlined"
                                    startIcon={<LockIcon />}
                                    disabled={lockerOpening}
                                >
                                    Drop-off key
                                </Button>
                            ) : null}
                        </div>
                        <Chat
                            carWashChat
                            reservation={reservation}
                            updateReservation={() => {}}
                            openSnackbar={() => {}}
                        />
                        <Typography variant="subtitle1" className={classes.subheader}>
                            Selected services
                        </Typography>
                        {!editServices ? (
                            <>
                                {reservation.services.map(serviceId => (
                                    <Chip
                                        label={getServiceName(configuration, serviceId)}
                                        className={classes.chip}
                                        key={serviceId}
                                    />
                                ))}
                                <IconButton onClick={this.handleEditServices} aria-label="Add service" size="large">
                                    <EditIcon />
                                </IconButton>
                            </>
                        ) : (
                            <>
                                {reservation.services.map(serviceId => (
                                    <Chip
                                        label={getServiceName(configuration, serviceId)}
                                        className={classes.chip}
                                        key={serviceId}
                                        onDelete={this.handleRemoveService(serviceId)}
                                    />
                                ))}
                                {this.getUnselectedServices(reservation.services).map(serviceId => (
                                    <Chip
                                        label={getServiceName(configuration, serviceId)}
                                        className={classes.unselectedChip}
                                        key={serviceId}
                                        variant="outlined"
                                        onClick={this.handleAddService(serviceId)}
                                    />
                                ))}
                                <IconButton
                                    onClick={this.handleUpdateServices(reservation.services)}
                                    aria-label="Save services"
                                    size="large"
                                >
                                    <SaveIcon />
                                </IconButton>
                            </>
                        )}
                        {this.getFab(reservation.state)}
                    </DialogContent>
                    <DialogActions
                        className={classNames(classes.actions, {
                            [classes.pushActionsUp]: this.props.fullScreen && snackbarOpen,
                        })}
                    >
                        {this.getActions(reservation.state)}
                    </DialogActions>
                </Dialog>
                <Dialog
                    open={this.state.cancelDialogOpen}
                    onClose={this.handleCancelDialogClose}
                    aria-labelledby="cancel-dialog-title"
                    aria-describedby="cancel-dialog-title"
                >
                    <DialogTitle id="cancel-dialog-title">Cancel this reservation?</DialogTitle>
                    <DialogActions>
                        <Button onClick={this.handleCancelDialogClose} color="primary">
                            Don't cancel
                        </Button>
                        <Button
                            onClick={this.handleCancelConfirmed}
                            color="primary"
                            className={classes.dangerButton}
                            autoFocus
                        >
                            Cancel
                        </Button>
                    </DialogActions>
                </Dialog>
            </>
        );
    }
}

CarwashDetailsDialog.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservation: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    closedKeyLockerBoxIds: PropTypes.arrayOf(PropTypes.string).isRequired,
};

const withMediaQuery =
    (...args) =>
    Component =>
    props => {
        // eslint-disable-line react/display-name
        const mediaQuery = useMediaQuery(...args);
        return <Component fullScreen={mediaQuery} {...props} />;
    };

export default withStyles(styles)(withMediaQuery(theme => theme.breakpoints.down('sm'))(CarwashDetailsDialog));
