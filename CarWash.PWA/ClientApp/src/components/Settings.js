import React from 'react';
import PropTypes from 'prop-types';
import TrackedComponent from './TrackedComponent';
import apiFetch from '../Auth';
import { withStyles } from '@mui/styles';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Paper from '@mui/material/Paper';
import Dialog from '@mui/material/Dialog';
import Radio from '@mui/material/Radio';
import RadioGroup from '@mui/material/RadioGroup';
import FormControlLabel from '@mui/material/FormControlLabel';
import FormControl from '@mui/material/FormControl';
import Switch from '@mui/material/Switch';
import TextField from '@mui/material/TextField';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DeleteForeverIcon from '@mui/icons-material/DeleteForever';
import red from '@mui/material/colors/red';
import * as download from 'downloadjs';
import * as moment from 'moment';
import { NotificationChannel } from '../Constants';
import registerPush, { askPermission } from '../PushService';
import Spinner from './Spinner';

const styles = theme => ({
    dangerButton: {
        color: theme.palette.mode === 'dark' ? '#CF6679' : red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
    dangerButtonContained: {
        color: '#FFFFFF',
        backgroundColor: theme.palette.mode === 'dark' ? '#CF6679' : 'rgb(225, 0, 80)',
        '&:hover': {
            backgroundColor: 'rgb(157, 0, 56)',
        },
        marginTop: theme.spacing(1),
    },
    primaryButtonContained: {
        marginTop: theme.spacing(1),
    },
    center: {
        display: 'grid',
        placeItems: 'center',
        textAlign: 'center',
        height: '80%',
    },
    errorIcon: {
        margin: theme.spacing(1),
        color: '#BDBDBD',
        width: '100px',
        height: '100px',
    },
    errorText: {
        color: '#9E9E9E',
    },
    link: {
        textDecoration: 'underline',
        color: theme.palette.text.primary,
    },
    title: {
        marginTop: '16px',
    },
    paper: {
        ...theme.mixins.gutters(),
        paddingTop: theme.spacing(2),
        paddingBottom: theme.spacing(2),
        maxWidth: '600px',
        marginBottom: theme.spacing(2),
    },
    group: {
        margin: `${theme.spacing(1)}px 0`,
    },
    formControl: {
        marginTop: theme.spacing(2),
        marginBottom: theme.spacing(2),
    },
    dateField: {
        minWidth: 220,
    },
});

class Settings extends TrackedComponent {
    displayName = 'Settings';

    state = {
        userDeleted: false,
        deleteDialogOpen: false,
        exportStartDate: '',
        exportEndDate: '',
    };

    handleDeleteDialogOpen = () => {
        this.setState({ deleteDialogOpen: true });
    };

    handleDeleteDialogClose = () => {
        this.setState({ deleteDialogOpen: false });
    };

    handleDeleteConfirmed = () => {
        this.setState({ deleteDialogOpen: false });

        localStorage.clear();

        apiFetch(`api/users/${this.props.user.id}`, { method: 'DELETE' }, true).then(
            () => {
                this.props.openSnackbar('Your account has been deleted.');
                this.setState({ userDeleted: true });
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    handleDownloadDataClick = () => {
        apiFetch('api/users/downloadpersonaldata', null, true).then(
            data => {
                const dataString = JSON.stringify(data);
                const name = this.props.user.firstName.toLowerCase() + this.props.user.lastName.toLowerCase();
                const date = moment().format('YYYY-MM-DD'); // get the current date in this format: yyyy-mm-dd
                download(dataString, `carwash_${name}_${date}.json`, 'text/json');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    handleCalendarIntegrationChange = () => {
        const { calendarIntegration } = this.props.user;

        this.props.updateUser('calendarIntegration', !calendarIntegration);
        this.updateSetting('calendarIntegration', !calendarIntegration);
    };

    handleNotificationChannelChange = event => {
        const notificationChannel = parseInt(event.target.value, 10);

        if (notificationChannel === NotificationChannel.Push) {
            if ('PushManager' in window) {
                askPermission().then(permissionResult => {
                    if (permissionResult === 'granted') {
                        this.props.updateUser('notificationChannel', notificationChannel);
                        this.updateSetting('notificationChannel', notificationChannel);
                        registerPush();
                    } else if (permissionResult === 'denied') {
                        this.props.openSnackbar(
                            'You have denied notification permission previouly. You need to go to your browser settings to enable notifications first.'
                        );
                    }
                });
            } else {
                this.props.openSnackbar('Push notifications are not supported by your device! :(');
            }
        } else {
            this.props.updateUser('notificationChannel', notificationChannel);
            this.updateSetting('notificationChannel', notificationChannel);
        }
    };

    handleExportStartDateChange = event => {
        this.setState({
            exportStartDate: event.target.value,
        });
    };

    handleExportEndDateChange = event => {
        this.setState({
            exportEndDate: event.target.value,
        });
    };

    handleExportClick = () => {
        apiFetch(`api/reservations/export?startDate=${this.state.exportStartDate}&endDate=${this.state.exportEndDate}`, null, true).then(
            () => {},
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    updateSetting = (key, value) => {
        apiFetch(
            `api/users/settings/${key}`,
            {
                method: 'PUT',
                body: JSON.stringify(value),
                headers: {
                    'Content-Type': 'application/json',
                },
            },
            true
        ).then(
            () => {
                this.props.openSnackbar('Updates saved.');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    render() {
        const { classes, user } = this.props;

        if (this.state.userDeleted) {
            return (
                <div className={classes.center}>
                    <div>
                        <DeleteForeverIcon className={classes.errorIcon} />
                        <Typography variant="h6" gutterBottom className={classes.errorText}>
                            Account permanently deleted
                        </Typography>
                        <Typography className={classes.errorText}>Please close the app now!</Typography>
                    </div>
                </div>
            );
        }

        if (Object.keys(user).length === 0) {
            return <Spinner />;
        }

        return (
            <React.Fragment>
                <Paper className={classes.paper} elevation={1}>
                    <Typography variant="h5" component="h3">
                        Notifications
                    </Typography>
                    <Typography component="p">How do you want us to remind you to drop off the keys or notify when your car is ready?</Typography>
                    <FormControl component="fieldset">
                        <RadioGroup
                            aria-label="Channel"
                            name="channel"
                            className={classes.group}
                            value={`${user.notificationChannel}`}
                            onChange={this.handleNotificationChannelChange}
                        >
                            <FormControlLabel value="1" control={<Radio />} label="Disable" />
                            <FormControlLabel value="2" control={<Radio />} label="Email" />
                            <FormControlLabel value="3" control={<Radio />} label="Push notification" />
                        </RadioGroup>
                    </FormControl>
                </Paper>
                <Paper className={classes.paper} elevation={1}>
                    <Typography variant="h5" component="h3">
                        Calendar integration
                    </Typography>
                    <Typography component="p">Do you want us to automatically create a (non-blocker) event in your calendar for your reservations?</Typography>
                    <FormControlLabel
                        control={
                            <Switch
                                checked={user.calendarIntegration}
                                onChange={this.handleCalendarIntegrationChange}
                                value="calendarIntegration"
                                color="primary"
                            />
                        }
                        label={user.calendarIntegration ? 'On' : 'Off'}
                    />
                </Paper>
                {(user.isAdmin || user.isCarwashAdmin) && (
                    <Paper className={classes.paper} elevation={1}>
                        <Typography variant="h5" component="h3">
                            Export reservations to Excel
                        </Typography>
                        <Typography component="p">
                            You can export reservations from the database to an Excel file for accounting and auditing pourposes.
                        </Typography>
                        <div className={classes.formControl}>
                            <TextField
                                id="startDate"
                                label="Start date"
                                type="date"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleExportStartDateChange}
                                className={classes.dateField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <TextField
                                id="endDate"
                                label="End date"
                                type="date"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleExportEndDateChange}
                                className={classes.dateField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <Button variant="contained" color="primary" className={classes.primaryButtonContained} onClick={this.handleExportClick}>
                                Export
                            </Button>
                        </div>
                    </Paper>
                )}
                <Paper className={classes.paper} elevation={1}>
                    <Typography variant="h5" component="h3">
                        We take your privacy seriously!
                    </Typography>
                    <Typography component="p">
                        By using this app, you agree to Microsoft's{' '}
                        <a href="https://go.microsoft.com/fwlink/?LinkID=206977" className={classes.link}>
                            Terms of use
                        </a>{' '}
                        and that you have read Microsoft's{' '}
                        <a href="https://go.microsoft.com/fwlink/?LinkId=521839" className={classes.link}>
                            Privacy & cookies policy
                        </a>
                        .
                    </Typography>
                    <Typography component="p">
                        Your account contains personal data that you have given us. You can download or delete that data below.
                    </Typography>
                    <Typography variant="subtitle1" gutterBottom className={classes.title}>
                        Download your data
                    </Typography>
                    <Button variant="contained" color="primary" className={classes.primaryButtonContained} onClick={this.handleDownloadDataClick}>
                        Download
                    </Button>
                    <Typography variant="subtitle1" className={classes.title}>
                        Delete your account and your personal data
                    </Typography>
                    <Typography component="p" gutterBottom>
                        Please keep in mind, that we are required to continue storing your previous reservations including their vehicle registration plates for
                        accounting and auditing purposes.
                    </Typography>
                    <Button variant="contained" color="primary" className={classes.dangerButtonContained} onClick={this.handleDeleteDialogOpen}>
                        Delete
                    </Button>
                </Paper>
                <Dialog
                    open={this.state.deleteDialogOpen}
                    onClose={this.handleDeleteDialogClose}
                    aria-labelledby="alert-dialog-title"
                    aria-describedby="alert-dialog-title"
                >
                    <DialogTitle id="alert-dialog-title">Delete your account?</DialogTitle>
                    <DialogContent>
                        <DialogContentText id="alert-dialog-description">
                            Deleting your data will permanently remove your account, and this cannot be undone.
                        </DialogContentText>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={this.handleDeleteDialogClose} color="primary">
                            Cancel
                        </Button>
                        <Button onClick={this.handleDeleteConfirmed} color="primary" className={classes.dangerButton} autoFocus>
                            Delete
                        </Button>
                    </DialogActions>
                </Dialog>
            </React.Fragment>
        );
    }
}

Settings.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    updateUser: PropTypes.func.isRequired,
    user: PropTypes.object, // eslint-disable-line react/forbid-prop-types
};

export default withStyles(styles)(Settings);
