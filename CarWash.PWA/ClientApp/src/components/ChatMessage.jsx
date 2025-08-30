import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';

const styles = theme => ({
    receivedMessage: {
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomRightRadius: '1.3em',
        backgroundColor: theme.palette.mode === 'dark' ? '#616161' : '#e0e0e0',
        padding: '6px 12px',
        margin: '1px 0',
        clear: 'left',
        float: 'left',
        maxWidth: '85%',
        whiteSpace: 'pre-wrap',
    },
    sentMessage: {
        clear: 'right',
        float: 'right',
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomLeftRadius: '1.3em',
        backgroundColor: '#007ac1',
        color: '#fff',
        padding: '6px 12px',
        margin: '1px 0',
        maxWidth: '85%',
        whiteSpace: 'pre-wrap',
        textAlign: 'right',
    },
    senderName: {
        color: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, .40)' : 'rgba(0, 0, 0, .40)',
        fontSize: '12px',
        fontWeight: 'normal',
        lineHeight: '1.1',
        marginBottom: '1px',
        overflow: 'hidden',
        padding: '12px 12px 0 12px',
        textOverflow: 'ellipsis',
        whiteSpace: 'nowrap',
    },
    after: {
        clear: 'both',
        display: 'block',
        fontSize: 0,
        height: 0,
        lineHeight: 0,
        visibility: 'hidden',
    },
});

const ChatMessage = ({ classes, message, name, isSent }) => {
    if (!message) return null;
    return (
        <div>
            <Typography component="h5" className={classes.senderName} sx={{ textAlign: isSent ? 'right' : 'left' }}>
                {name}
            </Typography>
            <Typography component="p" className={isSent ? classes.sentMessage : classes.receivedMessage}>
                {message}
            </Typography>
            <div className={classes.after}>.</div>
        </div>
    );
};

ChatMessage.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    message: PropTypes.string,
    name: PropTypes.string,
    isSent: PropTypes.bool,
};

export default withStyles(styles)(ChatMessage);
