import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Grid from '@material-ui/core/Grid';
import CircularProgress from '@material-ui/core/CircularProgress';
import Typography from '@material-ui/core/Typography';
import CarwashCard from './CarwashCard';
import CardSection from './CardSection';
import { State } from './Constants';

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
    readyText: {
        marginLeft: '58px',
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
        const { classes, backlog, backlogLoading, openSnackbar, updateBacklogItem } = this.props;

        if (backlogLoading) {
            return (
                <div className={classes.progress}>
                    <CircularProgress size={50} />
                </div>
            );
        }

        const yesterdayMidnight = new Date();
        yesterdayMidnight.setHours(0, 0, 0);
        const todayMidnight = new Date();
        todayMidnight.setDate(todayMidnight.getDate() + 1);
        todayMidnight.setHours(0, 0, 0);
        const tomorrowMidnight = new Date();
        tomorrowMidnight.setDate(tomorrowMidnight.getDate() + 2);
        tomorrowMidnight.setHours(0, 0, 0);

        const earlier = backlog.filter(r => r.startDate < yesterdayMidnight);
        const done = backlog.filter(r => (r.state === State.Done || r.state === State.NotYetPaid) && r.startDate < todayMidnight);
        const today = backlog.filter(
            r => r.state !== State.Done && r.state !== State.NotYetPaid && r.startDate < todayMidnight && r.startDate > yesterdayMidnight
        );
        const tomorrow = backlog.filter(r => r.startDate > todayMidnight && r.startDate < tomorrowMidnight);
        const later = backlog.filter(r => r.startDate > tomorrowMidnight);

        return (
            <Grid container direction="row" justify="flex-start" alignItems="flex-start" spacing={16} className={classes.grid}>
                <CardSection title="Earlier">
                    {earlier.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard reservation={reservation} openSnackbar={openSnackbar} updateReservation={updateBacklogItem} />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Done">
                    {done.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard reservation={reservation} openSnackbar={openSnackbar} updateReservation={updateBacklogItem} />
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
                            <CarwashCard reservation={reservation} openSnackbar={openSnackbar} updateReservation={updateBacklogItem} />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Tomorrow">
                    {tomorrow.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard reservation={reservation} openSnackbar={openSnackbar} updateReservation={updateBacklogItem} />
                        </Grid>
                    ))}
                </CardSection>
                <CardSection title="Later">
                    {later.map(reservation => (
                        <Grid item key={reservation.id} className={classes.card}>
                            <CarwashCard reservation={reservation} openSnackbar={openSnackbar} updateReservation={updateBacklogItem} />
                        </Grid>
                    ))}
                </CardSection>
            </Grid>
        );
    }
}

CarwashGrid.propTypes = {
    classes: PropTypes.object.isRequired,
    backlog: PropTypes.arrayOf(PropTypes.object).isRequired,
    backlogLoading: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateBacklogItem: PropTypes.func.isRequired,
};

export default withStyles(styles)(CarwashGrid);
