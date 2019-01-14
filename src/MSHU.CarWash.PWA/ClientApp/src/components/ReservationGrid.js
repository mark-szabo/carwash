import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Typography from '@material-ui/core/Typography';
import * as moment from 'moment';
import memoize from 'memoize-one';
import { FixedSizeList as List } from 'react-window';
import AutoSizer from 'react-virtualized-auto-sizer';
import ReservationCard from './ReservationCard';
import RoadAnimation from './RoadAnimation';
import Spinner from './Spinner';
import { State } from '../Constants';

const CARD_WIDTH = 416;

const styles = theme => ({
    grid: {
        margin: '-24px',
        padding: '8px',
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
    row: {
        display: 'flex',
        justifyContent: 'space-around',
    },
});

function Row(props) {
    const { data, index, style } = props;
    const { classes, itemsPerRow, reservations, removeReservation, updateReservation, invokeBacklogHub, lastSettings, openSnackbar, admin } = data;

    const items = [];
    const fromIndex = index * itemsPerRow;
    const toIndex = Math.min(fromIndex + itemsPerRow, reservations.length);

    for (let i = fromIndex; i < toIndex; i++) {
        items.push(
            <ReservationCard
                key={i}
                reservation={reservations[i]}
                reservations={reservations}
                removeReservation={removeReservation}
                updateReservation={updateReservation}
                invokeBacklogHub={invokeBacklogHub}
                lastSettings={lastSettings}
                openSnackbar={openSnackbar}
                admin={admin}
            />
        );
    }

    const rowStyle = {
        margin: '8px 8px 0 8px',
        position: style.position,
        width: style.width,
        height: style.height,
        top: style.top,
        left: style.left,
    };

    return (
        <div className={classes.row} style={rowStyle}>
            {items}
        </div>
    );
}

class ReservationGrid extends Component {
    displayName = 'ReservationGrid';

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    getItemData = memoize((classes, itemsPerRow, reservations, removeReservation, updateReservation, invokeBacklogHub, lastSettings, openSnackbar, admin) => ({
        classes,
        itemsPerRow,
        reservations,
        removeReservation,
        updateReservation,
        invokeBacklogHub,
        lastSettings,
        openSnackbar,
        admin,
    }));

    reorderReservations = reservations =>
        reservations
            .filter(r => r.state !== State.Done)
            .sort((r1, r2) => (moment(r1.startDate).isBefore(moment(r2.startDate)) ? -1 : 1))
            .concat(reservations.filter(r => r.state === State.Done));

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

        return (
            <AutoSizer>
                {({ height, width }) => {
                    const itemsPerRow = Math.floor(width / CARD_WIDTH) || 1;
                    const rowCount = Math.ceil(reservations.length / itemsPerRow);
                    const itemData = this.getItemData(
                        classes,
                        itemsPerRow,
                        this.reorderReservations(reservations),
                        removeReservation,
                        updateReservation,
                        invokeBacklogHub,
                        lastSettings,
                        openSnackbar,
                        admin
                    );

                    return (
                        <List height={height} itemCount={rowCount} itemData={itemData} itemSize={600} width={width + 48} className={classes.grid}>
                            {Row}
                        </List>
                    );
                }}
            </AutoSizer>
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
