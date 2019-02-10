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
        <ListItem button component={Link} to={props.path} onClick={props.closeDrawer}>
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
            >
                <AddIcon style={{ marginRight: '16px' }} />
                Reserve
            </Fab>
            <DrawerItem path="/" icon={<ListIcon />} title="My reservations" closeDrawer={closeDrawer} />
            {user.isCarwashAdmin && <DrawerItem path="/carwashadmin" icon={<LocalCarWashIcon />} title="CarWash admin" closeDrawer={closeDrawer} />}
            {user.isAdmin && <DrawerItem path="/admin" icon={<BuildIcon />} title="Admin" closeDrawer={closeDrawer} />}
            {(user.isAdmin || user.isCarwashAdmin) && <DrawerItem path="/blockers" icon={<BlockIcon />} title="Blockers" closeDrawer={closeDrawer} />}
            {(user.isAdmin || user.isCarwashAdmin) && <DrawerItem path="/analytics" icon={<BarChartIcon />} title="Analytics" closeDrawer={closeDrawer} />}
        </div>
    );
}

export function otherDrawerItems(closeDrawer) {
    return (
        <div>
            <DrawerItem path="/settings" icon={<SettingsIcon />} title="Settings" closeDrawer={closeDrawer} />
            <DrawerItem path="/support" icon={<HelpIcon />} title="Contact support" closeDrawer={closeDrawer} />
            <ListItem button onClick={signOut}>
                <ListItemIcon>
                    <ExitToAppIcon />
                </ListItemIcon>
                <ListItemText primary="Sign out" />
            </ListItem>
        </div>
    );
}
