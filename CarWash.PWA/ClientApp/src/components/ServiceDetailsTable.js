import React from 'react';
import PropTypes from 'prop-types';
import classNames from 'classnames';
import { withStyles } from '@mui/styles';
import Typography from '@mui/material/Typography';
import ButtonBase from '@mui/material/ButtonBase';
import IconButton from '@mui/material/IconButton';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Paper from '@mui/material/Paper';

const styles = theme => {
    const transition = {
        duration: theme.transitions.duration.shortest,
    };
    const secodaryColor = theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.54)' : 'rgba(0, 0, 0, 0.54)';

    return {
        title: {
            paddingTop: theme.spacing(1),
            paddingLeft: theme.spacing(1),
            paddingRight: theme.spacing(1),
            paddingBottom: '0',
            width: '100%',
            display: 'flex',
            minHeight: 8 * 6,
            transition: theme.transitions.create(['min-height', 'background-color'], transition),
            padding: '0 24px 0 24px',
            '&:hover': {
                cursor: 'pointer',
            },
            '&$expanded': {
                minHeight: 64,
            },
            '&$focused': {
                backgroundColor: theme.palette.grey[300],
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
        root: {
            width: 'calc(100% - 8px)',
            margin: 4,
        },
        table: {
            minWidth: 200,
        },
        cell: {
            padding: '16px 24px 16px 24px',
            borderBottom: 'none',
            borderTop:
                theme.palette.mode === 'dark' ? '1px solid rgba(81, 81, 81, 1)' : '1px solid rgba(224, 224, 224, 1)',
        },
        titleCell: {
            padding: '16px 24px 8px 24px',
        },
        descriptionCell: {
            padding: '0 8px 16px 24px',
            color: secodaryColor,
        },
        row: {
            height: 'initial',
        },
        contactUs: {
            padding: 24,
            color: secodaryColor,
            lineHeight: 1.6,
        },
        link: {
            color: secodaryColor,
            textDecoration: 'underline',
        },
    };
};

class ServiceDetailsTable extends React.Component {
    displayName = 'ServiceDetailsTable';

    state = {
        expanded: false,
        focused: false,
    };

    handleFocus = () => {
        this.setState({ focused: true });
    };

    handleBlur = () => {
        this.setState({ focused: false });
    };

    handleToggle = () => {
        this.setState(state => ({ expanded: !state.expanded }));
    };

    getTable = configuration => (
        <Table className={this.props.classes.table}>
            <TableHead>
                <TableRow>
                    <TableCell className={this.props.classes.titleCell}>Name</TableCell>
                    <TableCell align="right" className={this.props.classes.titleCell}>
                        Price
                    </TableCell>
                    <TableCell align="right" className={this.props.classes.titleCell}>
                        Price - MPV
                    </TableCell>
                </TableRow>
            </TableHead>
            <TableBody>
                {configuration.services.map(service => (
                    <React.Fragment key={service.type}>
                        <TableRow className={this.props.classes.row}>
                            <TableCell component="th" scope="row" className={this.props.classes.cell}>
                                {service.name}
                            </TableCell>
                            <TableCell align="right" className={this.props.classes.cell}>
                                {service.price !== -1 ? `${service.price} Ft` : 'Ask for offer'}
                            </TableCell>
                            <TableCell align="right" className={this.props.classes.cell}>
                                {service.priceMpv !== -1 ? `${service.priceMpv} Ft` : 'Ask for offer'}
                            </TableCell>
                        </TableRow>
                        {service.description && (
                            <TableRow className={this.props.classes.row}>
                                <TableCell colSpan="3" className={this.props.classes.descriptionCell}>
                                    {service.description}
                                </TableCell>
                            </TableRow>
                        )}
                    </React.Fragment>
                ))}
                <TableRow className={this.props.classes.row}>
                    <TableCell colSpan="3" className={this.props.classes.contactUs}>
                        Prices are for reference and may vary by company according to individual agreements. During the
                        service, our team may add essential additional treatments if they are deemed necessary for your
                        vehicle's proper cleaning and maintenance. The final amount may therefore be higher than the
                        price displayed at booking.
                        <br />
                        <br />
                        Call us (
                        <a href="tel:+36704506612" className={this.props.classes.link}>
                            +36 70 701 5803
                        </a>{' '}
                        or{' '}
                        <a href="tel:+36704506612" className={this.props.classes.link}>
                            +36 70 450 6612
                        </a>
                        ) or email us (
                        <a href="mailto:mimosonk@gmail.com" className={this.props.classes.link}>
                            mimosonk@gmail.com
                        </a>
                        )!
                    </TableCell>
                </TableRow>
            </TableBody>
        </Table>
    );

    render() {
        const { classes, configuration } = this.props;
        const { expanded, focused } = this.state;
        // const width = window.innerWidth > 0 ? window.innerWidth : window.screen.width;
        // const mobile = width <= 960;
        // if (!mobile) return <Paper className={classes.root}>{this.getTable()}</Paper>;

        return (
            <Paper className={classes.root}>
                <ButtonBase
                    component="div"
                    aria-expanded={expanded}
                    className={classNames(classes.title, {
                        [classes.expanded]: expanded,
                        [classes.focused]: focused,
                    })}
                    onFocusVisible={this.handleFocus}
                    onBlur={this.handleBlur}
                    onClick={this.handleToggle}
                >
                    <IconButton
                        className={classNames(classes.expandIcon, {
                            [classes.expanded]: expanded,
                        })}
                        component="div"
                        tabIndex={-1}
                        aria-hidden="true"
                        disableRipple
                        size="large"
                    >
                        <ExpandMoreIcon />
                    </IconButton>
                    <div className={classNames(classes.titleText, { [classes.expanded]: expanded })}>
                        <Typography variant="subtitle1">Service pricing details</Typography>
                    </div>
                </ButtonBase>

                {expanded && this.getTable(configuration)}
            </Paper>
        );
    }
}

ServiceDetailsTable.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
};

export default withStyles(styles)(ServiceDetailsTable);
