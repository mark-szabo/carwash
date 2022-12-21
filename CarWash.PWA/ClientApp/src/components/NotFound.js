import React from 'react';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';

const styles = theme => ({
    center: {
        display: 'grid',
        placeItems: 'center',
        height: '80%',
        [theme.breakpoints.down('md')]: {
            gridTemplateColumns: '1fr',
        },
        [theme.breakpoints.up('md')]: {
            gridTemplateColumns: '1fr 1fr',
        },
    },
    errorIcon: {
        margin: theme.spacing(1),
        color: '#BDBDBD',
        width: '300px',
        height: '300px',
        justifySelf: 'end',
    },
    error: {
        justifySelf: 'start',
    },
    errorText: {
        color: '#9E9E9E',
    },
});

function NotFound(props) {
    const { classes } = props;

    return (
        <div className={classes.center}>
            <img src="images/search-robot.svg" width="300px" className={classes.errorIcon} alt="" />
            <div className={classes.error}>
                <Typography variant="h1" gutterBottom className={classes.errorText}>
                    404!
                </Typography>
                <Typography variant="h5" className={classes.errorText}>
                    I can't seem to find the page you're looking for.
                </Typography>
            </div>
        </div>
    );
}

export default withStyles(styles)(NotFound);
