import { Component } from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import apiFetch from '../Auth';
import { withStyles } from '@mui/styles';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardMedia from '@mui/material/CardMedia';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import ButtonBase from '@mui/material/ButtonBase';
import Collapse from '@mui/material/Collapse';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import red from '@mui/material/colors/red';
import CarwashCardHeader from './CarwashCardHeader';
import CarwashDetailsDialog from './CarwashDetailsDialog';
import { getAdminStateName, getServiceName } from '../Constants';
import { formatLocation, formatDate } from '../Helpers';
import Chat from './Chat';
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
        minHeight: 400,
    },
    buttonBase: {
        width: '100%',
    },
    media: {
        height: 0,
        paddingTop: '48.83%', // 512:250
        opacity: 0.75,
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

class CarwashCard extends Component {
    displayName = 'CarwashCard';

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
        const { classes, reservation, configuration } = this.props;
        const { focused, detailsDialogOpen } = this.state;

        return (
            <ErrorBoundary
                fallback={
                    <Card className={classes.card}>
                        <CardContent>
                            <Typography>Failed to load card.</Typography>
                        </CardContent>
                    </Card>
                }
            >
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
                                configuration={configuration}
                                company={reservation.user.company}
                                title={reservation.vehiclePlateNumber}
                                private={reservation.private}
                                subheader={getAdminStateName(reservation.state)}
                                subheaderSecondLine={formatDate(reservation)}
                            />
                            <CardContent>
                                <Typography variant="caption" color="textSecondary" gutterBottom>
                                    Location
                                </Typography>
                                <Typography gutterBottom>
                                    {reservation.location ? formatLocation(reservation.location) : 'Not set'}
                                </Typography>
                                <Typography variant="caption" color="textSecondary" gutterBottom>
                                    Key locker
                                </Typography>
                                <Typography gutterBottom>
                                    {reservation.keyLockerBox ? reservation.keyLockerBox.name : 'Not dropped off'}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    color="textSecondary"
                                    gutterBottom
                                    style={{ marginTop: '8px' }}
                                >
                                    Name
                                </Typography>
                                <Typography gutterBottom>
                                    {reservation.user.firstName} {reservation.user.lastName}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    color="textSecondary"
                                    gutterBottom
                                    style={{ marginTop: '8px' }}
                                >
                                    Company
                                </Typography>
                                <Typography gutterBottom>{reservation.user.company}</Typography>
                                <Chat
                                    carWashChat
                                    hideInput
                                    reservation={reservation}
                                    updateReservation={this.props.updateReservation}
                                    openSnackbar={this.props.openSnackbar}
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
                        </Card>
                    </ButtonBase>
                </Collapse>
                <CarwashDetailsDialog
                    reservation={reservation}
                    configuration={configuration}
                    open={detailsDialogOpen}
                    handleClose={this.handleDetailsDialogClose}
                    updateReservation={this.props.updateReservation}
                    removeReservation={this.props.removeReservation}
                    snackbarOpen={this.props.snackbarOpen}
                    openSnackbar={this.props.openSnackbar}
                    closedKeyLockerBoxIds={this.props.closedKeyLockerBoxIds}
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
            </ErrorBoundary>
        );
    }
}

CarwashCard.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservation: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    updateReservation: PropTypes.func.isRequired,
    removeReservation: PropTypes.func.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    closedKeyLockerBoxIds: PropTypes.arrayOf(PropTypes.string).isRequired,
};

export default withStyles(styles)(CarwashCard);
