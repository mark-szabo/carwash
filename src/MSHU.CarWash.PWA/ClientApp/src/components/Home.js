import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import ReservationGrid from './ReservationGrid';

export default class Home extends TrackedComponent {
    displayName = Home.name;

    render() {
        const { reservations, reservationsLoading, removeReservation, openSnackbar } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                openSnackbar={openSnackbar}
            />
        );
    }
}

Home.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};
