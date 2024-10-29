import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';
import * as moment from 'moment';
import AutoSizer from 'react-virtualized/dist/commonjs/AutoSizer';
import Masonry from 'react-virtualized/dist/commonjs/Masonry';
import createCellPositioner from 'react-virtualized/dist/commonjs/Masonry/createCellPositioner';
import { CellMeasurer, CellMeasurerCache } from 'react-virtualized/dist/commonjs/CellMeasurer';
import ReservationCard from './ReservationCard';
import RoadAnimation from './RoadAnimation';
import Spinner from './Spinner';
import { State } from '../Constants';
import './ReservationGrid.css';

const CARD_WIDTH = 400;
const CARD_DEFAULT_HEIGHT = 680;
const CARD_GUTTER = 16;

const styles = theme => ({
    grid: {
        margin: '-24px',
        display: 'flex',
        flexGrow: 1,
        height: 'calc(100vh - 56px)',
        [`${theme.breakpoints.up('xs')} and (orientation: landscape)`]: {
            height: 'calc(100vh - 48px)',
        },
        [theme.breakpoints.up('sm')]: {
            height: 'calc(100vh - 64px)',
        },
    },
    masonry: {
        height: 'calc(100vh - 56px)',
        [`${theme.breakpoints.up('xs')} and (orientation: landscape)`]: {
            height: 'calc(100vh - 48px)',
        },
        [theme.breakpoints.up('sm')]: {
            height: 'calc(100vh - 64px)',
        },
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
        marginTop: theme.spacing(4),
    },
});

class ReservationGrid extends React.PureComponent {
    displayName = 'ReservationGrid';

    constructor(props, context) {
        super(props, context);

        this._columnCount = 0;

        this._cache = new CellMeasurerCache({
            defaultHeight: CARD_DEFAULT_HEIGHT,
            defaultWidth: CARD_WIDTH,
            fixedWidth: true,
        });
    }

    componentDidMount() {
        document.getElementsByTagName('main')[0].style.overflow = 'hidden';
    }

    componentDidUpdate() {
        if (!this._cellPositioner) return;

        this.resetCellPositioner();
        this._masonry.recomputeCellPositions();
    }

    componentWillUnmount() {
        document.getElementsByTagName('main')[0].style.overflow = 'auto';
    }

    reorderReservations = reservations =>
        reservations
            .filter(r => r.state !== State.Done)
            .sort((r1, r2) => (moment(r1.startDate).isBefore(moment(r2.startDate)) ? -1 : 1))
            .concat(reservations.filter(r => r.state === State.Done));

    onResize = ({ width }) => {
        this._width = width;

        this.calculateColumnCount();
        this.resetCellPositioner();
        this._masonry.recomputeCellPositions();
    };

    calculateColumnCount() {
        this._columnCount = Math.floor(this._width / (CARD_WIDTH + CARD_GUTTER));
    }

    initCellPositioner() {
        if (typeof this._cellPositioner === 'undefined') {
            this._cellPositioner = createCellPositioner({
                cellMeasurerCache: this._cache,
                columnCount: this._columnCount,
                columnWidth: CARD_WIDTH,
                spacer: CARD_GUTTER,
            });
        }
    }

    resetCellPositioner() {
        this._cellPositioner.reset({
            columnCount: this._columnCount,
            columnWidth: CARD_WIDTH,
            spacer: CARD_GUTTER,
        });
    }

    cellRenderer = ({ index, key, parent, style }) => {
        const reservations = this.reorderReservations(this.props.reservations);
        const { removeReservation, updateReservation, invokeBacklogHub, openSnackbar, lastSettings, admin } = this.props;

        if (!reservations[index]) return null;

        return (
            <CellMeasurer cache={this._cache} index={index} key={key} parent={parent}>
                <ReservationCard
                    reservation={reservations[index]}
                    removeReservation={removeReservation}
                    updateReservation={updateReservation}
                    invokeBacklogHub={invokeBacklogHub}
                    lastSettings={lastSettings}
                    openSnackbar={openSnackbar}
                    admin={admin}
                    style={{
                        ...style,
                        width: CARD_WIDTH,
                    }}
                />
            </CellMeasurer>
        );
    };

    render() {
        const { classes, reservations, reservationsLoading } = this.props;

        if (reservationsLoading) {
            return <Spinner />;
        }

        if (reservations.length <= 0) {
            return (
                <div className={classes.center} id="reservationgrid-lonely">
                    <Typography variant="h6" gutterBottom className={classes.lonelyTitle}>
                        Your reservations will show up here...
                    </Typography>
                    <Typography className={classes.lonelyText}>Tap the Reserve button on the left to get started.</Typography>
                    <RoadAnimation />
                </div>
            );
        }

        return (
            <div className={classes.grid} id="reservationgrid-grid">
                <AutoSizer onResize={this.onResize}>
                    {({ width, height }) => {
                        this._width = width;

                        this.calculateColumnCount();
                        this.initCellPositioner();

                        return (
                            <Masonry
                                cellCount={this.props.reservations.length}
                                cellMeasurerCache={this._cache}
                                cellPositioner={this._cellPositioner}
                                cellRenderer={this.cellRenderer}
                                height={height}
                                ref={ref => {
                                    this._masonry = ref;
                                }}
                                width={width}
                                className={this.props.classes.masonry}
                            />
                        );
                    }}
                </AutoSizer>
            </div>
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
