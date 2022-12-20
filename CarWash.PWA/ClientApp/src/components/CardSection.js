import React from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';
import ButtonBase from '@mui/material/ButtonBase';
import IconButton from '@mui/material/IconButton';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';

const styles = theme => {
    const transition = {
        duration: theme.transitions.duration.shortest,
    };
    return {
        title: {
            paddingTop: theme.spacing.unit,
            paddingLeft: theme.spacing.unit,
            paddingRight: theme.spacing.unit,
            paddingBottom: '0',
            width: '100%',
            display: 'flex',
            minHeight: 8 * 6,
            transition: theme.transitions.create(['min-height', 'background-color'], transition),
            padding: '0 24px 0 24px',
            '&:hover:not($disabled)': {
                cursor: 'pointer',
            },
            '&$expanded': {
                minHeight: 64,
            },
            '&$focused': {
                backgroundColor: theme.palette.grey[300],
            },
            '&$disabled': {
                opacity: 0.38,
            },
        },
        titleText: {
            display: 'flex',
            flexGrow: 1,
            transition: theme.transitions.create(['margin'], transition),
            margin: '12px 48px',
            '& > :last-child': {
                paddingRight: 32,
            },
            '&$expanded': {
                margin: '20px 48px',
            },
        },
        expandIcon: {
            position: 'absolute',
            top: '55%',
            left: 8,
            transform: 'translateY(-50%) rotate(0deg)',
            transition: theme.transitions.create('transform', transition),
            '&:hover': {
                // Disable the hover effect for the IconButton,
                // because a hover effect should apply to the entire Expand button and
                // not only to the IconButton.
                backgroundColor: 'transparent',
            },
            '&$expanded': {
                transform: 'translateY(-50%) rotate(180deg)',
            },
        },
        /* Styles applied to the root element if `expanded={true}`. */
        expanded: {},
        /* Styles applied to the root and children wrapper elements when focused. */
        focused: {},
        /* Styles applied to the root element if `disabled={true}`. */
        disabled: {},
    };
};

class CardSection extends React.Component {
    displayName = 'CardSection';

    state = {
        expanded: this.props.expanded,
        focused: false,
    };

    handleFocus = () => {
        this.setState({
            focused: true,
        });
    };

    handleBlur = () => {
        this.setState({
            focused: false,
        });
    };

    handleToggle = () => {
        this.setState(state => ({ expanded: !state.expanded }));
    };

    render() {
        const { classes, disabled, title } = this.props;
        const { expanded, focused } = this.state;

        return (
            <React.Fragment>
                <ButtonBase
                    disabled={disabled}
                    component="div"
                    aria-expanded={expanded}
                    className={classNames(classes.title, {
                        [classes.disabled]: disabled,
                        [classes.expanded]: expanded,
                        [classes.focused]: focused,
                    })}
                    onFocusVisible={this.handleFocus}
                    onBlur={this.handleBlur}
                    onClick={this.handleToggle}
                >
                    <IconButton
                        disabled={disabled}
                        className={classNames(classes.expandIcon, {
                            [classes.expanded]: expanded,
                        })}
                        component="div"
                        tabIndex={-1}
                        aria-hidden="true"
                        disableRipple
                        size="large">
                        <ExpandMoreIcon />
                    </IconButton>
                    <div className={classNames(classes.titleText, { [classes.expanded]: expanded })}>
                        <Typography variant="subtitle1">{title}</Typography>
                    </div>
                </ButtonBase>

                {expanded && this.props.children}
            </React.Fragment>
        );
    }
}

CardSection.propTypes = {
    children: PropTypes.node,
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    title: PropTypes.string.isRequired,
    expanded: PropTypes.bool,
    disabled: PropTypes.bool,
};

CardSection.defaultProps = {
    expanded: false,
    disabled: false,
};

export default withStyles(styles)(CardSection);
