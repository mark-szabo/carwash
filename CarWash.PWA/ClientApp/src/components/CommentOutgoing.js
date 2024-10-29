import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';

const styles = theme => ({
    comment: {
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
    after: {
        clear: 'both',
        display: 'block',
        fontSize: 0,
        height: 0,
        lineHeight: 0,
        visibility: 'hidden',
    },
});

class CommentOutgoing extends Component {
    displayName = 'CommentOutgoing';

    render() {
        const { classes, comment } = this.props;

        if (!comment) return null;
        return (
            <div>
                <Typography component="p" className={classes.comment}>
                    {comment}
                </Typography>
                <div className={classes.after}>.</div>
            </div>
        );
    }
}

CommentOutgoing.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    comment: PropTypes.string,
};

export default withStyles(styles)(CommentOutgoing);
