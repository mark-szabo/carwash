import React, { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { Link, useHistory } from 'react-router-dom';
import { withStyles } from '@mui/styles';
import Alert from '@mui/material/Alert';
import Card from '@mui/material/Card';
import CardActions from '@mui/material/CardActions';
import CardContent from '@mui/material/CardContent';
import CardMedia from '@mui/material/CardMedia';
import CardHeader from '@mui/material/CardHeader';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Grow from '@mui/material/Grow';
import LockIcon from '@mui/icons-material/Lock';
import InfoOutlinedIcon from '@mui/icons-material/InfoOutlined';
import ErrorOutlineOutlinedIcon from '@mui/icons-material/ErrorOutlineOutlined';
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
    cardWarning: {
        padding: 0,
    },
    cardWarningAlert: {
        borderRadius: 0,
        backgroundColor: theme.palette.primary.main,
    },
    cardErrorAlert: {
        borderRadius: 0,
    },
    cardWarningAlertIcon: {
        height: 'auto',
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

function ReservationCard(props) {
    const { classes, reservation, configuration, admin, style } = props;
    const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
    const [dropoffDialogOpen, setDropoffDialogOpen] = useState(props.dropoffDialogOpen);
    const [garage, setGarage] = useState('');
    const [floor, setFloor] = useState('');
    const [spot, setSpot] = useState('');
    const [validationErrors, setValidationErrors] = useState({ garage: false, floor: false });
    const history = useHistory();

    useEffect(() => {
        if (reservation.location) {
            const [g, f, s] = reservation.location.split('/');
            setGarage(g || '');
            setFloor(f || '');
            setSpot(s || '');
        }
    }, [props.lastSettings.garage, reservation.location]);

    const getButtons = () => {
        switch (reservation.state) {
            case 0:
            case 1:
                if (moment(reservation.startDate).isSame(moment(), 'day')) {
                    return (
                        <CardActions className={classes.cardActions}>
                            <Button size="small" color="primary" variant="outlined" onClick={handleDropoffDialogOpen}>
                                Confirm key drop-off
                            </Button>
                            <Button component={Link} to={`/reserve/${reservation.id}`} size="small" color="primary">
                                Edit
                            </Button>
                            <Button
                                size="small"
                                color="secondary"
                                className={classes.dangerButton}
                                onClick={handleCancelDialogOpen}
                            >
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
                        <Button
                            size="small"
                            color="secondary"
                            className={classes.dangerButton}
                            onClick={handleCancelDialogOpen}
                        >
                            Cancel
                        </Button>
                    </CardActions>
                );
            default:
                return null;
        }
    };

    const handleCancelDialogOpen = () => {
        setCancelDialogOpen(true);
    };

    const handleCancelDialogClose = () => {
        setCancelDialogOpen(false);
    };

    const handleCancelConfirmed = () => {
        setCancelDialogOpen(false);

        apiFetch(`api/reservations/${reservation.id}`, {
            method: 'DELETE',
        }).then(
            () => {
                props.openSnackbar('Reservation successfully canceled.');

                // Remove deleted reservation from reservations
                props.removeReservation(reservation.id);

                // Broadcast using SignalR
                props.invokeBacklogHub(BacklogHubMethods.ReservationDeleted, reservation.id);
            },
            error => {
                props.openSnackbar(error);
            }
        );
    };

    const handleDropoffDialogOpen = () => {
        setDropoffDialogOpen(true);
    };

    const handleDropoffDialogClose = () => {
        setDropoffDialogOpen(false);
    };

    const handleDropoffConfirmed = () => {
        const errors = {
            garage: garage === '',
            floor: floor === '',
        };

        if (errors.garage || errors.floor) {
            setValidationErrors(errors);
            return;
        }

        reservation.location = `${garage}/${floor}/${spot}`;
        const oldState = reservation.state;
        reservation.state = State.CarKeyLeftAndLocationConfirmed;

        setDropoffDialogOpen(false);

        history.replace('/');

        props.updateReservation(reservation);

        apiFetch(`api/reservations/${reservation.id}/confirmdropoff`, {
            method: 'POST',
            body: JSON.stringify(reservation.location),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                props.openSnackbar('Drop-off and location confirmed.');

                // Broadcast using SignalR
                props.invokeBacklogHub(BacklogHubMethods.ReservationDropoffConfirmed, reservation.id);
            },
            error => {
                reservation.state = oldState;
                props.updateReservation(reservation);
                props.openSnackbar(error);
            }
        );
    };

    const handleGarageChange = event => {
        setGarage(event.target.value);
    };

    const handleFloorChange = event => {
        setFloor(event.target.value);
    };

    const handleSpotChange = event => {
        setSpot(event.target.value);
    };

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
                                    <LockIcon
                                        alt="Private (car is not company-owned)"
                                        style={{ margin: '8px 16px 0 0' }}
                                    />
                                </Tooltip>
                            ) : null
                        }
                        title={getStateName(reservation.state)}
                        subheader={formatDate2(reservation)}
                    />
                    {reservation.state === State.ReminderSentWaitingForKey && moment(reservation.startDate) > moment() && (
                        <CardContent className={classes.cardWarning}>
                            <Alert
                                variant="filled"
                                severity="info"
                                className={classes.cardWarningAlert}
                                icon={<InfoOutlinedIcon className={classes.cardWarningAlertIcon} />}
                            >
                                Drop off the key before {moment(reservation.startDate).format('h:mm A')}
                                or we cannot guarantee completion by {moment(reservation.endDate).format('h:mm A')}.
                            </Alert>
                        </CardContent>
                    )}
                    {reservation.state === State.ReminderSentWaitingForKey && moment(reservation.startDate) < moment() && (
                        <CardContent className={classes.cardWarning}>
                            <Alert
                                variant="filled"
                                severity="error"
                                className={classes.cardErrorAlert}
                                icon={<ErrorOutlineOutlinedIcon className={classes.cardWarningAlertIcon} />}
                            >
                                Key was not dropped off before {moment(reservation.startDate).format('h:mm A')}.
                                Completion by {moment(reservation.endDate).format('h:mm A')} is not guaranteed.
                            </Alert>
                        </CardContent>
                    )}
                    <CardContent>
                        <Typography variant="caption" color="textSecondary" gutterBottom>
                            Vehicle plate number
                        </Typography>
                        <Typography gutterBottom>{reservation.vehiclePlateNumber}</Typography>
                        {reservation.location && (
                            <React.Fragment>
                                <Typography
                                    variant="caption"
                                    color="textSecondary"
                                    gutterBottom
                                    style={{ marginTop: 8 }}
                                >
                                    Location
                                </Typography>
                                <Typography gutterBottom>{formatLocation(reservation.location)}</Typography>
                            </React.Fragment>
                        )}
                        {admin && (
                            <React.Fragment>
                                <Typography
                                    variant="caption"
                                    color="textSecondary"
                                    gutterBottom
                                    style={{ marginTop: 8 }}
                                >
                                    Name
                                </Typography>
                                <Typography gutterBottom>
                                    {reservation.user.firstName} {reservation.user.lastName}
                                </Typography>
                            </React.Fragment>
                        )}
                        <Comments
                            commentOutgoing={reservation.comment}
                            commentIncoming={reservation.carwashComment}
                            commentIncomingName="CarWash"
                        />
                        <Divider className={classes.divider} />
                        <Typography variant="subtitle1">Selected services</Typography>
                        {reservation.services.map(serviceId => (
                            <Chip
                                label={getServiceName(configuration, serviceId)}
                                className={classes.chip}
                                key={serviceId}
                            />
                        ))}
                    </CardContent>
                    {getButtons(reservation, classes)}
                </Card>
            </Grow>
            <Dialog
                open={cancelDialogOpen}
                onClose={handleCancelDialogClose}
                aria-labelledby="cancel-dialog-title"
                aria-describedby="cancel-dialog-title"
            >
                <DialogTitle id="cancel-dialog-title">Cancel this reservation?</DialogTitle>
                <DialogActions>
                    <Button onClick={handleCancelDialogClose} color="primary" id="reservationcard-dontcancel-button">
                        Don't cancel
                    </Button>
                    <Button
                        onClick={handleCancelConfirmed}
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
                open={dropoffDialogOpen}
                onClose={handleDropoffDialogClose}
                aria-labelledby="dropoff-dialog-title"
                aria-describedby="dropoff-dialog-title"
            >
                <DialogTitle id="dropoff-dialog-title">Confirm drop-off and location</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        Please drop-off the key at the reception and confirm vehicle location!
                    </DialogContentText>
                    <FormControl className={classes.formControl} error={validationErrors.garage}>
                        <InputLabel htmlFor="garage">Building</InputLabel>
                        <Select
                            required
                            value={garage}
                            onChange={handleGarageChange}
                            inputProps={{
                                name: 'garage',
                                id: 'garage',
                            }}
                        >
                            {configuration.garages.map(g => (
                                <MenuItem key={g.building} value={g.building}>
                                    {g.building}
                                </MenuItem>
                            ))}
                        </Select>
                    </FormControl>
                    {garage && configuration.garages.some(g => g.building === garage) && (
                        <FormControl className={classes.formControl} error={validationErrors.floor}>
                            <InputLabel htmlFor="floor">Floor</InputLabel>
                            <Select
                                required
                                value={floor}
                                onChange={handleFloorChange}
                                inputProps={{
                                    name: 'floor',
                                    id: 'floor',
                                }}
                            >
                                {configuration.garages
                                    .find(g => g.building === garage)
                                    .floors.map(f => (
                                        <MenuItem value={f} key={f}>
                                            {f}
                                        </MenuItem>
                                    ))}
                            </Select>
                        </FormControl>
                    )}
                    {floor && (
                        <TextField
                            id="spot"
                            label="Spot (optional)"
                            value={spot}
                            className={classes.textField}
                            margin="normal"
                            onChange={handleSpotChange}
                        />
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleDropoffDialogClose} color="primary">
                        Cancel
                    </Button>
                    <Button onClick={handleDropoffConfirmed} color="primary" autoFocus>
                        Confirm
                    </Button>
                </DialogActions>
            </Dialog>
        </ErrorBoundary>
    );
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
    dropoffDialogOpen: PropTypes.bool.isRequired,
    admin: PropTypes.bool,
};

export default withStyles(styles)(ReservationCard);
