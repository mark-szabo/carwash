import React, { Component } from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import apiFetch from '../Auth';
import { withStyles } from '@material-ui/core/styles';
import Card from '@material-ui/core/Card';
import CardContent from '@material-ui/core/CardContent';
import CardMedia from '@material-ui/core/CardMedia';
import Button from '@material-ui/core/Button';
import Typography from '@material-ui/core/Typography';
import ButtonBase from '@material-ui/core/ButtonBase';
import Collapse from '@material-ui/core/Collapse';
import Chip from '@material-ui/core/Chip';
import Divider from '@material-ui/core/Divider';
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions';
import DialogTitle from '@material-ui/core/DialogTitle';
import red from '@material-ui/core/colors/red';
import CarwashCardHeader from './CarwashCardHeader';
import CarwashDetailsDialog from './CarwashDetailsDialog';
import { getAdminStateName, getServiceName } from './Constants';
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
        '&:hover': {
            cursor: 'pointer',
        },
    },
    buttonBase: {
        width: '100%',
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
    /* Styles applied to the root element when focused. */
    focused: {},
    collapseTransition: {
        overflow: 'initial',
    },
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
        day: '2-digit',
    }).format(new Date(reservation.startDate));

    return `${from} - ${to} • ${date}`;
}

class CarwashCard extends Component {
    state = {
        detailsDialogOpen: false,
        cancelDialogOpen: false,
        focused: false,
    };

    handleDetailsDialogOpen = () => {
        this.setState({ detailsDialogOpen: true });
    };

    handleDetailsDialogClose = () => {
        this.setState({ detailsDialogOpen: false });
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
                // this.props.removeReservation(this.props.reservation.id);
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    handleFocus = () => {
        this.setState({
            focused: true,
        });
    };

    handleBlur = () => {
        this.setState({
            focused: false,
        });
    };

    render() {
        const { classes, reservation } = this.props;
        const { focused, detailsDialogOpen } = this.state;

        return (
            <React.Fragment>
                <Collapse in className={classes.collapseTransition}>
                    <ButtonBase
                        className={classes.buttonBase}
                        component="div"
                        onFocusVisible={this.handleFocus}
                        onBlur={this.handleBlur}
                        onClick={this.handleDetailsDialogOpen}
                    >
                        <Card
                            className={classNames(classes.card, {
                                [classes.focused]: focused,
                            })}
                            raised={focused}
                        >
                            <CardMedia className={classes.media} image={`/images/state${reservation.state}.png`} />
                            <CarwashCardHeader
                                company={reservation.user.company}
                                title={reservation.vehiclePlateNumber}
                                private={reservation.private}
                                subheader={`${getAdminStateName(reservation.state)} • ${getDate(reservation)}`}
                            />
                            <CardContent>
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
                                    {reservation.user.firstName} {reservation.user.lastName}
                                </Typography>
                                <Comments
                                    commentOutgoing={reservation.carwashComment}
                                    commentIncoming={reservation.comment}
                                    commentIncomingName={reservation.user.firstName}
                                    incomingFirst
                                />
                                <Divider className={classes.divider} />
                                <Typography variant="subheading">Selected services</Typography>
                                {reservation.services.map(service => (
                                    <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                                ))}
                            </CardContent>
                        </Card>
                    </ButtonBase>
                </Collapse>
                <CarwashDetailsDialog
                    reservation={reservation}
                    open={detailsDialogOpen}
                    handleClose={this.handleDetailsDialogClose}
                    openSnackbar={this.props.openSnackbar}
                    updateReservation={this.props.updateReservation}
                />
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
            </React.Fragment>
        );
    }
}

CarwashCard.propTypes = {
    classes: PropTypes.object.isRequired,
    reservation: PropTypes.object.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
};

export default withStyles(styles)(CarwashCard);
