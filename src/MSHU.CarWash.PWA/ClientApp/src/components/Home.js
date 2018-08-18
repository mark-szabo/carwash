import React, { Component } from 'react';
import apiFetch from '../Auth';
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

    constructor(props) {
        super(props);
        this.state = {
            snackbarOpen: false,
            snackbarMessage: '',
            loading: true,
            reservations: []
        };
    }

    componentDidMount() {
        apiFetch('api/reservations')
            .then((data) => {
                this.setState({ reservations: data, loading: false });
            }, (error) => {
                //this.setState({
                //    snackbarOpen: true,
                //    snackbarMessage: error,
                //    loading: false
                //});
            });
    }

    render() {
        const { classes } = this.props;
        if (this.state.loading) {
            return (<div className={classes.progress}><CircularProgress size={50} /></div>);
        } else {
            return (
                <Grid
                    container
                    direction="row"
                    justify="flex-start"
                    alignItems="flex-start"
                    spacing={16}
                    className={classes.grid}
                >
                    {this.state.reservations.map(reservation =>
                        <Grid item key={reservation.id} className={classes.card} >
                            <ReservationCard reservation={reservation} />
                        </Grid>
                    )}
                </Grid>
            );
        }
    }
}

Home.propTypes = {
    classes: PropTypes.object.isRequired,
};

export default withStyles(styles)(Home);