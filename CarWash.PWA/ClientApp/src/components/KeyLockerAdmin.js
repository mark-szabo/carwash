import { useEffect, useState } from 'react';
import PropTypes from 'prop-types';
import apiFetch from '../Auth';
import { Box, Grid, Typography, Paper, Popover, IconButton, Button } from '@mui/material';
import LockIcon from '@mui/icons-material/Lock';
import LockOpenIcon from '@mui/icons-material/LockOpen';
import WifiOffIcon from '@mui/icons-material/WifiOff';
import CloseIcon from '@mui/icons-material/Close';
import * as moment from 'moment';
import { amber } from '@mui/material/colors';

const getBoxColor = (state, isConnected, isDoorClosed) => {
    if (!isConnected) return null;
    if (isConnected && !isDoorClosed) return amber[200];
    if (isConnected && isDoorClosed && state === 0) return 'green';
    if (isConnected && isDoorClosed && state === 1) return null;
    return null;
};

const getBoxIcon = (state, isConnected, isDoorClosed) => {
    if (!isConnected) return <WifiOffIcon color="disabled" />;
    if (isConnected && !isDoorClosed) return <LockOpenIcon color={state === 2 ? 'disabled' : 'warning'} />;
    if (isConnected && isDoorClosed) return <LockIcon color={state === 2 ? 'disabled' : 'success'} />;
    return <LockIcon color="disabled" />;
};

function KeyLockerAdmin({ user, openSnackbar, closedKeyLockerBoxIds }) {
    const [lockers, setLockers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [anchorEl, setAnchorEl] = useState(null);
    const [selectedBox, setSelectedBox] = useState(null);
    const [openConfirm, setOpenConfirm] = useState(false);
    const [opening, setOpening] = useState(false);

    useEffect(() => {
        apiFetch('api/keylocker/state').then(
            data => {
                setLockers(data);
                setLoading(false);
            },
            error => {
                setLoading(false);
                openSnackbar(error);
            }
        );
    }, [openSnackbar, closedKeyLockerBoxIds]);

    const handleBoxClick = (event, box) => {
        setAnchorEl(event.currentTarget);
        setSelectedBox(box);
        setOpenConfirm(false);
        setOpening(false);
    };

    const handlePopoverClose = () => {
        setAnchorEl(null);
        setOpenConfirm(false);
        setOpening(false);
    };

    const handleOpenBoxClick = async () => {
        if (!openConfirm) {
            setOpenConfirm(true);
            return;
        }
        if (!selectedBox) return;
        setOpening(true);
        try {
            await apiFetch(`api/keylocker/box/${selectedBox.boxId}/open`, { method: 'POST' });
            openSnackbar('Box opened successfully!');
            setOpenConfirm(false);
        } catch (error) {
            openSnackbar(error.toString());
        } finally {
            setOpening(false);
        }
    };

    if (loading) {
        return <Typography>Loading lockers...</Typography>;
    }

    const open = Boolean(anchorEl);

    return (
        <>
            {lockers.map(locker => (
                <Paper key={locker.lockerId} sx={{ mb: 4, p: 2, maxWidth: 420 }} elevation={3}>
                    <Typography variant="h5">{locker.lockerId}</Typography>
                    <Typography variant="body1" gutterBottom sx={{ mb: 2 }}>
                        Building: {locker.building} | Floor: {locker.floor}
                    </Typography>
                    <Grid container spacing={2}>
                        {locker.boxes.map((box, idx) => (
                            <Grid item xs={2.4} key={box.boxId}>
                                <Button
                                    {...(!box.isConnected && { color: 'error' })}
                                    variant={box.state === 1 || !box.isConnected ? 'outlined' : 'contained'}
                                    startIcon={getBoxIcon(box.state, box.isConnected, box.isDoorClosed)}
                                    onClick={e => handleBoxClick(e, box)}
                                    disabled={box.state === 2}
                                    sx={{
                                        bgcolor: getBoxColor(box.state, box.isConnected, box.isDoorClosed),
                                        color: 'white',
                                        width: 56,
                                        height: 56,
                                        borderRadius: 2,
                                        boxShadow: 2,
                                    }}
                                >
                                    {box.name}
                                </Button>
                            </Grid>
                        ))}
                    </Grid>
                </Paper>
            ))}
            <Popover
                open={open}
                anchorEl={anchorEl}
                onClose={handlePopoverClose}
                anchorOrigin={{
                    vertical: 'bottom',
                    horizontal: 'center',
                }}
                transformOrigin={{
                    vertical: 'top',
                    horizontal: 'center',
                }}
            >
                <Paper sx={{ p: 2, minWidth: 300, maxWidth: 400 }} elevation={4}>
                    <Typography variant="h6" gutterBottom>
                        Box {selectedBox?.name}
                    </Typography>
                    <Typography variant="body2" gutterBottom>
                        State: {selectedBox?.state}
                        <br />
                        Connected: {selectedBox?.isConnected ? 'Yes' : 'No'}
                        <br />
                        Door Closed: {selectedBox?.isDoorClosed ? 'Yes' : 'No'}
                        <br />
                        Last Modified: {moment.utc(selectedBox?.lastModifiedAt).local().fromNow()}
                        <br />
                        Last Activity: {moment.utc(selectedBox?.lastActivity).local().fromNow()}
                    </Typography>
                    {selectedBox?.reservation ? (
                        <Box sx={{ mt: 2 }}>
                            <Typography variant="subtitle1">Reservation Info</Typography>
                            <Typography variant="body2">
                                Vehicle Plate Number: {selectedBox.reservation.vehiclePlateNumber}
                                <br />
                                Location: {selectedBox.reservation.location}
                                <br />
                                Start:{' '}
                                {selectedBox.reservation.startDate
                                    ? moment.utc(selectedBox.reservation.startDate).local().fromNow()
                                    : '-'}
                            </Typography>
                        </Box>
                    ) : (
                        <Typography variant="body2" sx={{ mt: 2 }} color="text.secondary">
                            No reservation connected.
                        </Typography>
                    )}
                    <Box sx={{ mt: 2, textAlign: 'right', display: 'flex', gap: 1, justifyContent: 'flex-end' }}>
                        <Button
                            variant="contained"
                            color={openConfirm ? 'warning' : 'primary'}
                            onClick={handleOpenBoxClick}
                            disabled={opening}
                        >
                            {opening ? 'Opening...' : openConfirm ? 'Sure?' : 'Open Box'}
                        </Button>
                        <IconButton onClick={handlePopoverClose}>
                            <CloseIcon />
                        </IconButton>
                    </Box>
                </Paper>
            </Popover>
        </>
    );
}

KeyLockerAdmin.propTypes = {
    user: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
    closedKeyLockerBoxIds: PropTypes.arrayOf(PropTypes.string).isRequired,
};

export default KeyLockerAdmin;
