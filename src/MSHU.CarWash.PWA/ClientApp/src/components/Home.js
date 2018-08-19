import React, { Component } from 'react';
import ReservationCard from './ReservationCard';
import Grid from '@material-ui/core/Grid';
import CircularProgress from '@material-ui/core/CircularProgress';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';

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
});

class Home extends Component {
    displayName = Home.name

    render() {
        const { classes, reservations, reservationsLoading, removeReservation, openSnackbar } = this.props;

        if (reservationsLoading) {
            return (<div className={classes.progress}><CircularProgress size={50} /></div>);
        }

        return (
            <Grid
                container
                direction="row"
                justify="flex-start"
                alignItems="flex-start"
                spacing={16}
                className={classes.grid}
            >
                {reservations.map(reservation => (
                    <Grid item key={reservation.id} className={classes.card} >
                        <ReservationCard
                            reservation={reservation}
                            reservations={reservations}
                            removeReservation={removeReservation}
                            openSnackbar={openSnackbar}
                        />
                    </Grid>
                ))}
            </Grid>
        );
    }
}

Home.propTypes = {
    classes: PropTypes.object.isRequired,
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(Home);
