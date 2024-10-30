﻿import React from 'react';
import TrackedComponent from './TrackedComponent';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import { withStyles } from '@mui/styles';

const styles = theme => ({
    link: {
        textDecoration: 'underline',
        color: theme.palette.text.primary,
    },
    paper: {
        ...theme.mixins.gutters(),
        paddingTop: theme.spacing(2),
        paddingBottom: theme.spacing(2),
        maxWidth: '600px',
        marginBottom: theme.spacing(2),
    },
});

class Support extends TrackedComponent {
    displayName = 'Support';

    render() {
        const { classes } = this.props;

        return (
            <React.Fragment>
                <Paper className={classes.paper} elevation={1}>
                    <Typography variant="h5" component="h3">
                        Contact the Car Wash service provider
                    </Typography>
                    <Typography component="p">
                        If you have any question, request or need to modify your reservation, contact the Car Wash service provider!
                    </Typography>
                    <Typography component="p">
                        Call us (
                        <a href="tel:+36704506612" className={classes.link}>
                            +36 70 701 5803
                        </a>{' '}
                        or{' '}
                        <a href="tel:+36704506612" className={classes.link}>
                            +36 70 450 6612
                        </a>
                        ) or email us (
                        <a href="mailto:mimosonk@gmail.com" className={classes.link}>
                            mimosonk@gmail.com
                        </a>
                        )!
                    </Typography>
                </Paper>
                <Paper className={classes.paper} elevation={1}>
                    <Typography variant="h5" component="h3">
                        Contact the CarWash app support
                    </Typography>
                    <Typography component="p">
                        If you experience any problem with the app, please contact support on{' '}
                        <a href="mailto:support@mimosonk.hu" className={classes.link}>
                            support@mimosonk.hu
                        </a>
                        !
                    </Typography>
                </Paper>
            </React.Fragment>
        );
    }
}

export default withStyles(styles)(Support);
