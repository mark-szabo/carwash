import React, { Component } from 'react';
import PropTypes from 'prop-types';
import ReservationGrid from './ReservationGrid';

export default class Admin extends Component {
    displayName = Admin.name;

    render() {
        const { reservations, reservationsLoading, removeReservation, openSnackbar } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                openSnackbar={openSnackbar}
                admin
            />
        );
    }
}

Admin.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired,
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};
