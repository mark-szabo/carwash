import React, { useState } from 'react';
import PropTypes from 'prop-types';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import apiFetch from '../Auth';
import { PaymentMethod } from '../Constants';

function BillingDetailsDialog({ open, handleClose, openSnackbar, updateUser }) {
    const [billingName, setBillingName] = useState('');
    const [billingAddress, setBillingAddress] = useState('');
    const [paymentMethod, setPaymentMethod] = useState('');
    const [error, setError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    const validate = () => {
        if (!billingName.trim() || !billingAddress.trim() || paymentMethod === '') {
            setError('All fields are required.');
            return false;
        }
        setError('');
        return true;
    };

    const handleSubmit = () => {
        if (!validate()) return;
        setSubmitting(true);
        const settings = {
            billingName,
            billingAddress,
            paymentMethod: parseInt(paymentMethod, 10),
        };
        apiFetch('api/users/settings', {
            method: 'PUT',
            body: JSON.stringify(settings),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                openSnackbar('Billing details updated.');
                updateUser('billingName', billingName);
                updateUser('billingAddress', billingAddress);
                updateUser('paymentMethod', parseInt(paymentMethod, 10));
                handleClose(true);
            },
            error => {
                setError(error);
                setSubmitting(false);
            }
        );
    };

    return (
        <Dialog open={open} onClose={() => handleClose(false)} aria-labelledby="billing-dialog-title">
            <DialogTitle id="billing-dialog-title">Enter your billing details</DialogTitle>
            <DialogContent>
                <DialogContentText>
                    Please provide your billing name, address, and payment method to continue with a private
                    reservation.
                </DialogContentText>
                <TextField
                    autoFocus
                    margin="dense"
                    id="billingName"
                    label="Billing Name"
                    type="text"
                    fullWidth
                    value={billingName}
                    onChange={e => setBillingName(e.target.value)}
                    disabled={submitting}
                />
                <TextField
                    margin="dense"
                    id="billingAddress"
                    label="Billing Address"
                    type="text"
                    fullWidth
                    value={billingAddress}
                    onChange={e => setBillingAddress(e.target.value)}
                    disabled={submitting}
                />
                <TextField
                    margin="dense"
                    id="paymentMethod"
                    label="Payment Method"
                    select
                    fullWidth
                    value={paymentMethod}
                    onChange={e => setPaymentMethod(e.target.value)}
                    disabled={submitting}
                >
                    {Object.entries(PaymentMethod)
                        .filter(([key, value]) => typeof value === 'number' && value !== PaymentMethod.NotSet)
                        .map(([key, value]) => (
                            <MenuItem key={value} value={value}>
                                {key}
                            </MenuItem>
                        ))}
                </TextField>
                {error && <DialogContentText style={{ color: 'red' }}>{error}</DialogContentText>}
            </DialogContent>
            <DialogActions>
                <Button onClick={() => handleClose(false)} color="primary" disabled={submitting}>
                    Cancel
                </Button>
                <Button onClick={handleSubmit} color="primary" disabled={submitting}>
                    Save
                </Button>
            </DialogActions>
        </Dialog>
    );
}

BillingDetailsDialog.propTypes = {
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    updateUser: PropTypes.func.isRequired,
};

export default BillingDetailsDialog;
