import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { useHistory } from 'react-router-dom';
import { withStyles } from '@mui/styles';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogContentText from '@mui/material/DialogContentText';
import CircularProgress from '@mui/material/CircularProgress';
import { State } from '../Constants';
import LocationSelector from './LocationSelector';

const styles = theme => ({
    formControl: {
        marginRight: theme.spacing(1),
        marginBottom: theme.spacing(1),
        marginTop: theme.spacing(2),
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 176,
        },
    },
    circularButton: {
        width: 120,
        height: 120,
        borderRadius: '50%',
        fontSize: 28,
        fontWeight: 700,
        marginBottom: 24,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        color: 'rgba(0, 0, 0, 0.87)',
        background: '#80d8ff',
        boxShadow: '0px 3px 1px -2px rgba(0,0,0,0.2),0px 2px 2px 0px rgba(0,0,0,0.14),0px 1px 5px 0px rgba(0,0,0,0.12)',
    },
});

function DropoffDialog({
    classes,
    configuration,
    reservation,
    open,
    onClose,
    updateReservation,
    openSnackbar,
    closedKeyLockerBoxIds,
}) {
    const [step, setStep] = useState(1);
    const [garage, setGarage] = useState('');
    const [floor, setFloor] = useState('');
    const [spot, setSpot] = useState('');
    const [validationErrors, setValidationErrors] = useState({ garage: false, floor: false });
    const [openingLocker, setOpeningLocker] = useState(false);
    const [openedBoxName, setOpenedBoxName] = useState('');
    const [openedBoxId, setOpenedBoxId] = useState('');
    const history = useHistory();

    useEffect(() => {
        if (reservation.location) {
            const [g, f, s] = reservation.location.split('/');
            setGarage(g || '');
            setFloor(f || '');
            setSpot(s || '');
        }
    }, [reservation.location]);

    useEffect(() => {
        if (open) {
            setStep(1);
            setValidationErrors({ garage: false, floor: false });
        }
    }, [open]);

    // When the locker closed event arrives from the SignalR hub,
    // if the DropoffDialog is visible and on step 3 then push to step 4.
    useEffect(() => {
        if (closedKeyLockerBoxIds && closedKeyLockerBoxIds.includes(openedBoxId) && step === 3) {
            setStep(4);
        }
        if (closedKeyLockerBoxIds && !closedKeyLockerBoxIds.includes(openedBoxId) && step === 4) {
            setStep(3); // Go back to step 3 when the box is reopened
        }
    }, [closedKeyLockerBoxIds, reservation.id, step]);

    const handleGarageChange = event => {
        setGarage(event.target.value);
        setFloor('');
        setSpot('');
        setValidationErrors({ ...validationErrors, garage: false });
    };

    const handleFloorChange = event => {
        setFloor(event.target.value);
        setSpot('');
        setValidationErrors({ ...validationErrors, floor: false });
    };

    const handleSpotChange = event => {
        setSpot(event.target.value);
    };

    const handleNext = () => {
        const errors = {
            garage: garage === '',
            floor: floor === '',
        };
        setValidationErrors(errors);

        if (!errors.garage && !errors.floor) {
            setStep(step + 1);
        }
    };

    const handleBack = () => {
        setStep(step - 1);
    };

    const handleConfirm = () => {
        const errors = {
            garage: garage === '',
            floor: floor === '',
        };

        if (errors.garage || errors.floor) {
            setValidationErrors(errors);
            return;
        }

        reservation.location = `${garage}/${floor}/${spot}`;
        const oldState = reservation.state;
        reservation.state = State.CarKeyLeftAndLocationConfirmed;
        reservation.keyLockerBox = {};
        reservation.keyLockerBox.name = openedBoxName;
        reservation.keyLockerBox.boxId = openedBoxId;

        onClose();

        history.replace('/');

        updateReservation(reservation);

        apiFetch(`api/reservations/${reservation.id}/confirmdropoff`, {
            method: 'POST',
            body: JSON.stringify(reservation.location),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            () => {
                openSnackbar('Drop-off and location confirmed.');
            },
            error => {
                reservation.state = oldState;
                updateReservation(reservation);
                openSnackbar(error);
            }
        );
    };

    const handleOpenLocker = async () => {
        setOpeningLocker(true);
        try {
            const response = await apiFetch(
                `api/keylocker/open/available?reservationId=${reservation.id}&location=${garage}/${floor}/${spot}`,
                {
                    method: 'POST',
                }
            );
            if (response && response.name) {
                setOpenedBoxName(response.name);
                setOpenedBoxId(response.boxId);
            }
            openSnackbar('Locker opened. Please place your key inside.');
            setStep(3);
        } catch (error) {
            openSnackbar(error?.message || 'Failed to open locker.');
        } finally {
            setOpeningLocker(false);
        }
    };

    const handleOpenDoorAgain = async () => {
        try {
            await apiFetch(`api/keylocker/open/by-reservation?reservationId=${reservation.id}`, {
                method: 'POST',
            });
            openSnackbar('Locker opened again.');
            setStep(3);
        } catch (error) {
            openSnackbar(error?.message || 'Failed to open locker again.');
        }
    };

    // Cancel handler that frees the locker if on step 3 or 4
    const handleCancel = () => {
        if (step === 3 || step === 4) {
            // Fire and forget, do not block dialog closure
            apiFetch(`api/keylocker/free/by-reservation?reservationId=${reservation.id}`, {
                method: 'POST',
            });
        }
        onClose();
    };

    return (
        <Dialog
            open={open}
            onClose={handleCancel}
            fullWidth={true}
            maxWidth="sm"
            aria-labelledby="dropoff-dialog-title"
            aria-describedby="dropoff-dialog-title"
        >
            <DialogTitle id="dropoff-dialog-title">
                {step === 1
                    ? 'Step 1: Confirm location'
                    : step === 2
                      ? 'Step 2: Open locker'
                      : step === 3
                        ? 'Step 3: Leave key'
                        : 'Step 4: Confirm drop-off'}
            </DialogTitle>
            <DialogContent>
                {step === 1 && (
                    <>
                        <DialogContentText>
                            Please drop-off the key at one of the lockers and confirm vehicle location!
                        </DialogContentText>
                        <LocationSelector
                            configuration={configuration}
                            garage={garage}
                            floor={floor}
                            spot={spot}
                            validationErrors={validationErrors}
                            onGarageChange={handleGarageChange}
                            onFloorChange={handleFloorChange}
                            onSpotChange={handleSpotChange}
                        />
                    </>
                )}
                {step === 2 && (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minHeight: 250 }}>
                        <DialogContentText style={{ marginBottom: 32 }}>
                            Tap the button below to open an available box.
                        </DialogContentText>
                        <Button
                            onClick={handleOpenLocker}
                            disabled={openingLocker}
                            className={classes.circularButton}
                            variant="contained"
                        >
                            {openingLocker ? <CircularProgress size={40} color="inherit" /> : 'OPEN'}
                        </Button>
                        <Button onClick={handleConfirm}>or confirm drop-off to non-locker</Button>
                    </div>
                )}
                {step === 3 && (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minHeight: 250 }}>
                        <DialogContentText style={{ marginBottom: 32 }}>
                            Leave your key in the box and close the door
                        </DialogContentText>
                        <div className={classes.circularButton}>
                            <span style={{ fontSize: 48, fontWeight: 700, lineHeight: 1.1, letterSpacing: 2 }}>
                                {openedBoxName}
                            </span>
                            <span style={{ fontSize: 16, fontWeight: 400 }}>OPENED</span>
                        </div>
                        <DialogContentText style={{ marginBottom: 16 }}>Waiting for door closure...</DialogContentText>
                        <CircularProgress size={40} color="primary" />
                    </div>
                )}
                {step === 4 && (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', minHeight: 250 }}>
                        <DialogContentText style={{ marginBottom: 32 }}>
                            Confirm that the key is in the box.
                        </DialogContentText>
                        <div className={classes.circularButton}>
                            <span style={{ fontSize: 48, fontWeight: 700, lineHeight: 1.1, letterSpacing: 2 }}>
                                {openedBoxName}
                            </span>
                            <span style={{ fontSize: 16, fontWeight: 400 }}>CLOSED</span>
                        </div>
                        <Button onClick={handleOpenDoorAgain}>Open door again</Button>
                    </div>
                )}
            </DialogContent>
            <DialogActions>
                {step === 1 ? (
                    <>
                        <Button onClick={handleCancel} color="primary">
                            Cancel
                        </Button>
                        <Button onClick={handleNext} color="primary" autoFocus>
                            Next
                        </Button>
                    </>
                ) : step === 4 ? (
                    <>
                        <Button onClick={handleCancel} color="primary">
                            Cancel
                        </Button>
                        <Button onClick={handleConfirm} color="primary" variant="contained" autoFocus>
                            Confirm
                        </Button>
                    </>
                ) : (
                    <Button onClick={handleCancel} color="primary">
                        Cancel
                    </Button>
                )}
            </DialogActions>
        </Dialog>
    );
}

DropoffDialog.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    reservation: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    open: PropTypes.bool.isRequired,
    onClose: PropTypes.func.isRequired,
    updateReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    closedKeyLockerBoxIds: PropTypes.arrayOf(PropTypes.string).isRequired,
};

export default withStyles(styles)(DropoffDialog);
