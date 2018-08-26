import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Grid from '@material-ui/core/Grid';
import CircularProgress from '@material-ui/core/CircularProgress';
import Typography from '@material-ui/core/Typography';
import CarwashCard from './CarwashCard';
import CardSection from './CardSection';
import RoadAnimation from './RoadAnimation';

const styles = theme => ({
    card: {
        [theme.breakpoints.down('sm')]: {
            minWidth: '100%',
            maxWidth: '100%',
        },
        [theme.breakpoints.up('md')]: {
            minWidth: 'inherit',
            maxWidth: 'inherit',
        },
    },
    grid: {
        maxHeight: 'calc(100% - 8px)',
        width: 'calc(100% + 48px)',
        margin: '-24px',
        padding: '8px',
        overflow: 'auto',
    },
    progress: {
        margin: theme.spacing.unit * 2,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
    },
    center: {
        textAlign: 'center',
        height: '80%',
    },
    lonelyText: {
        color: '#9E9E9E',
    },
    lonelyTitle: {
        color: '#9E9E9E',
        marginTop: theme.spacing.unit * 4,
    },
});

class CarwashGrid extends Component {
    displayName = CarwashGrid.name;

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    render() {
        const { classes, reservations, reservationsLoading, removeReservation, openSnackbar } = this.props;

        if (reservationsLoading) {
            return (
                <div className={classes.progress}>
                    <CircularProgress size={50} />
                </div>
            );
        }

        if (reservations.length <= 0) {
            return (
                <div className={classes.center}>
                    <Typography variant="title" gutterBottom className={classes.lonelyTitle}>
                        It's lonely here...
                    </Typography>
                    <Typography className={classes.lonelyText}>Tap the Reserve button on the left to get started.</Typography>
                    <RoadAnimation />
                </div>
            );
        }

        return (
            <Grid container direction="row" justify="flex-start" alignItems="flex-start" spacing={16} className={classes.grid}>
                <CardSection title="Today">
                    {reservations.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                reservations={reservations}
                                removeReservation={removeReservation}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Tomorrow">
                    {reservations.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                reservations={reservations}
                                removeReservation={removeReservation}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Later">
                    {reservations.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                reservations={reservations}
                                removeReservation={removeReservation}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
            </Grid>
        );
    }
}

CarwashGrid.propTypes = {
    classes: PropTypes.object.isRequired,
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(CarwashGrid);
