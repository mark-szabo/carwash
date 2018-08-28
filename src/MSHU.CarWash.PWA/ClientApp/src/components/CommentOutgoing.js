import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Typography from '@material-ui/core/Typography';

const styles = theme => ({
    comment: {
        clear: 'right',
        float: 'right',
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomLeftRadius: '1.3em',
        backgroundColor: theme.palette.primary.dark,
        color: '#fff',
        padding: '6px 12px',
        margin: '1px 0',
        maxWidth: '85%',
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
    classes: PropTypes.object.isRequired,
    comment: PropTypes.string,
};

export default withStyles(styles)(CommentOutgoing);
