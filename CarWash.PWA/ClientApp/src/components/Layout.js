import React from 'react';
import PropTypes from 'prop-types';
import { Link } from 'react-router-dom';
import { withStyles } from '@mui/styles';
import Drawer from '@mui/material/Drawer';
import AppBar from '@mui/material/AppBar';
import Toolbar from '@mui/material/Toolbar';
import List from '@mui/material/List';
import Typography from '@mui/material/Typography';
import IconButton from '@mui/material/IconButton';
import Hidden from '@mui/material/Hidden';
import Divider from '@mui/material/Divider';
import MenuIcon from '@mui/icons-material/Menu';
import RefreshIcon from '@mui/icons-material/Refresh';
import FavoriteIcon from '@mui/icons-material/Favorite';
import { drawerItems, otherDrawerItems } from './DrawerItems';

const drawerWidth = 240;

const styles = theme => ({
    root: {
        flexGrow: 1,
        height: '100vh',
        zIndex: 1,
        overflow: 'hidden',
        position: 'relative',
        display: 'flex',
        width: '100%',
    },
    flex: {
        flexGrow: 1,
    },
    appBar: {
        position: 'absolute',
        marginLeft: drawerWidth,
        [theme.breakpoints.up('md')]: {
            width: `calc(100% - ${drawerWidth}px)`,
        },
        ...(theme.palette.mode === 'dark' && {
            backgroundColor: theme.palette.background.paper,
        }),
        backgroundImage: 'none',
    },
    navIconHide: {
        [theme.breakpoints.up('md')]: {
            display: 'none',
        },
    },
    toolbar: theme.mixins.toolbar,
    drawerPaper: {
        width: drawerWidth,
        backgroundImage: 'none',
        [theme.breakpoints.up('md')]: {
            position: 'relative',
        },
    },
    drawer: {
        height: '100vh',
        position: 'relative',
    },
    content: {
        flexGrow: 1,
        overflow: 'auto',
        backgroundColor: theme.palette.background.default,
        padding: theme.spacing(3),
        marginTop: 56,
        [`${theme.breakpoints.up('xs')} and (orientation: landscape)`]: {
            marginTop: 48,
        },
        [theme.breakpoints.up('sm')]: {
            marginTop: 64,
        },
    },
    appTitle: {
        height: 60,
        padding: '16px 24px',
    },
    menuList: {
        backgroundColor: theme.palette.background.paper,
        zIndex: 1,
    },
    footer: {
        position: 'absolute',
        bottom: 0,
        padding: 24,
        color: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.54)' : 'rgba(0, 0, 0, 0.54)',
        fontSize: '0.8125rem',
        fontWeight: 400,
        zIndex: 0,
    },
    link: {
        textDecoration: 'underline',
        color: theme.palette.mode === 'dark' ? 'rgba(255, 255, 255, 0.54)' : 'rgba(0, 0, 0, 0.54)',
        fontSize: '0.8125rem',
        fontWeight: 400,
    },
    madeWithLove: {
        marginTop: 12,
        display: 'block',
    },
    loveIcon: {
        fontSize: '0.8125rem',
    },
});

class Layout extends React.Component {
    displayName = 'Layout';

    state = {
        mobileOpen: false,
    };

    handleDrawerToggle = () => {
        this.setState(state => ({ mobileOpen: !state.mobileOpen }));
    };

    handleDrawerClose = () => {
        this.setState({ mobileOpen: false });
    };

    getNavbarName() {
        if (window.location.pathname === '/') return this.props.children.props.children[0].props.navbarName;

        let name;
        this.props.children.props.children.map(child => {
            if (child.props.path === '/') return null;

            try {
                if (window.location.pathname.includes(child.props.path)) {
                    name = child.props.navbarName;
                }
            } catch (e) {
                if (window.location.pathname.indexOf(child.props.path) !== -1) {
                    name = child.props.navbarName;
                }
            }

            return null;
        });

        return name;
    }

    getRefreshFunc() {
        if (window.location.pathname === '/') return this.props.children.props.children[0].props.refresh;

        let func;
        this.props.children.props.children.map(child => {
            if (child.props.path === '/') return null;

            try {
                if (window.location.pathname.includes(child.props.path)) {
                    func = child.props.refresh;
                }
            } catch (e) {
                if (window.location.pathname.indexOf(child.props.path) !== -1) {
                    func = child.props.refresh;
                }
            }

            return null;
        });

        return func;
    }

    render() {
        const { classes, theme, configuration, user } = this.props;
        const refresh = this.getRefreshFunc();

        const drawer = (
            <div className={classes.drawer}>
                <div className={classes.toolbar}>
                    <Link to="/" onClick={this.handleDrawerClose}>
                        <img src={'/images/carwash.svg'} alt="CarWash" height="20px" className={classes.appTitle} />
                    </Link>
                </div>
                <Divider />
                <List className={classes.menuList}>{drawerItems(this.handleDrawerClose, user)}</List>
                <Divider />
                <List className={classes.menuList}>{otherDrawerItems(this.handleDrawerClose)}</List>
                <div className={classes.footer}> 
                    <a href="https://go.microsoft.com/fwlink/?LinkID=206977" className={classes.link}>
                        Terms of use
                    </a>
                    <br />
                    <a href="https://go.microsoft.com/fwlink/?LinkId=521839" className={classes.link}>
                        Privacy & cookies policy
                    </a>
                    <br />
                    <span className={classes.madeWithLove}>
                        Version: {process.env.REACT_APP_BUILD_NUMBER} {process.env.REACT_APP_BUILD_NUMBER !== configuration.buildNumber && (<>- Update available!</>)}
                    </span>
                    <span className={classes.madeWithLove}>
                        Made with <FavoriteIcon className={classes.loveIcon} /> by friends at Microsoft
                    </span>
                </div>
            </div>
        );

        return (
            <div className={classes.root}>
                <AppBar className={classes.appBar}>
                    <Toolbar>
                        <IconButton
                            aria-label="open drawer"
                            onClick={this.handleDrawerToggle}
                            className={classes.navIconHide}
                            size="large"
                        >
                            <MenuIcon />
                        </IconButton>
                        <Typography variant="h6" noWrap className={classes.flex}>
                            {this.getNavbarName()}
                        </Typography>
                        {refresh && (
                            <IconButton aria-label="refresh" onClick={refresh} size="large">
                                <RefreshIcon />
                            </IconButton>
                        )}
                    </Toolbar>
                </AppBar>
                <Hidden mdUp>
                    <Drawer
                        variant="temporary"
                        anchor={theme.direction === 'rtl' ? 'right' : 'left'}
                        open={this.state.mobileOpen}
                        onClose={this.handleDrawerToggle}
                        classes={{
                            paper: classes.drawerPaper,
                        }}
                        ModalProps={{
                            keepMounted: true, // Better open performance on mobile.
                        }}
                    >
                        {drawer}
                    </Drawer>
                </Hidden>
                <Hidden mdDown implementation="css">
                    <Drawer
                        variant="permanent"
                        open
                        classes={{
                            paper: classes.drawerPaper,
                        }}
                    >
                        {drawer}
                    </Drawer>
                </Hidden>
                <div className={classes.toolbar} />
                <main className={classes.content}>{this.props.children}</main>
            </div>
        );
    }
}

Layout.propTypes = {
    classes: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    theme: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    location: PropTypes.object, // eslint-disable-line react/forbid-prop-types
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
    user: PropTypes.object, // eslint-disable-line react/forbid-prop-types
};

export default withStyles(styles, { withTheme: true })(Layout);
