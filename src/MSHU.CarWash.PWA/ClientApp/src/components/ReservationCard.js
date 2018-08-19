import React, { Component } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { Link } from 'react-router-dom'
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
    comment: {
        clear: 'right',
        float: 'right',
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomLeftRadius: '1.3em',
        backgroundColor: theme.palette.primary.dark,
        color: '#fff',
        padding: '6px 12px',
        margin: '1px 0',
        maxWidth: '85%',
    },
    carwashName: {
        color: 'rgba(0, 0, 0, .40)',
        fontSize: '12px',
        fontWeight: 'normal',
        lineHeight: '1.1',
        marginBottom: '1px',
        overflow: 'hidden',
        paddingLeft: '12px',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
    },
    carwashComment: {
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomRightRadius: '1.3em',
        backgroundColor: '#e0e0e0',
        padding: '6px 12px',
        margin: '1px 0',
        clear: 'left',
        float: 'left',
        maxWidth: '85%',
    },
    after: {
        clear: 'both',
        display: 'block',
        fontSize: 0,
        height: 0,
        lineHeight: 0,
        visibility: 'hidden',
    },
    dangerButton: {
        color: red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
});

function getStateName(state) {
    switch (state) {
        case 0:
            return 'Scheduled';
        case 1:
            return 'Leave the key at reception';
        case 2:
            return 'Waiting';
        case 3:
            return 'Wash in progress';
        case 4:
            return 'You need to pay';
        case 5:
            return 'Done';
        default:
            return 'No info';
    }
}

function getServiceName(service) {
    switch (service) {
        case 0:
            return 'exterior';
        case 1:
            return 'interior';
        case 2:
            return 'carpet';
        case 3:
            return 'spot cleaning';
        case 4:
            return 'vignette removal';
        case 5:
            return 'polishing';
        case 6:
            return 'AC cleaning \'ozon\'';
        case 7:
            return 'AC cleaning \'bomba\'';
        case 8:
            return 'bug removal';
        case 9:
            return 'wheel cleaning';
        case 10:
            return 'tire care';
        case 11:
            return 'leather care';
        case 12:
            return 'plastic care';
        case 13:
            return 'prewash';
        default:
            return 'no info';
    }
}

function getButtons(reservation, classes, handleCancelDialogOpen) {
    switch (reservation.state) {
        case 0:
            return (
                <CardActions>
                    <Button
                        component={Link}
                        to={`/reserve/${reservation.id}`}
                        size="small"
                        color="primary"
                    >Edit</Button>
                    <Button
                        size="small"
                        color="secondary"
                        className={classes.dangerButton}
                        onClick={handleCancelDialogOpen}
                    >Cancel</Button>
                </CardActions>
            );
        case 1:
            return (
                <CardActions>
                    <Button size="small" color="primary">Confirm drop off and location</Button>
                    <Button size="small" color="secondary">Cancel</Button>
                </CardActions>
            );
        case 4:
            return (
                <CardActions>
                    <Button size="small" color="primary">I have already paid</Button>
                </CardActions>
            );
        default:
            return (null);
    }
}

function getComment(comment, classes) {
    if (!comment) return null;
    return (
        <div>
            <Divider className={classes.divider} />
            <Typography component="p" className={classes.comment}>
                {comment}
            </Typography>
            <div className={classes.after}>.</div>
        </div>
    );
}

function getCarwashComment(comment, classes) {
    if (!comment) return null;
    return (
        <div>
            <Typography component="h5" className={classes.carwashName}>
                CarWash
            </Typography>
            <Typography component="p" className={classes.carwashComment}>
                {comment}
            </Typography>
            <div className={classes.after}>.</div>
        </div>
    );
}

function getDate(reservation) {
    const from = new Intl.DateTimeFormat('en-US',
        {
            month: 'long',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(reservation.dateFrom));

    const to = new Intl.DateTimeFormat('en-US',
        {
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(reservation.dateTo));

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
        }).then(() => {
            this.props.openSnackbar('Reservation successfully canceled.');

            // Remove deleted reservation from reservations
            this.props.removeReservation(this.props.reservation.id);
        }, (error) => {
            this.props.openSnackbar(error);
        });
    };

    render() {
        const { classes, reservation } = this.props;
        return (
            <React.Fragment>
                <Grow in={true}>
                    <Card className={classes.card}>
                        <CardMedia
                            className={classes.media}
                            image={`/images/state${reservation.state}.png`}
                        />
                        <CardHeader
                            action={
                                reservation.private ? <LockIcon alt="Private" style={{ margin: '8px 16px 0 0' }} /> : null
                            }
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
                            {getComment(reservation.comment, classes)}
                            {getCarwashComment(reservation.carwashComment, classes)}
                            <Divider className={classes.divider} />
                            <Typography variant="subheading">
                                Selected services
                        </Typography>
                            {reservation.services.map(service =>
                                <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                            )}
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
};

export default withStyles(styles)(ReservationCard);