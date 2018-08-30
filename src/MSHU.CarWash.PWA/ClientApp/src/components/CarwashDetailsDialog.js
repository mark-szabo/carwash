import React from 'react';
import PropTypes from 'prop-types';
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
import Icon from '@material-ui/core/Icon';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import LocalShippingIcon from '@material-ui/icons/LocalShipping';
import LocalShippingOutlinedIcon from '@material-ui/icons/LocalShippingOutlined';
import CloseIcon from '@material-ui/icons/Close';
import DoneIcon from '@material-ui/icons/Done';
import MoneyOffIcon from '@material-ui/icons/MoneyOff';
import SendIcon from '@material-ui/icons/Send';
import { State, getServiceName } from './Constants';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
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
    },
    closeButton: {
        margin: theme.spacing.unit,
        position: 'absolute',
        top: 0,
        right: 0,
    },
    actions: {
        justifyContent: 'initial',
    },
    notSelectedMpv: {
        color: theme.palette.grey[300],
    },
    commentTextfield: {
        margin: theme.spacing.unit,
    },
});

class CarwashDetailsDialog extends React.Component {
    state = {
        commentTextfield: '',
    };

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
                        className={this.props.classes.button}
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
                        className={this.props.classes.button}
                        autoFocus
                    >
                        <DoneIcon />
                    </Button>
                );
            case State.NotYetPaid:
                return (
                    <Button onClick={this.handleConfirmPayment} variant="fab" color="primary" aria-label="Paid" className={this.props.classes.button} autoFocus>
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
                        <Button onClick={this.handleBackToWaiting}>
                            Back to waiting
                        </Button>
                    </React.Fragment>
                );
            case State.NotYetPaid:
            case State.Done:
                return (
                    <React.Fragment>
                        <Button onClick={this.handleBackToWash}>
                            Back to wash
                        </Button>
                    </React.Fragment>
                );
            default:
                return null;
        }
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

    handleUpdateServices = services => {
        const reservation = this.props.reservation;
        const oldServices = reservation.services;
        reservation.services = services;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/services`, {
            method: 'POST',
            body: JSON.stringify(services),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Updated selected services..');
            },
            error => {
                reservation.services = oldServices;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
    };

    handleUpdateLocation = location => {
        const reservation = this.props.reservation;
        const oldLocation = reservation.location;
        reservation.location = location;
        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/location`, {
            method: 'POST',
            body: JSON.stringify(location),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Updated vehicle location...');
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

    preventDefault = event => {
        event.preventDefault();
    };

    render() {
        const { reservation, open, handleClose, fullScreen, classes } = this.props;

        return (
            <Dialog open={open} onClose={handleClose} fullScreen={fullScreen}>
                <DialogContent className={classes.details}>
                    <IconButton onClick={handleClose} className={classes.closeButton} aria-label="Close">
                        <CloseIcon />
                    </IconButton>
                    <Typography variant="display2" gutterBottom>{reservation.vehiclePlateNumber}
                        <IconButton onClick={this.handleToggleMpv} aria-label="MPV">
                            {reservation.mpv ? <LocalShippingIcon /> : <LocalShippingOutlinedIcon color="disabled" />}
                        </IconButton>
                    </Typography>
                    <Typography variant="caption" gutterBottom>
                        Location
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                        {reservation.location}
                    </Typography>
                    <Typography variant="caption" gutterBottom style={{ marginTop: '8px' }}>
                        Name
                    </Typography>
                    <Typography variant="body1" gutterBottom>
                        {reservation.user.firstName} {reservation.user.lastName} ({reservation.user.company})
                    </Typography>
                    <FormControl className={classes.commentTextfield}>
                        <InputLabel htmlFor="comment">Comment</InputLabel>
                        <Input
                            id="comment"
                            type="text"
                            value={this.state.commentTextfield}
                            onChange={this.handleCommentChange}
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
                    <Typography variant="subheading">Selected services</Typography>
                    {reservation.services.map(service => (
                        <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                    ))}
                    {this.getFab(reservation.state)}
                </DialogContent>
                <DialogActions className={classes.actions}>{this.getActions(reservation.state)}</DialogActions>
            </Dialog>
        );
    }
}

CarwashDetailsDialog.propTypes = {
    classes: PropTypes.object.isRequired,
    reservation: PropTypes.object.isRequired,
    fullScreen: PropTypes.bool.isRequired,
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
};

export default withStyles(styles)(withMobileDialog()(CarwashDetailsDialog));
