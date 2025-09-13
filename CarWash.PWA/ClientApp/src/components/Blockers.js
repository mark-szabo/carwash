import { useState, useEffect } from 'react';
import PropTypes from 'prop-types';
import * as moment from 'moment';
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
import Paper from '@mui/material/Paper';
import Alert from '@mui/material/Alert';
import Spinner from './Spinner';
import { format2Dates } from '../Helpers';

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

function Blockers(props) {
    const { classes, openSnackbar, user } = props;

    const [loading, setLoading] = useState(true);
    const [blockers, setBlockers] = useState([]);
    const [newBlockerStartDate, setNewBlockerStartDate] = useState(null);
    const [newBlockerEndDate, setNewBlockerEndDate] = useState(null);
    const [newBlockerComment, setNewBlockerComment] = useState('');

    useEffect(() => {
        apiFetch('api/blockers').then(
            data => {
                setBlockers(data);
                setLoading(false);
            },
            error => {
                setLoading(false);
                openSnackbar(error);
            }
        );
    }, [openSnackbar]);

    const handleAddNewBlockerClick = () => {
        setLoading(true);

        const payload = {
            startDate: moment(newBlockerStartDate).utc().format(),
            endDate: moment(newBlockerEndDate).utc().format(),
            comment: newBlockerComment,
        };

        apiFetch('api/blockers', {
            method: 'POST',
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            data => {
                setBlockers(prev => {
                    const copy = [...prev];
                    copy.unshift(data);
                    return copy;
                });

                openSnackbar('Blocker successfully saved.');
                setLoading(false);
                // reset inputs (optional)
                setNewBlockerComment('');
                setNewBlockerStartDate(null);
                setNewBlockerEndDate(null);
            },
            error => {
                setLoading(false);
                openSnackbar(error);
            }
        );
    };

    const handleDelete = blockerId => {
        apiFetch(`api/blockers/${blockerId}`, {
            method: 'DELETE',
        }).then(
            () => {
                setBlockers(prev => prev.filter(b => b.id !== blockerId));
                openSnackbar('Blocker successfully deleted.');
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
                            id="newBlockerComment"
                            label="Comment (for CarWash)"
                            margin="normal"
                            value={newBlockerComment}
                            onChange={e => setNewBlockerComment(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <TextField
                            fullWidth
                            required
                            id="newBlockerStartDate"
                            label="Start date"
                            type="datetime-local"
                            InputLabelProps={{
                                shrink: true,
                            }}
                            value={newBlockerStartDate || ''}
                            onChange={e => setNewBlockerStartDate(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <TextField
                            fullWidth
                            id="newBlockerEndDate"
                            label="End date"
                            type="datetime-local"
                            InputLabelProps={{
                                shrink: true,
                            }}
                            value={newBlockerEndDate || ''}
                            onChange={e => setNewBlockerEndDate(e.target.value)}
                            className={classes.inputField}
                        />
                    </div>
                    <div className={classes.formControl}>
                        <Button
                            variant="contained"
                            color="primary"
                            className={classes.primaryButtonContained}
                            onClick={handleAddNewBlockerClick}
                        >
                            Save
                        </Button>
                    </div>
                </Paper>
            )}
            {blockers.length > 0 ? (
                <List className={classes.list}>
                    {blockers.map(blocker => (
                        <BlockerListItem
                            key={blocker.id}
                            blocker={blocker}
                            user={user}
                            handleDelete={handleDelete}
                            openSnackbar={openSnackbar}
                        />
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

Blockers.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    user: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(Blockers);

function BlockerListItem(props) {
    const { blocker, user, handleDelete } = props;

    return (
        <ListItem>
            <ListItemAvatar>
                <Avatar>{moment(blocker.startDate).format('D')}</Avatar>
            </ListItemAvatar>
            <ListItemText primary={format2Dates(blocker.startDate, blocker.endDate)} secondary={blocker.comment} />
            {user.isCarwashAdmin && (
                <ListItemSecondaryAction>
                    <IconButton aria-label="Delete" onClick={() => handleDelete(blocker.id)} size="large">
                        <DeleteIcon />
                    </IconButton>
                </ListItemSecondaryAction>
            )}
        </ListItem>
    );
}
