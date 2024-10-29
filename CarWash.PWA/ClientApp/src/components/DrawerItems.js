import React from 'react';
import { Link } from 'react-router-dom';
import ListItem from '@mui/material/ListItem';
import ListItemIcon from '@mui/material/ListItemIcon';
import ListItemText from '@mui/material/ListItemText';
import Fab from '@mui/material/Fab';
import AddIcon from '@mui/icons-material/Add';
import ListIcon from '@mui/icons-material/List';
import LocalCarWashIcon from '@mui/icons-material/LocalCarWash';
import BuildIcon from '@mui/icons-material/Build';
import SettingsIcon from '@mui/icons-material/SettingsRounded';
import HelpIcon from '@mui/icons-material/Help';
import ExitToAppIcon from '@mui/icons-material/ExitToApp';
import BlockIcon from '@mui/icons-material/Block';
import BarChartIcon from '@mui/icons-material/BarChart';
import { signOut } from '../Auth';

function DrawerItem(props) {
    return (
        <ListItem button component={Link} to={props.path} onClick={props.closeDrawer} id={props.id}>
            <ListItemIcon>{props.icon}</ListItemIcon>
            <ListItemText primary={props.title} />
        </ListItem>
    );
}

export function drawerItems(closeDrawer, user) {
    return (
        <div>
            <Fab
                component={Link}
                to="/reserve"
                onClick={closeDrawer}
                variant="extended"
                color="primary"
                aria-label="Reserve"
                style={{ margin: '8px 16px 16px 24px', padding: '0 24px 0 16px' }}
                id="draweritems-reserve-fab"
            >
                <AddIcon style={{ marginRight: '16px' }} />
                Reserve
            </Fab>
            <DrawerItem path="/" icon={<ListIcon />} title="My reservations" closeDrawer={closeDrawer} id="draweritems-myreservations" />
            {user.isCarwashAdmin && (
                <DrawerItem path="/carwashadmin" icon={<LocalCarWashIcon />} title="CarWash admin" closeDrawer={closeDrawer} id="draweritems-carwashadmin" />
            )}
            {user.isAdmin && <DrawerItem path="/admin" icon={<BuildIcon />} title="Admin" closeDrawer={closeDrawer} id="draweritems-admin" />}
            {(user.isAdmin || user.isCarwashAdmin) && (
                <DrawerItem path="/blockers" icon={<BlockIcon />} title="Blockers" closeDrawer={closeDrawer} id="draweritems-blockers" />
            )}
            {(user.isAdmin || user.isCarwashAdmin) && (
                <DrawerItem path="/analytics" icon={<BarChartIcon />} title="Analytics" closeDrawer={closeDrawer} id="draweritems-analytics" />
            )}
        </div>
    );
}

export function otherDrawerItems(closeDrawer) {
    return (
        <div>
            <DrawerItem path="/settings" icon={<SettingsIcon />} title="Settings" closeDrawer={closeDrawer} id="draweritems-settings" />
            <DrawerItem path="/support" icon={<HelpIcon />} title="Contact support" closeDrawer={closeDrawer} id="draweritems-support" />
            <ListItem button onClick={signOut} id="draweritems-signout">
                <ListItemIcon>
                    <ExitToAppIcon />
                </ListItemIcon>
                <ListItemText primary="Sign out" />
            </ListItem>
        </div>
    );
}
