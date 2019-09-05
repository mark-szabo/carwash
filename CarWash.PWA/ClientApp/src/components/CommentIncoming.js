import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Typography from '@material-ui/core/Typography';

const styles = theme => ({
    comment: {
        borderTopRightRadius: '1.3em',
        borderTopLeftRadius: '1.3em',
        borderBottomRightRadius: '1.3em',
        backgroundColor: theme.palette.type === 'dark' ? '#616161' : '#e0e0e0',
        padding: '6px 12px',
        margin: '1px 0',
        clear: 'left',
        float: 'left',
        maxWidth: '85%',
        whiteSpace: 'pre-wrap',
    },
    commentName: {
        color: theme.palette.type === 'dark' ? 'rgba(255, 255, 255, .40)' : 'rgba(0, 0, 0, .40)',
        fontSize: '12px',
        fontWeight: 'normal',
        lineHeight: '1.1',
        marginBottom: '1px',
        overflow: 'hidden',
        paddingLeft: '12px',
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

class CommentIncoming extends Component {
    displayName = 'CommentIncoming';

    render() {
        const { classes, comment, name } = this.props;

        if (!comment) return null;
        return (
            <div>
                <Typography component="h5" className={classes.commentName}>
                    {name}
                </Typography>
                <Typography component="p" className={classes.comment}>
                    {comment}
                </Typography>
                <div className={classes.after}>.</div>
            </div>
        );
    }
}

CommentIncoming.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    comment: PropTypes.string,
    name: PropTypes.string,
};

export default withStyles(styles)(CommentIncoming);
