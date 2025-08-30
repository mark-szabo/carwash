import React, { useState } from 'react';
import PropTypes from 'prop-types';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import apiFetch from '../Auth';

function PhoneNumberDialog({ open, handleClose, openSnackbar, updateUser }) {
    const [phoneNumber, setPhoneNumber] = useState('');
    const [error, setError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    const validatePhoneNumber = value => {
        // Simple validation: must be at least 8 digits, only numbers, can start with +
        const regex = /^\+?[0-9]{8,}$/;
        return regex.test(value);
    };

    const handleChange = event => {
        setPhoneNumber(event.target.value);
        setError('');
    };

    const handleSubmit = () => {
        if (!validatePhoneNumber(phoneNumber)) {
            setError('Please enter a valid phone number.');
            return;
        }
        setSubmitting(true);
        apiFetch('api/users/settings/phonenumber', {
            method: 'PUT',
            body: JSON.stringify(phoneNumber),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                openSnackbar('Phone number updated.');
                updateUser('phoneNumber', phoneNumber);
                handleClose();
            },
            error => {
                setError(error);
                setSubmitting(false);
            }
        );
    };

    return (
        <Dialog open={open} onClose={handleClose} aria-labelledby="phone-dialog-title">
            <DialogTitle id="phone-dialog-title">Enter your phone number</DialogTitle>
            <DialogContent>
                <DialogContentText>
                    Please provide a valid phone number to continue using the app. This is required for notifications
                    and account recovery.
                </DialogContentText>
                <TextField
                    autoFocus
                    margin="dense"
                    id="phoneNumber"
                    label="Phone Number"
                    type="tel"
                    fullWidth
                    value={phoneNumber}
                    onChange={handleChange}
                    error={!!error}
                    helperText={error}
                    disabled={submitting}
                />
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} color="primary" disabled={submitting}>
                    Cancel
                </Button>
                <Button onClick={handleSubmit} color="primary" disabled={submitting}>
                    Save
                </Button>
            </DialogActions>
        </Dialog>
    );
}

PhoneNumberDialog.propTypes = {
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateUser: PropTypes.func.isRequired,
};

export default PhoneNumberDialog;
