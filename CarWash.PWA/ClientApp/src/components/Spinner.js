import React from 'react';
import { withStyles } from '@mui/styles';
import CircularProgress from '@mui/material/CircularProgress';

const styles = theme => ({
    progress: {
        margin: theme.spacing.unit * 2,
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
    },
});

function Spinner(props) {
    const { classes } = props;

    return (
        <div className={classes.progress}>
            <CircularProgress size={50} />
        </div>
    );
}

export default withStyles(styles)(Spinner);
