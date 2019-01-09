import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import CarwashGrid from './CarwashGrid';

export default class CarwashAdmin extends TrackedComponent {
    displayName = 'CarwashAdmin';

    render() {
        const { backlog, backlogLoading, updateBacklogItem, removeBacklogItem, invokeBacklogHub, openSnackbar, snackbarOpen } = this.props;

        return (
            <CarwashGrid
                backlog={backlog}
                backlogLoading={backlogLoading}
                updateBacklogItem={updateBacklogItem}
                removeBacklogItem={removeBacklogItem}
                invokeBacklogHub={invokeBacklogHub}
                snackbarOpen={snackbarOpen}
                openSnackbar={openSnackbar}
            />
        );
    }
}

CarwashAdmin.propTypes = {
    backlog: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    backlogLoading: PropTypes.bool.isRequired,
    updateBacklogItem: PropTypes.func.isRequired,
    removeBacklogItem: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};
