import React, { Component } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { Link } from 'react-router-dom';
import { withStyles } from '@material-ui/core/styles';
import Card from '@material-ui/core/Card';
import CardActions from '@material-ui/core/CardActions';
import CardContent from '@material-ui/core/CardContent';
import CardMedia from '@material-ui/core/CardMedia';
import CardHeader from '@material-ui/core/CardHeader';
import Button from '@material-ui/core/Button';
import Typography from '@material-ui/core/Typography';
import Grow from '@material-ui/core/Grow';
import LockIcon from '@material-ui/icons/Lock';
import Chip from '@material-ui/core/Chip';
import Divider from '@material-ui/core/Divider';
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions';
import DialogTitle from '@material-ui/core/DialogTitle';
import DialogContent from '@material-ui/core/DialogContent';
import DialogContentText from '@material-ui/core/DialogContentText';
import InputLabel from '@material-ui/core/InputLabel';
import FormControl from '@material-ui/core/FormControl';
import TextField from '@material-ui/core/TextField';
import MenuItem from '@material-ui/core/MenuItem';
import Select from '@material-ui/core/Select';
import red from '@material-ui/core/colors/red';
import { getStateName, getServiceName, State, Garages, BacklogHubMethods } from '../Constants';
import { formatLocation, formatDate2 } from '../Helpers';
import Comments from './Comments';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
    },
    divider: {
        margin: '24px 0',
    },
    card: {
        [theme.breakpoints.down('sm')]: {
            minWidth: '100%',
            maxWidth: '100%',
        },
        [theme.breakpoints.up('md')]: {
            minWidth: 400,
            maxWidth: 400,
        },
    },
    media: {
        height: 0,
        paddingTop: '48.83%', // 512:250
    },
    dangerButton: {
        color: red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
    formControl: {
        marginRight: theme.spacing.unit,
        marginBottom: theme.spacing.unit,
        [theme.breakpoints.down('sm')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 75,
        },
    },
});

class ReservationCard extends Component {
    displayName = 'ReservationCard';

    state = {
        cancelDialogOpen: false,
        dropoffDialogOpen: false,
        garage: '',
        floor: '',
        seat: '',
        validationErrors: {
            garage: false,
            floor: false,
        },
    };

    componentDidMount() {
        let garage;
        let floor;
        let seat;

        garage = this.props.lastSettings.garage;
        if (this.props.reservation.location) [garage, floor, seat] = this.props.reservation.location.split('/');

        garage = garage || '';
        floor = floor || '';
        seat = seat || '';

        this.setState({
            garage,
            floor,
            seat,
        });
    }

    getButtons = (reservation, classes) => {
        switch (reservation.state) {
            case 0:
            case 1:
                return (
                    <CardActions>
                        <Button size="small" color="primary" onClick={this.handleDropoffDialogOpen}>
                            Confirm key drop-off
                        </Button>
                        <Button component={Link} to={`/reserve/${reservation.id}`} size="small" color="primary">
                            Edit
                        </Button>
                        <Button size="small" color="secondary" className={classes.dangerButton} onClick={this.handleCancelDialogOpen}>
                            Cancel
                        </Button>
                    </CardActions>
                );
            default:
                return null;
        }
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

                // Broadcast using SignalR
                this.props.invokeBacklogHub(BacklogHubMethods.ReservationDeleted, this.props.reservation.id);
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    handleDropoffDialogOpen = () => {
        this.setState({ dropoffDialogOpen: true });
    };

    handleDropoffDialogClose = () => {
        this.setState({ dropoffDialogOpen: false });
    };

    handleDropoffConfirmed = () => {
        const validationErrors = {
            garage: this.state.garage === '',
            floor: this.state.floor === '',
        };

        if (validationErrors.vehiclePlateNumber || validationErrors.garage || validationErrors.floor) {
            this.setState({ validationErrors });
            return;
        }

        const reservation = this.props.reservation;
        reservation.location = `${this.state.garage}/${this.state.floor}/${this.state.seat}`;
        const oldState = reservation.state;
        reservation.state = State.CarKeyLeftAndLocationConfirmed;

        this.setState({ dropoffDialogOpen: false });

        this.props.updateReservation(reservation);

        apiFetch(`api/reservations/${this.props.reservation.id}/confirmdropoff`, {
            method: 'POST',
            body: JSON.stringify(this.props.reservation.location),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Drop-off and location confirmed.');

                // Broadcast using SignalR
                this.props.invokeBacklogHub(BacklogHubMethods.ReservationDropoffConfirmed, this.props.reservation.id);
            },
            error => {
                reservation.state = oldState;
                this.props.updateReservation(reservation);
                this.props.openSnackbar(error);
            }
        );
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

    render() {
        const { garage, floor, seat, validationErrors } = this.state;
        const { classes, reservation, admin } = this.props;
        return (
            <React.Fragment>
                <Grow in>
                    <Card className={classes.card}>
                        <CardMedia className={classes.media} image={`/images/state${reservation.state}.png`} />
                        <CardHeader
                            action={reservation.private ? <LockIcon alt="Private" style={{ margin: '8px 16px 0 0' }} /> : null}
                            title={getStateName(reservation.state)}
                            subheader={formatDate2(reservation)}
                        />
                        <CardContent>
                            <Typography variant="caption" color="textSecondary" gutterBottom>
                                Vehicle plate number
                            </Typography>
                            <Typography gutterBottom>{reservation.vehiclePlateNumber}</Typography>
                            {reservation.location && (
                                <React.Fragment>
                                    <Typography variant="caption" color="textSecondary" gutterBottom style={{ marginTop: 8 }}>
                                        Location
                                    </Typography>
                                    <Typography gutterBottom>{formatLocation(reservation.location)}</Typography>
                                </React.Fragment>
                            )}
                            {admin && (
                                <React.Fragment>
                                    <Typography variant="caption" color="textSecondary" gutterBottom style={{ marginTop: 8 }}>
                                        Name
                                    </Typography>
                                    <Typography gutterBottom>
                                        {reservation.user.firstName} {reservation.user.lastName}
                                    </Typography>
                                </React.Fragment>
                            )}
                            <Comments commentOutgoing={reservation.comment} commentIncoming={reservation.carwashComment} commentIncomingName="CarWash" />
                            <Divider className={classes.divider} />
                            <Typography variant="subtitle1">Selected services</Typography>
                            {reservation.services.map(service => (
                                <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                            ))}
                        </CardContent>
                        {this.getButtons(reservation, classes, this.handleCancelDialogOpen)}
                    </Card>
                </Grow>
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
                        <Button onClick={this.handleCancelConfirmed} color="primary" className={classes.dangerButton} autoFocus>
                            Cancel
                        </Button>
                    </DialogActions>
                </Dialog>
                <Dialog
                    open={this.state.dropoffDialogOpen}
                    onClose={this.handleDropoffDialogClose}
                    aria-labelledby="dropoff-dialog-title"
                    aria-describedby="dropoff-dialog-title"
                >
                    <DialogTitle id="dropoff-dialog-title">Confirm drop-off and location</DialogTitle>
                    <DialogContent>
                        <DialogContentText>Please drop-off the key at the reception and confirm vehicle location!</DialogContentText>
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
                        {garage &&
                            Garages[garage] && (
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
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={this.handleDropoffDialogClose} color="primary">
                            Cancel
                        </Button>
                        <Button onClick={this.handleDropoffConfirmed} color="primary" autoFocus>
                            Confirm
                        </Button>
                    </DialogActions>
                </Dialog>
            </React.Fragment>
        );
    }
}

ReservationCard.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    updateReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    admin: PropTypes.bool,
};

export default withStyles(styles)(ReservationCard);
