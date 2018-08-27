import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Typography from '@material-ui/core/Typography';
import LockIcon from '@material-ui/icons/Lock';

export const styles = theme => ({
    /* Styles applied to the root element. */
    root: theme.mixins.gutters({
        display: 'flex',
        alignItems: 'center',
        paddingTop: 16,
        paddingBottom: 16,
    }),
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
    const { company, classes, subheader, title, private: priv, ...other } = props;

    return (
        <div className={classes.root} {...other}>
            <div className={classes.content}>
                <div className={classes.titleRow}>
                    <Typography variant="headline" component="span" className={classes.title}>
                        {title} {priv && <LockIcon alt="Private" />}
                    </Typography>
                    <div className={classes.logo}>
                        <img src={`/images/${company}.svg`} alt={company} height="20px" />
                    </div>
                </div>
                <Typography variant="body1" className={classes.subheader} color="textSecondary" component="span">
                    {subheader}
                </Typography>
            </div>
        </div>
    );
}

CarwashCardHeader.propTypes = {
    company: PropTypes.string,
    classes: PropTypes.object.isRequired,
    subheader: PropTypes.string.isRequired,
    title: PropTypes.string.isRequired,
    private: PropTypes.bool,
};

export default withStyles(styles)(CarwashCardHeader);
