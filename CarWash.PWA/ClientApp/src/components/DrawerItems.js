import React from 'react';
import { Link } from 'react-router-dom';
import ListItem from '@material-ui/core/ListItem';
import ListItemIcon from '@material-ui/core/ListItemIcon';
import ListItemText from '@material-ui/core/ListItemText';
import Fab from '@material-ui/core/Fab';
import AddIcon from '@material-ui/icons/Add';
import ListIcon from '@material-ui/icons/List';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import BuildIcon from '@material-ui/icons/Build';
import SettingsIcon from '@material-ui/icons/SettingsRounded';
import HelpIcon from '@material-ui/icons/Help';
import ExitToAppIcon from '@material-ui/icons/ExitToApp';
import BlockIcon from '@material-ui/icons/Block';
import BarChartIcon from '@material-ui/icons/BarChart';
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
