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
import IconButton from '@material-ui/core/IconButton';
import DeleteIcon from '@material-ui/icons/Delete';
import Spinner from './Spinner';
import TrackedComponent from './TrackedComponent';
import { format2Dates } from '../Helpers';

const styles = theme => ({});

class Blockers extends TrackedComponent {
    displayName = Blockers.name;

    state = {
        loading: true,
        blockers: [],
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

    render() {
        const { classes } = this.props;
        const { loading, blockers } = this.state;

        if (loading) {
            return <Spinner />;
        }

        return (
            <List>
                {blockers.map(blocker => (
                    <ListItem key={blocker.id}>
                        <ListItemAvatar>
                            <Avatar>{moment(blocker.startDate).format('D')}</Avatar>
                        </ListItemAvatar>
                        <ListItemText primary={format2Dates(blocker.startDate, blocker.endDate)} secondary={blocker.comment} />
                        <ListItemSecondaryAction>
                            <IconButton aria-label="Delete">
                                <DeleteIcon />
                            </IconButton>
                        </ListItemSecondaryAction>
                    </ListItem>
                ))}
            </List>
        );
    }
}

Blockers.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    snackbarOpen: PropTypes.bool.isRequired,
    openSnackbar: PropTypes.func.isRequired,
};

export default withStyles(styles)(Blockers);
