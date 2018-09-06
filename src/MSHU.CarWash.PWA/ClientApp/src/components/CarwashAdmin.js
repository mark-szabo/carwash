import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import CarwashGrid from './CarwashGrid';

export default class CarwashAdmin extends TrackedComponent {
    displayName = CarwashAdmin.name;

    render() {
        const { backlog, backlogLoading, openSnackbar, updateBacklogItem } = this.props;

        return (
            <CarwashGrid
                backlog={backlog}
                backlogLoading={backlogLoading}
                snackbarOpen={this.props.snackbarOpen}
                openSnackbar={openSnackbar}
                updateBacklogItem={updateBacklogItem}
            />
        );
    }
}

CarwashAdmin.propTypes = {
    backlog: PropTypes.arrayOf(PropTypes.object).isRequired, // eslint-disable-line react/forbid-prop-types
    backlogLoading: PropTypes.bool.isRequired,
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateBacklogItem: PropTypes.func.isRequired,
};
