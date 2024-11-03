import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import ReservationGrid from './ReservationGrid';

export default class Admin extends TrackedComponent {
    displayName = 'Admin';

    render() {
        const { reservations, configuration, reservationsLoading, removeReservation, updateReservation, invokeBacklogHub, openSnackbar, lastSettings } = this.props;

        return (
            <ReservationGrid
                reservations={reservations}
                configuration={configuration}
                reservationsLoading={reservationsLoading}
                removeReservation={removeReservation}
                updateReservation={updateReservation}
                invokeBacklogHub={invokeBacklogHub}
                lastSettings={lastSettings}
                openSnackbar={openSnackbar}
                admin
            />
        );
    }
}

Admin.propTypes = {
    reservations: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types    
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservationsLoading: PropTypes.bool.isRequired,
    removeReservation: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    lastSettings: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
};
