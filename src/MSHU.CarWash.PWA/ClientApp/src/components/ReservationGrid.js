import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Typography from '@material-ui/core/Typography';
import * as moment from 'moment';
import memoize from 'memoize-one';
import AutoSizer from 'react-virtualized/dist/commonjs/AutoSizer';
import Masonry from 'react-virtualized/dist/commonjs/Masonry';
import createCellPositioner from 'react-virtualized/dist/commonjs/Masonry/createCellPositioner';
import { CellMeasurer, CellMeasurerCache } from 'react-virtualized/dist/commonjs/CellMeasurer';
import ReservationCard from './ReservationCard';
import RoadAnimation from './RoadAnimation';
import Spinner from './Spinner';
import { State } from '../Constants';

const CARD_WIDTH = 400;
const CARD_DEFAULT_HEIGHT = 680;
const CARD_GUTTER = 16;

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

        this._cellRenderer = this._cellRenderer.bind(this);
        this._onResize = this._onResize.bind(this);
        this._renderMasonry = this._renderMasonry.bind(this);
    }

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

    _onResize({ width }) {
        this._width = width;

        this._calculateColumnCount();
        this._resetCellPositioner();
        this._masonry.recomputeCellPositions();
    }

    _renderMasonry({ width }) {
        this._width = width;

        this._calculateColumnCount();
        this._initCellPositioner();

        return (
            <Masonry
                // autoHeight={windowScrollerEnabled}
                cellCount={this.props.reservations.length}
                cellMeasurerCache={this._cache}
                cellPositioner={this._cellPositioner}
                cellRenderer={this._cellRenderer}
                height={CARD_DEFAULT_HEIGHT}
                // overscanByPixels={20}
                ref={ref => {
                    this._masonry = ref;
                }}
                // scrollTop={this._scrollTop}
                width={width}
            />
        );
    }

    _calculateColumnCount() {
        this._columnCount = Math.floor(this._width / (CARD_WIDTH + CARD_GUTTER));
    }

    _initCellPositioner() {
        if (typeof this._cellPositioner === 'undefined') {
            this._cellPositioner = createCellPositioner({
                cellMeasurerCache: this._cache,
                columnCount: this._columnCount,
                CARD_WIDTH,
                spacer: CARD_GUTTER,
            });
        }
    }

    _resetCellPositioner() {
        this._cellPositioner.reset({
            columnCount: this._columnCount,
            CARD_WIDTH,
            spacer: CARD_GUTTER,
        });
    }

    _cellRenderer({ index, key, parent, style }) {
        const { reservations, removeReservation, updateReservation, invokeBacklogHub, openSnackbar, lastSettings, admin } = this.props;

        return (
            <CellMeasurer cache={this._cache} index={index} key={key} parent={parent}>
                <ReservationCard
                    reservation={reservations[index]}
                    reservations={reservations}
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
    }

    render() {
        const { classes, reservations, reservationsLoading } = this.props;

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
            <AutoSizer disableHeight height={CARD_DEFAULT_HEIGHT} onResize={this._onResize}>
                {this._renderMasonry}
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
