import React, { Component } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { withStyles } from '@material-ui/core/styles';
import Button from '@material-ui/core/Button';
import Typography from '@material-ui/core/Typography';
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions';
import DialogTitle from '@material-ui/core/DialogTitle';
import DialogContent from '@material-ui/core/DialogContent';
import DialogContentText from '@material-ui/core/DialogContentText';
import DeleteForeverIcon from '@material-ui/icons/DeleteForever';
import red from '@material-ui/core/colors/red';
import * as download from 'downloadjs';

const styles = theme => ({
    dangerButton: {
        color: red[300],
        '&:hover': {
            backgroundColor: 'rgba(229,115,115,0.08)',
        },
    },
    dangerButtonContained: {
        color: '#FFFFFF',
        backgroundColor: 'rgb(225, 0, 80)',
        '&:hover': {
            backgroundColor: 'rgb(157, 0, 56)',
        },
    },
    center: {
        display: 'grid',
        placeItems: 'center',
        textAlign: 'center',
        height: '80%',
    },
    errorIcon: {
        margin: theme.spacing.unit,
        color: '#BDBDBD',
        width: '100px',
        height: '100px',
    },
    errorText: {
        color: '#9E9E9E',
    },
    link: {
        textDecoration: 'underline',
        color: 'initial',
    },
    title: {
        marginTop: '16px',
    },
});

class Settings extends Component {
    displayName = Settings.name;

    state = {
        userDeleted: false,
        deleteDialogOpen: false,
    };

    handleDeleteDialogOpen = () => {
        this.setState({ deleteDialogOpen: true });
    };

    handleDeleteDialogClose = () => {
        this.setState({ deleteDialogOpen: false });
    };

    handleDeleteConfirmed = () => {
        this.setState({ deleteDialogOpen: false });

        apiFetch(`api/users/${this.props.user.id}`, {
            method: 'DELETE',
        }).then(
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
        apiFetch('api/users/downloadpersonaldata').then(
            data => {
                const dataString = JSON.stringify(data);
                const name = this.props.user.firstName.toLowerCase() + this.props.user.lastName.toLowerCase();
                const date = new Date().toISOString().split('T')[0]; // get the current date in this format: yyyy-mm-dd
                download(dataString, `carwash_${name}_${date}.json`, 'text/json');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    render() {
        const { classes } = this.props;

        if (this.state.userDeleted) {
            return (
                <div className={classes.center}>
                    <div>
                        <DeleteForeverIcon className={classes.errorIcon} />
                        <Typography variant="title" gutterBottom className={classes.errorText}>
                            Account permanently deleted
                        </Typography>
                        <Typography className={classes.errorText}>Please close the app now!</Typography>
                    </div>
                </div>
            );
        }

        return (
            <div>
                <Typography variant="display1" gutterBottom>
                    Notifications
                </Typography>
                <Typography variant="display1" gutterBottom>
                    Thanks to GDPR...
                </Typography>
                <Typography variant="body1" gutterBottom>
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
                <Typography variant="body1" gutterBottom>
                    Your account contains personal data that you have given us. You can download or delete that data below.
                </Typography>
                <Typography variant="title" gutterBottom className={classes.title}>
                    Download your data
                </Typography>
                <Button variant="contained" color="primary" onClick={this.handleDownloadDataClick}>
                    Download
                </Button>
                <Typography variant="title" gutterBottom className={classes.title}>
                    Delete your account and your personal data
                </Typography>
                <Typography variant="body1" gutterBottom>
                    Please keep in mind, that we are required to continue storing your previous reservations including their vehicle registration plates for
                    accounting and auditing purposes.
                </Typography>
                <Button variant="contained" color="primary" className={classes.dangerButtonContained} onClick={this.handleDeleteDialogOpen}>
                    Delete
                </Button>
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
            </div>
        );
    }
}

Settings.propTypes = {
    classes: PropTypes.object.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    user: PropTypes.object,
};

export default withStyles(styles)(Settings);
