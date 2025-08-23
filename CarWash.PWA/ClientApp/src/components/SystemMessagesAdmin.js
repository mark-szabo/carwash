import React from 'react';
import PropTypes from 'prop-types';
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
import { Alert } from '@mui/material';
import { Severity } from '../Constants';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import Select from '@mui/material/Select';
import { getSeverityName } from '../Helpers';

const styles = theme => ({
    list: {
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 600,
        },
        backgroundColor: theme.palette.background.paper,
        marginTop: 48,
    },
    primaryButtonContained: {
        marginTop: theme.spacing(1),
    },
    formControl: {
        marginTop: theme.spacing(2),
        marginBottom: theme.spacing(2),
    },
    inputField: {
        minWidth: 220,
    },
});

class SystemMessagesAdmin extends React.Component {
    state = {
        loading: true,
        systemMessages: [],
        newMessage: '',
        newSeverity: Severity.Info,
        newStartDateTime: null,
        newEndDateTime: null,
    };

    componentDidMount() {
        apiFetch('api/systemmessages').then(
            data => {
                this.setState({
                    systemMessages: data,
                    loading: false,
                });
            },
            error => {
                this.setState({ loading: false });
                this.props.openSnackbar(error);
            }
        );
    }

    handleChange = name => event => {
        this.setState({
            [name]: event.target.value,
        });
    };

    handleChangeSeverity = event => {
        this.setState({
            newSeverity: event.target.value,
        });
    };

    handleAddNewMessageClick = () => {
        this.setState({ loading: true });

        const payload = {
            message: this.state.newMessage,
            severity: this.state.newSeverity,
            startDateTime: this.state.newStartDateTime,
            endDateTime: this.state.newEndDateTime,
        };

        apiFetch('api/systemmessages', {
            method: 'POST',
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            data => {
                this.setState(state => {
                    const systemMessages = [...state.systemMessages];
                    systemMessages.unshift(data);

                    return { systemMessages };
                });

                this.props.openSnackbar('System message successfully saved.');
                this.setState({ loading: false });
            },
            error => {
                this.setState({ loading: false });
                this.props.openSnackbar(error);
            }
        );
    };

    handleDelete = messageId => {
        apiFetch(`api/systemmessages/${messageId}`, {
            method: 'DELETE',
        }).then(
            () => {
                this.setState(state => {
                    let systemMessages = [...state.systemMessages];
                    systemMessages = systemMessages.filter(m => m.id !== messageId);

                    return { systemMessages };
                });

                this.props.openSnackbar('System message successfully deleted.');
                this.setState({ loading: false });
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    render() {
        const { classes, user } = this.props;
        const { loading, newSeverity, systemMessages } = this.state;

        if (loading) {
            return <Spinner />;
        }

        return (
            <React.Fragment>
                {user.isCarwashAdmin && (
                    <div>
                        <div className={classes.formControl}>
                            <TextField
                                required
                                id="newMessage"
                                label="Message"
                                margin="normal"
                                onChange={this.handleChange('newMessage')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <FormControl>
                                <InputLabel id="newSeverityLabel">Severity</InputLabel>
                                <Select
                                    labelId="newSeverityLabel"
                                    id="newSeverity"
                                    value={newSeverity}
                                    label="Severity"
                                    onChange={this.handleChangeSeverity}
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
                                id="newStartDateTime"
                                label="Start DateTime"
                                type="datetime-local"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleChange('newStartDateTime')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <TextField
                                id="newEndDateTime"
                                label="End DateTime"
                                type="datetime-local"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleChange('newEndDateTime')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <Button
                                variant="contained"
                                color="primary"
                                className={classes.primaryButtonContained}
                                onClick={this.handleAddNewMessageClick}
                            >
                                Save
                            </Button>
                        </div>
                    </div>
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
                                    secondary={`${message.startDateTime} - ${message.endDateTime}`}
                                />
                                {user.isCarwashAdmin && (
                                    <ListItemSecondaryAction>
                                        <IconButton
                                            aria-label="Delete"
                                            onClick={() => this.handleDelete(message.id)}
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
                    <Alert severity="info">No system messages.</Alert>
                )}
            </React.Fragment>
        );
    }
}

SystemMessagesAdmin.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    user: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(SystemMessagesAdmin);
