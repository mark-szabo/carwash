import { useState } from 'react';
import PropTypes from 'prop-types';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import apiFetch from '../Auth';
import { validatePhoneNumber } from '../Helpers';

function PhoneNumberDialog({ open, handleClose, openSnackbar, updateUser }) {
    const [phoneNumber, setPhoneNumber] = useState('');
    const [error, setError] = useState('');
    const [submitting, setSubmitting] = useState(false);

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
        <Dialog open={open} aria-labelledby="phone-dialog-title">
            <DialogTitle id="phone-dialog-title">Enter your phone number</DialogTitle>
            <DialogContent>
                <DialogContentText gutterBottom>
                    Please provide a valid phone number to continue using the app. This is necessary for sending
                    notifications and for contacting you regarding your reservations.
                </DialogContentText>
                <TextField
                    autoFocus
                    margin="dense"
                    id="phoneNumber"
                    label="Phone Number"
                    type="tel"
                    fullWidth
                    required
                    value={phoneNumber}
                    onChange={handleChange}
                    error={!!error}
                    helperText={error}
                    disabled={submitting}
                    onKeyDown={e => {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            handleSubmit();
                        }
                    }}
                />
            </DialogContent>
            <DialogActions>
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
