import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import ReservationGrid from './ReservationGrid';

export default class Home extends TrackedComponent {
    displayName = 'Home';

    render() {
        const { reservations, reservationsLoading, removeReservation, updateReservation, invokeBacklogHub, openSnackbar, lastSettings } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                updateReservation={updateReservation}
                invokeBacklogHub={invokeBacklogHub}
                lastSettings={lastSettings}
                openSnackbar={openSnackbar}
            />
        );
    }
}

Home.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
};
