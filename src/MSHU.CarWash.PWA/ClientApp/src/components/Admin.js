import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import ReservationGrid from './ReservationGrid';

export default class Admin extends TrackedComponent {
    displayName = Admin.name;

    render() {
        const { reservations, reservationsLoading, removeReservation, openSnackbar, updateReservation, lastSettings } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                lastSettings={lastSettings}
                openSnackbar={openSnackbar}
                updateReservation={updateReservation}
                admin
            />
        );
    }
}

Admin.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
};
