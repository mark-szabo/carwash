import React, { useState } from 'react';
import PropTypes from 'prop-types';
import Divider from '@mui/material/Divider';
import apiFetch from '../Auth';
import { BacklogHubMethods } from '../Constants';
import OutlinedInput from '@mui/material/OutlinedInput';
import InputLabel from '@mui/material/InputLabel';
import InputAdornment from '@mui/material/InputAdornment';
import FormControl from '@mui/material/FormControl';
import IconButton from '@mui/material/IconButton';
import SendIcon from '@mui/icons-material/Send';
import ChatMessage from './ChatMessage';

export default function Chat(props) {
    const { reservation, carWashChat, hideInput } = props;
    const [commentTextfield, setCommentTextfield] = useState('');
    const userRole = carWashChat ? 'carwash' : 'user';

    const handleAddComment = async () => {
        // Saving the old comments state in case of an error
        const oldComments = reservation.comments;

        // Update the frontend reservation state with the new comment
        reservation.comments.push({
            message: commentTextfield,
            role: userRole,
            // timestamp: new Date().toISOString(),
            timestamp: new Date().toString(),
        });
        props.updateReservation(reservation);

        const messageToBeSent = commentTextfield;
        setCommentTextfield('');

        try {
            await apiFetch(
                `api/reservations/${props.reservation.id}/comment`,
                {
                    method: 'POST',
                    body: JSON.stringify(messageToBeSent),
                    headers: {
                        'Content-Type': 'application/json',
                    },
                },
                true
            );
            props.openSnackbar('Comment saved.');

            // Broadcast using SignalR
            props.invokeBacklogHub(BacklogHubMethods.ReservationUpdated, props.reservation.id);
        } catch (error) {
            reservation.comments = oldComments;
            props.updateReservation(reservation);

            setCommentTextfield(messageToBeSent);

            props.openSnackbar(error);
        }
    };

    const handleCommentKeyPress = event => {
        if (event.key === 'Enter') handleAddComment();
    };

    return (
        <div>
            {!(hideInput && reservation.comments.length === 0) && <Divider sx={{ margin: '24px 0 12px 0' }} />}
            {reservation.comments.map((comment, index) => (
                <ChatMessage
                    key={index}
                    message={comment.message}
                    isSent={comment.role === userRole}
                    name={comment.role === 'user' ? (reservation.user?.firstName ?? 'You') : 'CarWash'}
                />
            ))}
            {reservation.comments.length === 0 && userRole === 'user' && (
                <ChatMessage
                    message="Feel free to leave a message or ask a question below – even after we've started washing your car."
                    name="CarWash"
                />
            )}
            {!hideInput && (
                <FormControl variant="outlined" sx={{ width: '100%', marginTop: '24px' }}>
                    <InputLabel htmlFor="comment">Message</InputLabel>
                    <OutlinedInput
                        id="comment"
                        type="text"
                        label="Mesaage"
                        value={commentTextfield}
                        onChange={event => setCommentTextfield(event.target.value)}
                        onKeyUp={handleCommentKeyPress}
                        endAdornment={
                            <InputAdornment position="end">
                                {commentTextfield && (
                                    <IconButton
                                        aria-label="Save comment"
                                        onClick={handleAddComment}
                                        onMouseDown={event => {
                                            event.preventDefault();
                                        }}
                                        size="large"
                                    >
                                        <SendIcon />
                                    </IconButton>
                                )}
                            </InputAdornment>
                        }
                        sx={{ borderRadius: '28px' }}
                    />
                </FormControl>
            )}
        </div>
    );
}

Chat.propTypes = {
    reservation: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    carWashChat: PropTypes.bool,
    hideInput: PropTypes.bool,
    updateReservation: PropTypes.func.isRequired,
    openSnackbar: PropTypes.func.isRequired,
    invokeBacklogHub: PropTypes.func.isRequired,
};
