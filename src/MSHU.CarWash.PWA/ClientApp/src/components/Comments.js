import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Divider from '@material-ui/core/Divider';
import CommentOutgoing from './CommentOutgoing';
import CommentIncoming from './CommentIncoming';

const styles = {
    divider: {
        margin: '24px 0',
    },
};

class Comments extends Component {
    displayName = 'Comments';

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
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    commentOutgoing: PropTypes.string,
    commentIncoming: PropTypes.string,
    commentIncomingName: PropTypes.string,
    incomingFirst: PropTypes.bool,
};

export default withStyles(styles)(Comments);
