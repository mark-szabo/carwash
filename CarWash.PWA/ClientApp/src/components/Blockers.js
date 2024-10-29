import React from 'react';
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
import Spinner from './Spinner';
import TrackedComponent from './TrackedComponent';
import { format2Dates } from '../Helpers';
import { Typography } from '@mui/material';

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

class Blockers extends TrackedComponent {
    displayName = 'Blockers';

    state = {
        loading: true,
        blockers: [],
        newBlockerStartDate: null,
        newBlockerEndDate: null,
        newBlockerComment: '',
    };

    componentDidMount() {
        super.componentDidMount();

        apiFetch('api/blockers').then(
            data => {
                this.setState({
                    blockers: data,
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

    handleAddNewBlockerClick = () => {
        this.setState({ loading: true });

        const payload = {
            startDate: this.state.newBlockerStartDate,
            endDate: this.state.newBlockerEndDate,
            comment: this.state.newBlockerComment,
        };

        apiFetch('api/blockers', {
            method: 'POST',
            body: JSON.stringify(payload),
            headers: {
                'Content-Type': 'application/json',
            },
        }).then(
            data => {
                this.setState(state => {
                    const blockers = [...state.blockers];
                    blockers.unshift(data);

                    return { blockers };
                });

                this.props.openSnackbar('Blocker successfully saved.');
                this.setState({ loading: false });
            },
            error => {
                this.setState({ loading: false });
                this.props.openSnackbar(error);
            }
        );
    };

    handleDelete = blockerId => {
        apiFetch(`api/blockers/${blockerId}`, {
            method: 'DELETE',
        }).then(
            () => {
                this.setState(state => {
                    let blockers = [...state.blockers];
                    blockers = blockers.filter(b => b.id !== blockerId);

                    return { blockers };
                });

                this.props.openSnackbar('Blocker successfully deleted.');
                this.setState({ loading: false });
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    render() {
        const { classes, openSnackbar, user } = this.props;
        const { loading, blockers } = this.state;

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
                                id="newBlockerStartDate"
                                label="Start date"
                                type="datetime-local"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleChange('newBlockerStartDate')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <TextField
                                id="newBlockerEndDate"
                                label="End date"
                                type="datetime-local"
                                InputLabelProps={{
                                    shrink: true,
                                }}
                                onChange={this.handleChange('newBlockerEndDate')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <TextField
                                required
                                id="newBlockerComment"
                                label="Comment (for CarWash)"
                                margin="normal"
                                onChange={this.handleChange('newBlockerComment')}
                                className={classes.inputField}
                            />
                        </div>
                        <div className={classes.formControl}>
                            <Button variant="contained" color="primary" className={classes.primaryButtonContained} onClick={this.handleAddNewBlockerClick}>
                                Save
                            </Button>
                        </div>
                    </div>
                )}
                {blockers.length > 0 ? (
                    <List className={classes.list}>
                        {blockers.map(blocker => (
                            <BlockerListItem key={blocker.id} blocker={blocker} user={user} handleDelete={this.handleDelete} openSnackbar={openSnackbar} />
                        ))}
                    </List>
                ) : (
                    <Typography>No blockers.</Typography>
                )}
            </React.Fragment>
        );
    }
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
