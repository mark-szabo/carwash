import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import CarwashGrid from './CarwashGrid';

export default class CarwashAdmin extends TrackedComponent {
    displayName = 'CarwashAdmin';

    render() {
        const {
            configuration,
            backlog,
            backlogLoading,
            updateBacklogItem,
            removeBacklogItem,
            openSnackbar,
            snackbarOpen,
            searchTerm,
            closedKeyLockerBoxIds,
        } = this.props;

        return (
            <CarwashGrid
                configuration={configuration}
                backlog={backlog}
                backlogLoading={backlogLoading}
                updateBacklogItem={updateBacklogItem}
                removeBacklogItem={removeBacklogItem}
                snackbarOpen={snackbarOpen}
                openSnackbar={openSnackbar}
                searchTerm={searchTerm}
                closedKeyLockerBoxIds={closedKeyLockerBoxIds}
            />
        );
    }
}

CarwashAdmin.propTypes = {
    backlog: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    backlogLoading: PropTypes.bool.isRequired,
    updateBacklogItem: PropTypes.func.isRequired,
    removeBacklogItem: PropTypes.func.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    searchTerm: PropTypes.string.isRequired,
    closedKeyLockerBoxIds: PropTypes.arrayOf(PropTypes.string).isRequired,
};
