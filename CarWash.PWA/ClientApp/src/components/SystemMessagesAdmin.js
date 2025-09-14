import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import * as dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import { withStyles } from '@mui/styles';
import apiFetch from '../Auth';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemAvatar from '@mui/material/ListItemAvatar';
import ListItemSecondaryAction from '@mui/material/ListItemSecondaryAction';
import ListItemText from '@mui/material/ListItemText';
import Avatar from '@mui/material/Avatar';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import IconButton from '@mui/material/IconButton';
import DeleteIcon from '@mui/icons-material/Delete';
import Spinner from './Spinner';
import Alert from '@mui/material/Alert';
import { Severity } from '../Constants';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import Select from '@mui/material/Select';
import Paper from '@mui/material/Paper';
import { getSeverityName, format2Dates } from '../Helpers';

dayjs.extend(utc);

const styles = theme => ({
    list: {
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 600,
        },
        backgroundColor: theme.palette.background.paper,
    },
    primaryButtonContained: {
        marginTop: theme.spacing(1),
    },
    formControl: {
        marginTop: theme.spacing(2),
        marginBottom: theme.spacing(3),
    },
    inputField: {
        minWidth: 220,
    },
    paper: {
        ...theme.mixins.gutters(),
        paddingTop: theme.spacing(2),
        paddingBottom: theme.spacing(1),
        marginBottom: theme.spacing(3),
        maxWidth: 600,
    },
});

function SystemMessagesAdmin(props) {
    const { classes, user, openSnackbar } = props;

    const [loading, setLoading] = useState(true);
    const [systemMessages, setSystemMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const [newSeverity, setNewSeverity] = useState(Severity.Info);
    const [newStartDateTimeLocal, setNewStartDateTimeLocal] = useState('');
    const [newEndDateTimeLocal, setNewEndDateTimeLocal] = useState('');

    useEffect(() => {
        apiFetch('api/systemmessages').then(
            data => {
                setSystemMessages(data);
                setLoading(false);
            },
            error => {
                setLoading(false);
                openSnackbar(error);
            }
        );
    }, [openSnackbar]);

    const handleAddNewMessageClick = () => {
        setLoading(true);

        const payload = {
            message: newMessage,
            severity: newSeverity,
            startDateTime: dayjs(newStartDateTimeLocal).utc().format(),
            endDateTime: dayjs(newEndDateTimeLocal).utc().format(),
        };

        apiFetch('api/systemmessages', {
            method: 'POST',
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            data => {
                setSystemMessages(prev => [data, ...prev]);
                openSnackbar('System message successfully saved.');
                setLoading(false);
                // reset inputs
                setNewMessage('');
                setNewSeverity(Severity.Info);
                setNewStartDateTimeLocal('');
                setNewEndDateTimeLocal('');
            },
            error => {
                setLoading(false);
                openSnackbar(error);
            }
        );
    };

    const handleDelete = messageId => {
        apiFetch(`api/systemmessages/${messageId}`, {
            method: 'DELETE',
        }).then(
            () => {
                setSystemMessages(prev => prev.filter(m => m.id !== messageId));
                openSnackbar('System message successfully deleted.');
                setLoading(false);
            },
            error => {
                openSnackbar(error);
            }
        );
    };

    if (loading) {
        return <Spinner />;
    }

    return (
        <>
            {user.isCarwashAdmin && (
                <Paper className={classes.paper} elevation={1}>
                    <div>
                        <TextField
                            fullWidth
                            required
                            id="newMessage"
                            label="Message"
                            margin="normal"
                            value={newMessage}
                            onChange={e => setNewMessage(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <FormControl fullWidth sx={{ marginTop: 0 }}>
                            <InputLabel id="newSeverityLabel">Severity</InputLabel>
                            <Select
                                labelId="newSeverityLabel"
                                id="newSeverity"
                                value={newSeverity}
                                label="Severity"
                                onChange={e => setNewSeverity(e.target.value)}
                            >
                                <MenuItem value={Severity.Error}>Error</MenuItem>
                                <MenuItem value={Severity.Warning}>Warning</MenuItem>
                                <MenuItem value={Severity.Info}>Info</MenuItem>
                                <MenuItem value={Severity.Success}>Success</MenuItem>
                            </Select>
                        </FormControl>
                    </div>
                    <div className={classes.formControl}>
                        <TextField
                            required
                            fullWidth
                            id="newStartDateTimeLocal"
                            label="Start DateTime"
                            type="datetime-local"
                            InputLabelProps={{
                                shrink: true,
                            }}
                            value={newStartDateTimeLocal}
                            onChange={e => setNewStartDateTimeLocal(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <TextField
                            required
                            fullWidth
                            id="newEndDateTimeLocal"
                            label="End DateTime"
                            type="datetime-local"
                            InputLabelProps={{
                                shrink: true,
                            }}
                            value={newEndDateTimeLocal}
                            onChange={e => setNewEndDateTimeLocal(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <Button
                            variant="contained"
                            color="primary"
                            className={classes.primaryButtonContained}
                            onClick={handleAddNewMessageClick}
                        >
                            Save
                        </Button>
                    </div>
                </Paper>
            )}
            {systemMessages.length > 0 ? (
                <List className={classes.list}>
                    {systemMessages.map(message => (
                        <ListItem key={message.id}>
                            <ListItemAvatar>
                                <Avatar>
                                    <Alert
                                        severity={getSeverityName(message.severity)}
                                        sx={{ '& .MuiAlert-icon': { marginRight: 0 } }}
                                    />
                                </Avatar>
                            </ListItemAvatar>
                            <ListItemText
                                primary={message.message}
                                secondary={format2Dates(message.startDateTime, message.endDateTime)}
                            />
                            {user.isCarwashAdmin && (
                                <ListItemSecondaryAction>
                                    <IconButton
                                        aria-label="Delete"
                                        onClick={() => handleDelete(message.id)}
                                        size="large"
                                    >
                                        <DeleteIcon />
                                    </IconButton>
                                </ListItemSecondaryAction>
                            )}
                        </ListItem>
                    ))}
                </List>
            ) : (
                <Alert severity="info" sx={{ maxWidth: 600 }}>
                    No system messages.
                </Alert>
            )}
        </>
    );
}

SystemMessagesAdmin.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    user: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(SystemMessagesAdmin);
