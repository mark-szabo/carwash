import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Divider from '@material-ui/core/Divider';
import CommentOutgoing from './CommentOutgoing';
import CommentIncoming from './CommentIncoming';

const styles = theme => ({
    divider: {
        margin: '24px 0',
    },
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

class Comments extends Component {
    render() {
        const { classes, commentOutgoing, commentIncoming, commentIncomingName, incomingFirst } = this.props;

        if (!commentOutgoing && !commentIncoming) return null;
        if (incomingFirst) {
            return (
                <div>
                    <Divider className={classes.divider} />
                    <CommentIncoming comment={commentIncoming} name={commentIncomingName} />
                    <CommentOutgoing comment={commentOutgoing} />
                </div>
            );
        }
        return (
            <div>
                <Divider className={classes.divider} />
                <CommentOutgoing comment={commentOutgoing} />
                <CommentIncoming comment={commentIncoming} name={commentIncomingName} />
            </div>
        );
    }
}

Comments.propTypes = {
    classes: PropTypes.object.isRequired,
    commentOutgoing: PropTypes.string,
    commentIncoming: PropTypes.string,
    commentIncomingName: PropTypes.string,
    incomingFirst: PropTypes.bool,
};

export default withStyles(styles)(Comments);
