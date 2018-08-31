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
import red from '@material-ui/core/colors/red';
import { getStateName, getServiceName } from './Constants';
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
});

function getButtons(reservation, classes, handleCancelDialogOpen) {
    switch (reservation.state) {
        case 0:
            return (
                <CardActions>
                    <Button component={Link} to={`/reserve/${reservation.id}`} size="small" color="primary">
                        Edit
                    </Button>
                    <Button size="small" color="secondary" className={classes.dangerButton} onClick={handleCancelDialogOpen}>
                        Cancel
                    </Button>
                </CardActions>
            );
        case 1:
            return (
                <CardActions>
                    <Button size="small" color="primary">
                        Confirm drop off and location
                    </Button>
                    <Button size="small" color="secondary">
                        Cancel
                    </Button>
                </CardActions>
            );
        case 4:
            return (
                <CardActions>
                    <Button size="small" color="primary">
                        I have already paid
                    </Button>
                </CardActions>
            );
        default:
            return null;
    }
}

function getDate(reservation) {
    const from = new Intl.DateTimeFormat('en-US', {
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(reservation.startDate));

    const to = new Intl.DateTimeFormat('en-US', {
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(reservation.endDate));

    return `${from} - ${to}`;
}

class ReservationCard extends Component {
    state = {
        cancelDialogOpen: false,
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

    render() {
        const { classes, reservation, admin } = this.props;
        return (
            <React.Fragment>
                <Grow in>
                    <Card className={classes.card}>
                        <CardMedia className={classes.media} image={`/images/state${reservation.state}.png`} />
                        <CardHeader
                            action={reservation.private ? <LockIcon alt="Private" style={{ margin: '8px 16px 0 0' }} /> : null}
                            title={getStateName(reservation.state)}
                            subheader={getDate(reservation)}
                        />
                        <CardContent>
                            <Typography variant="caption" gutterBottom>
                                Vehicle plate number
                            </Typography>
                            <Typography variant="body1" gutterBottom>
                                {reservation.vehiclePlateNumber}
                            </Typography>
                            {admin && (
                                <React.Fragment>
                                    <Typography variant="caption" gutterBottom style={{ marginTop: '8px' }}>
                                        Name
                                    </Typography>
                                    <Typography variant="body1" gutterBottom>
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
                            <Typography variant="subheading">Selected services</Typography>
                            {reservation.services.map(service => (
                                <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                            ))}
                        </CardContent>
                        {getButtons(reservation, classes, this.handleCancelDialogOpen)}
                    </Card>
                </Grow>
                <Dialog
                    open={this.state.cancelDialogOpen}
                    onClose={this.handleCancelDialogClose}
                    aria-labelledby="alert-dialog-title"
                    aria-describedby="alert-dialog-title"
                >
                    <DialogTitle id="alert-dialog-title">Cancel this reservation?</DialogTitle>
                    <DialogActions>
                        <Button onClick={this.handleCancelDialogClose} color="primary">
                            Don't cancel
                        </Button>
                        <Button onClick={this.handleCancelConfirmed} color="primary" className={classes.dangerButton} autoFocus>
                            Cancel
                        </Button>
                    </DialogActions>
                </Dialog>
            </React.Fragment>
        );
    }
}

ReservationCard.propTypes = {
    classes: PropTypes.object.isRequired,
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    removeReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    admin: PropTypes.bool,
};

export default withStyles(styles)(ReservationCard);
