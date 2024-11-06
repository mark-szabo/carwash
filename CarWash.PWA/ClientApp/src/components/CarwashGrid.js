import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Grid from '@mui/material/Grid';
import Typography from '@mui/material/Typography';
import * as moment from 'moment';
import CarwashCard from './CarwashCard';
import CardSection from './CardSection';
import { State } from '../Constants';
import Spinner from './Spinner';

const styles = theme => ({
    card: {
        [theme.breakpoints.down('md')]: {
            minWidth: '100%',
            maxWidth: '100%',
        },
        [theme.breakpoints.up('md')]: {
            minWidth: 'inherit',
            maxWidth: 'inherit',
        },
        padding: '8px',
    },
    grid: {
        maxHeight: 'calc(100% + 48px)',
        width: 'calc(100% + 48px)',
        margin: '-24px',
        padding: '8px',
        overflow: 'auto',
    },
    readyText: {
        marginLeft: '58px',
    },
});

class CarwashGrid extends Component {
    displayName = 'CarwashGrid';

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    render() {
        const {
            classes,
            configuration,
            backlog,
            backlogLoading,
            updateBacklogItem,
            removeBacklogItem,
            invokeBacklogHub,
            openSnackbar,
            snackbarOpen,
        } = this.props;

        if (backlogLoading) {
            return <Spinner />;
        }

        const yesterdayMidnight = moment()
            .hours(0)
            .minutes(0)
            .seconds(0);
        const todayMidnight = moment()
            .hours(0)
            .minutes(0)
            .seconds(0)
            .add(1, 'days');
        const tomorrowMidnight = moment()
            .hours(0)
            .minutes(0)
            .seconds(0)
            .add(2, 'days');

        const earlier = backlog.filter(r => moment(r.startDate).isBefore(yesterdayMidnight));
        const done = backlog.filter(
            r =>
                (r.state === State.Done || r.state === State.NotYetPaid) &&
                moment(r.startDate).isAfter(yesterdayMidnight) &&
                moment(r.startDate).isBefore(todayMidnight)
        );
        const today = backlog.filter(
            r =>
                r.state !== State.Done &&
                r.state !== State.NotYetPaid &&
                moment(r.startDate).isAfter(yesterdayMidnight) &&
                moment(r.startDate).isBefore(todayMidnight)
        );
        const tomorrow = backlog.filter(r => moment(r.startDate).isAfter(todayMidnight) && moment(r.startDate).isBefore(tomorrowMidnight));
        const later = backlog.filter(r => moment(r.startDate).isAfter(tomorrowMidnight));

        return (
            <Grid container direction="row" justifyContent="flex-start" alignItems="flex-start" spacing={1} className={classes.grid}>
                <CardSection title="Earlier">
                    {earlier.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                configuration={configuration}
                                snackbarOpen={snackbarOpen}
                                updateReservation={updateBacklogItem}
                                removeReservation={removeBacklogItem}
                                invokeBacklogHub={invokeBacklogHub}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Done">
                    {done.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                configuration={configuration}
                                snackbarOpen={snackbarOpen}
                                updateReservation={updateBacklogItem}
                                removeReservation={removeBacklogItem}
                                invokeBacklogHub={invokeBacklogHub}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Today" expanded>
                    {today.length <= 0 && (
                        // eslint-disable-next-line
                        <Typography gutterBottom className={classes.readyText}>
                            Yay, there's nothing left for today! 🎉
                        </Typography>
                    )}
                    {today.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                configuration={configuration}
                                snackbarOpen={snackbarOpen}
                                updateReservation={updateBacklogItem}
                                removeReservation={removeBacklogItem}
                                invokeBacklogHub={invokeBacklogHub}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Tomorrow">
                    {tomorrow.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                configuration={configuration}
                                snackbarOpen={snackbarOpen}
                                updateReservation={updateBacklogItem}
                                removeReservation={removeBacklogItem}
                                invokeBacklogHub={invokeBacklogHub}
                                openSnackbar={openSnackbar}
                            />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Later">
                    {later.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard
                                reservation={reservation}
                                configuration={configuration}
                                snackbarOpen={snackbarOpen}
                                updateReservation={updateBacklogItem}
                                removeReservation={removeBacklogItem}
                                invokeBacklogHub={invokeBacklogHub}
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
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    backlog: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    backlogLoading: PropTypes.bool.isRequired,
    updateBacklogItem: PropTypes.func.isRequired,
    removeBacklogItem: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(CarwashGrid);
