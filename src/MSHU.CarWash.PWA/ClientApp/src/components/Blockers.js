import React from 'react';
import PropTypes from 'prop-types';
import * as moment from 'moment';
import { withStyles } from '@material-ui/core/styles';
import apiFetch from '../Auth';
import List from '@material-ui/core/List';
import ListItem from '@material-ui/core/ListItem';
import ListItemAvatar from '@material-ui/core/ListItemAvatar';
import ListItemSecondaryAction from '@material-ui/core/ListItemSecondaryAction';
import ListItemText from '@material-ui/core/ListItemText';
import Avatar from '@material-ui/core/Avatar';
import Button from '@material-ui/core/Button';
import TextField from '@material-ui/core/TextField';
import IconButton from '@material-ui/core/IconButton';
import DeleteIcon from '@material-ui/icons/Delete';
import Spinner from './Spinner';
import TrackedComponent from './TrackedComponent';
import { format2Dates } from '../Helpers';

const styles = theme => ({
    list: {
        [theme.breakpoints.down('sm')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 600,
        },
        backgroundColor: theme.palette.background.paper,
    },
    primaryButtonContained: {
        marginTop: theme.spacing.unit,
    },
    formControl: {
        marginTop: theme.spacing.unit * 2,
        marginBottom: theme.spacing.unit * 2,
    },
});

class Blockers extends TrackedComponent {
    displayName = Blockers.name;

    state = {
        loading: true,
        blockers: [],
        newBlockerStartDate: null,
        newBlockerEndDate: null,
        newBlockerComment: '',
    };

    componentDidMount() {
        super.componentDidMount();

        this.loadBlockers();
    }

    loadBlockers = () => {
        this.setState({ loading: true });

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
    };

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
                this.loadBlockers();
                this.props.openSnackbar('Blocker successfully saved.');
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
                this.loadBlockers();
                this.props.openSnackbar('Blocker successfully deleted.');
            },
            error => {
                this.props.openSnackbar(error);
            }
        );
    };

    render() {
        const { classes, openSnackbar } = this.props;
        const { loading, blockers } = this.state;

        if (loading) {
            return <Spinner />;
        }

        return (
            <React.Fragment>
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
                        />
                    </div>
                    <div className={classes.formControl}>
                        <TextField required id="newBlockerComment" label="Comment" margin="normal" onChange={this.handleChange('newBlockerComment')} />
                    </div>
                    <div className={classes.formControl}>
                        <Button variant="contained" color="primary" className={classes.primaryButtonContained} onClick={this.handleAddNewBlockerClick}>
                            Save
                        </Button>
                    </div>
                </div>
                <List className={classes.list}>
                    {blockers.map(blocker => (
                        <BlockerListItem key={blocker.id} blocker={blocker} handleDelete={this.handleDelete} openSnackbar={openSnackbar} />
                    ))}
                </List>
            </React.Fragment>
        );
    }
}

Blockers.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(Blockers);

class BlockerListItem extends React.Component {
    render() {
        const { blocker, handleDelete } = this.props;

        return (
            <ListItem>
                <ListItemAvatar>
                    <Avatar>{moment(blocker.startDate).format('D')}</Avatar>
                </ListItemAvatar>
                <ListItemText primary={format2Dates(blocker.startDate, blocker.endDate)} secondary={blocker.comment} />
                <ListItemSecondaryAction>
                    <IconButton aria-label="Delete" onClick={handleDelete(blocker.id)}>
                        <DeleteIcon />
                    </IconButton>
                </ListItemSecondaryAction>
            </ListItem>
        );
    }
}
