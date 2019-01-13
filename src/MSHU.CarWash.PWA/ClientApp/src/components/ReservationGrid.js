import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Grid from '@material-ui/core/Grid';
import Typography from '@material-ui/core/Typography';
import Portal from '@material-ui/core/Portal';
import InfiniteScroll from 'react-infinite-scroller';
import * as moment from 'moment';
import ReservationCard from './ReservationCard';
import RoadAnimation from './RoadAnimation';
import Spinner from './Spinner';
import { State } from '../Constants';

const NUMBER_OF_ITEMS_DISPLAYED_ON_A_PAGE = 6;

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

class ReservationGrid extends Component {
    displayName = 'ReservationGrid';

    state = {
        displayedReservations: [],
        listHasNotDisplayedItems: true,
    };

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    reorderReservations = reservations =>
        reservations
            .filter(r => r.state !== State.Done)
            .sort((r1, r2) => (moment(r1.startDate).isBefore(moment(r2.startDate)) ? -1 : 1))
            .concat(reservations.filter(r => r.state === State.Done));

    loadReservationsToGrid = page => {
        const allReservations = this.reorderReservations(this.props.reservations);
        const pageMultiplier = page - 1;
        this.setState(state => {
            const displayedReservations = [...state.displayedReservations];
            let listHasNotDisplayedItems = true;
            let lastItemToDisplay = pageMultiplier * NUMBER_OF_ITEMS_DISPLAYED_ON_A_PAGE + NUMBER_OF_ITEMS_DISPLAYED_ON_A_PAGE;

            if (lastItemToDisplay > allReservations.length) {
                lastItemToDisplay = allReservations.length;
                listHasNotDisplayedItems = false;
            }

            for (let i = pageMultiplier * NUMBER_OF_ITEMS_DISPLAYED_ON_A_PAGE; i < lastItemToDisplay; i++) {
                displayedReservations.push(allReservations[i]);
            }

            return { displayedReservations, listHasNotDisplayedItems };
        });
    };

    render() {
        const {
            classes,
            reservations,
            reservationsLoading,
            removeReservation,
            updateReservation,
            invokeBacklogHub,
            openSnackbar,
            lastSettings,
            admin,
        } = this.props;

        if (reservationsLoading) {
            return <Spinner />;
        }

        if (reservations.length <= 0) {
            return (
                <div className={classes.center}>
                    <Typography variant="h6" gutterBottom className={classes.lonelyTitle}>
                        Your reservations will show up here...
                    </Typography>
                    <Typography className={classes.lonelyText}>Tap the Reserve button on the left to get started.</Typography>
                    <RoadAnimation />
                </div>
            );
        }

        const gridItems = this.state.displayedReservations.map(reservation => (
            <Grid item key={reservation.id} className={classes.card}>
                <ReservationCard
                    reservation={reservation}
                    reservations={reservations}
                    removeReservation={removeReservation}
                    updateReservation={updateReservation}
                    invokeBacklogHub={invokeBacklogHub}
                    lastSettings={lastSettings}
                    openSnackbar={openSnackbar}
                    admin={admin}
                />
            </Grid>
        ));

        return (
            <InfiniteScroll
                pageStart={0}
                loadMore={this.loadReservationsToGrid}
                hasMore={this.state.listHasNotDisplayedItems}
                loader={<Spinner key={0} />}
                useWindow={false}
                element={<Grid container direction="row" justify="flex-start" alignItems="flex-start" spacing={16} className={classes.grid} />}
            >
                {gridItems}
            </InfiniteScroll>
        );
    }
}

ReservationGrid.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    admin: PropTypes.bool,
};

export default withStyles(styles)(ReservationGrid);
