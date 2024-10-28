import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';
import LockIcon from '@mui/icons-material/Lock';
import Tooltip from '@mui/material/Tooltip';

export const styles = theme => ({
    /* Styles applied to the root element. */
    root: {
        display: 'flex',
        alignItems: 'center',
        padding: 16,
    },
    /* Styles applied to the avatar element. */
    avatar: {
        flex: '0 0 auto',
        marginRight: 16,
    },
    /* Styles applied to the logo element. */
    logo: {
        flex: '0 0 auto',
        alignSelf: 'flex-start',
        marginTop: 8,
    },
    /* Styles applied to the content wrapper element. */
    content: {
        flex: '1 1 auto',
    },
    /* Styles applied to the title Typography element. */
    title: {
        flex: '1 1 auto',
    },
    titleRow: {
        display: 'flex',
    },
    /* Styles applied to the subheader Typography element. */
    subheader: {},
});

function CarwashCardHeader(props) {
    const { company, classes, subheader, subheaderSecondLine, title, private: priv, ...other } = props;

    return (
        <div className={classes.root} {...other}>
            <div className={classes.content}>
                <div className={classes.titleRow}>
                    <Typography variant="h5" component="span" className={classes.title}>
                        {title}{' '}
                        {priv && (
                            <Tooltip disableTouchListener title="Private (car is not company-owned)">
                                <LockIcon alt="Private (car is not company-owned)" />
                            </Tooltip>
                        )}
                    </Typography>
                    <div className={classes.logo}>
                        <img src={`/images/${company}.svg`} alt={company} height="20px" />
                    </div>
                </div>
                <div>
                    <Typography className={classes.subheader} color="textSecondary" component="span">
                        {subheader}
                    </Typography>
                </div>
                <div>
                    <Typography className={classes.subheader} color="textSecondary" component="span">
                        {subheaderSecondLine}
                    </Typography>
                </div>
            </div>
        </div>
    );
}

CarwashCardHeader.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    company: PropTypes.string,
    subheader: PropTypes.string.isRequired,
    subheaderSecondLine: PropTypes.string.isRequired,
    title: PropTypes.string.isRequired,
    private: PropTypes.bool,
};

export default withStyles(styles)(CarwashCardHeader);
