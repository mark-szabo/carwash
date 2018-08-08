import React from 'react';
import { Link } from 'react-router-dom'
import ListItem from '@material-ui/core/ListItem';
import ListItemIcon from '@material-ui/core/ListItemIcon';
import ListItemText from '@material-ui/core/ListItemText';
import Button from '@material-ui/core/Button';
import AddIcon from '@material-ui/icons/Add';
import ListIcon from '@material-ui/icons/List';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import BuildIcon from '@material-ui/icons/Build';
import SettingsIcon from '@material-ui/icons/Settings';
import HelpIcon from '@material-ui/icons/Help';
import ExitToAppIcon from '@material-ui/icons/ExitToApp';
import { signOut } from '../Auth';

export const drawerItems = (
    <div>
        <Button component={Link} to="/reserve" variant="extendedFab" color="primary" aria-label="Reserve" style={{ margin: '8px 16px 16px 24px', padding: '0 24px 0 16px' }}>
            <AddIcon style={{ marginRight: '16px' }}/>
            Reserve
        </Button>
        <ListItem button component={Link} to="/">
            <ListItemIcon>
                <ListIcon />
            </ListItemIcon>
            <ListItemText primary="My reservations" />
        </ListItem>
        <ListItem button component={Link} to="/carwashadmin">
            <ListItemIcon>
                <LocalCarWashIcon />
            </ListItemIcon>
            <ListItemText primary="CarWash admin" />
        </ListItem>
        <ListItem button component={Link} to="/admin">
            <ListItemIcon>
                <BuildIcon />
            </ListItemIcon>
            <ListItemText primary="Admin" />
        </ListItem>
    </div>
);

export const otherDrawerItems = (
    <div>
        <ListItem button component={Link} to="/settings">
            <ListItemIcon>
                <SettingsIcon />
            </ListItemIcon>
            <ListItemText primary="Settings" />
        </ListItem>
        <ListItem button component={Link} to="/support">
            <ListItemIcon>
                <HelpIcon />
            </ListItemIcon>
            <ListItemText primary="Contact support" />
        </ListItem>
        <ListItem button onClick={signOut}>
            <ListItemIcon>
                <ExitToAppIcon />
            </ListItemIcon>
            <ListItemText primary="Sign out" />
        </ListItem>
    </div>
);
