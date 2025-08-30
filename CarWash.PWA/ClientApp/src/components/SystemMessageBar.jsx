import { useState } from 'react';
import PropTypes from 'prop-types';
import { Alert, AlertTitle, Box } from '@mui/material';
import { makeStyles } from '@mui/styles';
import { getSeverityName } from '../Helpers';

const useStyles = makeStyles(theme => ({
    root: {
        position: 'absolute',
        top: 64, // Adjust based on AppBar height
        right: 0,
        padding: theme.spacing(1),
        zIndex: theme.zIndex.appBar - 1,
        [theme.breakpoints.down('md')]: {
            width: '100%',
        },
        [theme.breakpoints.up('md')]: {
            width: 500,
        },
    },
    alert: {
        marginBottom: theme.spacing(1),
    },
}));

const SystemMessageBar = ({ messages }) => {
    const classes = useStyles();
    const [dismissedMessages, setDismissedMessages] = useState([]);

    const handleDismiss = id => {
        setDismissedMessages([...dismissedMessages, id]);
    };

    const activeMessages = messages.filter(message => !dismissedMessages.includes(message.id));

    return (
        <Box className={classes.root}>
            {activeMessages.map(message => (
                <Alert
                    key={message.id}
                    severity={getSeverityName(message.severity)}
                    onClose={() => handleDismiss(message.id)}
                    className={classes.alert}
                >
                    <AlertTitle>{message.message}</AlertTitle>
                </Alert>
            ))}
        </Box>
    );
};

SystemMessageBar.propTypes = {
    messages: PropTypes.arrayOf(
        PropTypes.shape({
            id: PropTypes.string.isRequired,
            message: PropTypes.string.isRequired,
            severity: PropTypes.number.isRequired,
        })
    ).isRequired,
};

export default SystemMessageBar;
