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
import InputLabel from '@mui/material/InputLabel';
import FormControl from '@mui/material/FormControl';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import Select from '@mui/material/Select';
import { State, BacklogHubMethods } from '../Constants';

const styles = theme => ({
    formControl: {
        marginRight: theme.spacing(1),
        marginBottom: theme.spacing(1),
        marginTop: theme.spacing(2),
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 105,
        },
    },
});

function DropoffDialog({
    classes,
    configuration,
    reservation,
    open,
    onClose,
    updateReservation,
    invokeBacklogHub,
    openSnackbar,
}) {
    const [step, setStep] = useState(1);
    const [garage, setGarage] = useState('');
    const [floor, setFloor] = useState('');
    const [spot, setSpot] = useState('');
    const [validationErrors, setValidationErrors] = useState({ garage: false, floor: false });
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

                // Broadcast using SignalR
                invokeBacklogHub(BacklogHubMethods.ReservationDropoffConfirmed, reservation.id);
            },
            error => {
                reservation.state = oldState;
                updateReservation(reservation);
                openSnackbar(error);
            }
        );
    };

    return (
        <Dialog
            open={open}
            onClose={onClose}
            aria-labelledby="dropoff-dialog-title"
            aria-describedby="dropoff-dialog-title"
        >
            <DialogTitle id="dropoff-dialog-title">
                {step === 1 ? 'Step 1: Confirm location' : 'Step 2: Review & Confirm'}
            </DialogTitle>
            <DialogContent>
                {step === 1 && (
                    <>
                        <DialogContentText>
                            Please drop-off the key at one of the lockers and confirm vehicle location!
                        </DialogContentText>
                        <FormControl className={classes.formControl} error={validationErrors.garage}>
                            <InputLabel id="garageLabel">Building</InputLabel>
                            <Select
                                id="garage"
                                labelId="garageLabel"
                                label="Building"
                                required
                                value={garage}
                                onChange={handleGarageChange}
                                inputProps={{
                                    name: 'garage',
                                    id: 'garage',
                                }}
                            >
                                {configuration.garages.map(g => (
                                    <MenuItem key={g.building} value={g.building}>
                                        {g.building}
                                    </MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        {garage && configuration.garages.some(g => g.building === garage) && (
                            <FormControl className={classes.formControl} error={validationErrors.floor}>
                                <InputLabel id="floorLabel">Floor</InputLabel>
                                <Select
                                    id="floor"
                                    labelId="floorLabel"
                                    label="Floor"
                                    required
                                    value={floor}
                                    onChange={handleFloorChange}
                                    inputProps={{
                                        name: 'floor',
                                        id: 'floor',
                                    }}
                                >
                                    {configuration.garages
                                        .find(g => g.building === garage)
                                        .floors.map(f => (
                                            <MenuItem value={f} key={f}>
                                                {f}
                                            </MenuItem>
                                        ))}
                                </Select>
                            </FormControl>
                        )}
                        {floor && (
                            <TextField
                                id="spot"
                                label="Spot (optional)"
                                value={spot}
                                className={classes.textField}
                                margin="normal"
                                onChange={handleSpotChange}
                            />
                        )}
                    </>
                )}
                {step === 2 && (
                    <>
                        <DialogContentText>Please review your location details before confirming:</DialogContentText>
                        <div>
                            <strong>Building:</strong> {garage}
                        </div>
                        <div>
                            <strong>Floor:</strong> {floor}
                        </div>
                        <div>
                            <strong>Spot:</strong> {spot || <em>(none)</em>}
                        </div>
                    </>
                )}
            </DialogContent>
            <DialogActions>
                {step === 1 ? (
                    <>
                        <Button onClick={onClose} color="primary">
                            Cancel
                        </Button>
                        <Button onClick={handleNext} color="primary" autoFocus>
                            Next
                        </Button>
                    </>
                ) : (
                    <>
                        <Button onClick={handleBack} color="primary">
                            Back
                        </Button>
                        <Button onClick={handleConfirm} color="primary" autoFocus>
                            Confirm
                        </Button>
                    </>
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
    invokeBacklogHub: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(DropoffDialog);
