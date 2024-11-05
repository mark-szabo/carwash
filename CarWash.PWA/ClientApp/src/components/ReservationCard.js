import React, { Component } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { Link } from 'react-router-dom';
import { withStyles } from '@mui/styles';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardMedia from '@mui/material/CardMedia';
import CardHeader from '@mui/material/CardHeader';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Grow from '@mui/material/Grow';
import LockIcon from '@mui/icons-material/Lock';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import InputLabel from '@mui/material/InputLabel';
import FormControl from '@mui/material/FormControl';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import Tooltip from '@mui/material/Tooltip';
import red from '@mui/material/colors/red';
import { getStateName, getServiceName, State, Garages, BacklogHubMethods } from '../Constants';
import { formatLocation, formatDate2 } from '../Helpers';
import Comments from './Comments';
import * as moment from 'moment';
import ErrorBoundary from './ErrorBoundary';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
    },
    divider: {
        margin: '24px 0',
    },
    card: {
        [theme.breakpoints.down('md')]: {
            width: 'calc(100% - 32px)!important',
        },
        [theme.breakpoints.up('md')]: {
            width: 400,
        },
        margin: 16,
        display: 'flex',
        flexDirection: 'column',
        minHeight: 400,
    },
    cardActions: {
        padding: '8px 12px 12px 12px',
    },
    media: {
        height: 0,
        paddingTop: '48.83%', // 512:250
        opacity: 0.75,
    },
    dangerButton: {
        color: theme.palette.mode === 'dark' ? '#CF6679' : red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
    formControl: {
        marginRight: theme.spacing(1),
        marginBottom: theme.spacing(1),
        marginTop: theme.spacing(2),
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 105,
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
                if (moment(reservation.startDate).isSame(moment(), 'day')) {
                    return (
                        <CardActions className={classes.cardActions}>
                            <Button size="small" color="primary" variant="outlined" onClick={this.handleDropoffDialogOpen}>
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
                }

                return (
                    <CardActions className={classes.cardActions}>
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
        const { classes, reservation, configuration, admin, style } = this.props;
        return (
            <ErrorBoundary
                fallback={
                    <Card className={classes.card} style={style}>
                        <CardContent>
                            <Typography>Failed to load card.</Typography>
                        </CardContent>
                    </Card>
                }
            >
                <Grow in>
                    <Card className={classes.card} style={style}>
                        <CardMedia className={classes.media} image={`/images/state${reservation.state}.png`} />
                        <CardHeader
                            action={
                                reservation.private ? (
                                    <Tooltip disableTouchListener title="Private (car is not company-owned)">
                                        <LockIcon alt="Private (car is not company-owned)" style={{ margin: '8px 16px 0 0' }} />
                                    </Tooltip>
                                ) : null
                            }
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
                            {reservation.services.map(serviceId => (
                                <Chip label={getServiceName(configuration, serviceId)} className={classes.chip} key={serviceId} />
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
                        <Button onClick={this.handleCancelDialogClose} color="primary" id="reservationcard-dontcancel-button">
                            Don't cancel
                        </Button>
                        <Button
                            onClick={this.handleCancelConfirmed}
                            color="primary"
                            className={classes.dangerButton}
                            autoFocus
                            id="reservationcard-cancel-button"
                        >
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
                                {configuration.garages.map(g => (
                                    <MenuItem value={g.building}>{g.building}</MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        {garage &&
                            configuration.garages.some(g => g.building === garage) && (
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
                                    {configuration.garages.find(g => g.building === garage).floors.map(item => (
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
                                label="Spot (optional)"
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
            </ErrorBoundary>
        );
    }
}

ReservationCard.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservation: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    updateReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    admin: PropTypes.bool,
};

export default withStyles(styles)(ReservationCard);
