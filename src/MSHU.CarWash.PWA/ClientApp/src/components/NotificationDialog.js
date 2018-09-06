import React from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import registerPush, { askPermission } from '../PushService';
import Dialog from '@material-ui/core/Dialog';
import DialogTitle from '@material-ui/core/DialogTitle';
import DialogContent from '@material-ui/core/DialogContent';
import DialogContentText from '@material-ui/core/DialogContentText';
import DialogActions from '@material-ui/core/DialogActions';
import Button from '@material-ui/core/Button';
import { NotificationChannel } from '../Constants';

export default class NotificationDialog extends React.Component {
    handleEnable = () => {
        askPermission().then(permissionResult => {
            if (permissionResult === 'granted') {
                registerPush();
                this.handleEnabled();
            } else {
                this.handleCanceled();
            }
        });
    };

    handleCanceled = () => {
        apiFetch('api/users/settings/notificationChannel', {
            method: 'PUT',
            body: JSON.stringify(NotificationChannel.Email),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Ok, you will get notifications in email.');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );

        this.props.updateUser('notificationChannel', NotificationChannel.Email);
        this.props.handleClose();
    };

    handleEnabled = () => {
        apiFetch('api/users/settings/notificationChannel', {
            method: 'PUT',
            body: JSON.stringify(NotificationChannel.Push),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                this.props.openSnackbar('Ok, you will get push notifications.');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );

        this.props.updateUser('notificationChannel', NotificationChannel.Push);
        this.props.handleClose();
    };

    render() {
        const { open, handleClose } = this.props;

        if (!('PushManager' in window)) {
            // Push isn't supported on this browser, disable or hide UI.
            return null;
        }

        if (Notification.permission === 'denied') {
            return null;
        }

        if (Notification.permission === 'granted') {
            return null;
        }

        return (
            <Dialog open={open} onClose={handleClose} aria-labelledby="notification-dialog-title" aria-describedby="notification-dialog-title">
                <DialogTitle id="notification-dialog-title">Enable notifications?</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        We'd like to send you notifications to remind you when you need to drop-off your keys, confirm vehicle location or when your car is
                        ready for pick-up. We won't send you email notifications if you enable push notifications. You can change this in the settings.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={this.handleCanceled} color="primary">
                        Cancel
                    </Button>
                    <Button onClick={this.handleEnable} color="primary" autoFocus>
                        Enable
                    </Button>
                </DialogActions>
            </Dialog>
        );
    }
}

NotificationDialog.propTypes = {
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateUser: PropTypes.func.isRequired,
};
