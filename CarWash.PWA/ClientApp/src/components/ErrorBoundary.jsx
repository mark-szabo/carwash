import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';

const styles = theme => ({
    center: {
        display: 'grid',
        placeItems: 'center',
        textAlign: 'center',
        height: '80%',
    },
    errorIcon: {
        margin: theme.spacing(1),
        color: '#BDBDBD',
        width: '300px',
        height: '300px',
    },
    errorText: {
        color: '#9E9E9E',
    },
});

class ErrorBoundary extends React.Component {
    static getDerivedStateFromError(error) {
        // Update state so the next render will show the fallback UI.
        return { hasError: true };
    }

    displayName = 'ErrorBoundary';

    constructor(props) {
        super(props);
        this.state = { hasError: false };
    }

    componentDidCatch(error, info) {
        // Log the error to AppInsights
        try {
            appInsights.trackException(error, info.componentStack);
        } catch (e) {
            console.error(`AppInsights is not loaded: ${e}`);
        }
    }

    render() {
        const { classes, disableErrorMessage, fallback } = this.props;

        if (this.state.hasError) {
            if (disableErrorMessage) return null;

            if (fallback) return fallback;

            // Render fallback UI
            return (
                <div className={classes.center}>
                    <div>
                        <img src="images/rain-robot.svg" width="300px" className={classes.errorIcon} alt="" />
                        <Typography variant="h6" gutterBottom className={classes.errorText}>
                            An error has occured
                        </Typography>
                        <Typography className={classes.errorText}>We have logged the issue and are working on it. Give it a try and reload the app!</Typography>
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}

ErrorBoundary.propTypes = {
    disableErrorMessage: PropTypes.bool,
    fallback: PropTypes.node,
};

export default withStyles(styles)(ErrorBoundary);
