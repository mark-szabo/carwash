import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import ReservationGrid from './ReservationGrid';

export default class Home extends TrackedComponent {
    displayName = 'Home';

    render() {
        const {
            reservations,
            configuration,
            reservationsLoading,
            removeReservation,
            updateReservation,
            openSnackbar,
            lastSettings,
            dropoffDeepLink,
        } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                configuration={configuration}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                updateReservation={updateReservation}
                lastSettings={lastSettings}
                openSnackbar={openSnackbar}
                dropoffDeepLink={dropoffDeepLink}
            />
        );
    }
}

Home.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    dropoffDeepLink: PropTypes.bool,
};
