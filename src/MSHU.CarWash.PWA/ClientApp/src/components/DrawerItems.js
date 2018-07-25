import React from 'react';
import { Link } from 'react-router-dom'
import ListItem from '@material-ui/core/ListItem';
import ListItemIcon from '@material-ui/core/ListItemIcon';
import ListItemText from '@material-ui/core/ListItemText';
import Button from '@material-ui/core/Button';
import AddIcon from '@material-ui/icons/Add';
import ListIcon from '@material-ui/icons/List';
import DirectionsCarIcon from '@material-ui/icons/DirectionsCar';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import BuildIcon from '@material-ui/icons/Build';
import SettingsIcon from '@material-ui/icons/Settings';
import HelpIcon from '@material-ui/icons/Help';
import ExitToAppIcon from '@material-ui/icons/ExitToApp';

export const drawerItems = (
    <div>
        <Button component={Link} to="/reserve" variant="extendedFab" color="primary" aria-label="Reserve" style={{ margin: '8px 16px 16px 24px' }}>
            <AddIcon style={{ marginRight: '8px' }}/>
            Reserve
        </Button>
        <ListItem button component={Link} to="/">
            <ListItemIcon>
                <ListIcon />
            </ListItemIcon>
            <ListItemText primary="My reservations" />
        </ListItem>
        <ListItem button component={Link} to="/cars">
            <ListItemIcon>
                <DirectionsCarIcon />
            </ListItemIcon>
            <ListItemText primary="My cars" />
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
        <ListItem button component={Link} to="/signout">
            <ListItemIcon>
                <ExitToAppIcon />
            </ListItemIcon>
            <ListItemText primary="Sign out" />
        </ListItem>
        <ListItem button component={Link} to="/counter">
            <ListItemIcon>
                <DirectionsCarIcon />
            </ListItemIcon>
            <ListItemText primary="Counter" />
        </ListItem>
        <ListItem button component={Link} to="/fetchdata">
            <ListItemIcon>
                <LocalCarWashIcon />
            </ListItemIcon>
            <ListItemText primary="Fetch Data" />
        </ListItem>
    </div>
);
