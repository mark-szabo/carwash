import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Button from '@material-ui/core/Button';
import Dialog from '@material-ui/core/Dialog';
import DialogContent from '@material-ui/core/DialogContent';
import withMobileDialog from '@material-ui/core/withMobileDialog';
import Typography from '@material-ui/core/Typography';
import Chip from '@material-ui/core/Chip';
import Divider from '@material-ui/core/Divider';
import IconButton from '@material-ui/core/IconButton';
import LocalCarWashIcon from '@material-ui/icons/LocalCarWash';
import LocalShippingIcon from '@material-ui/icons/LocalShipping';
import { getServiceName } from './Constants';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
    },
    divider: {
        margin: '0 24px',
        width: 1,
        height: 'inherit',
        flex: '0 0 auto',
    },
    content: {
        display: 'flex',
    },
    details: {
        flex: '1 1 auto',
    },
    actions: {
        flex: '0 0 auto',
    },
    button: {
        margin: theme.spacing.unit,
    },
    notSelectedMpv: {
        color: theme.palette.grey[300],
    },
});

class CarwashDetailsDialog extends React.Component {
    render() {
        const { reservation, open, handleClose, fullScreen, classes } = this.props;

        return (
            <Dialog open={open} onClose={handleClose} fullScreen={fullScreen}>
                <DialogContent className={classes.content}>
                    <div className={classes.details}>
                        <Typography>{reservation.vehiclePlateNumber}</Typography>
                        <Typography>{reservation.user.company}</Typography>
                        <Typography>{reservation.location}</Typography>
                        <Typography>
                            {reservation.user.firstName} {reservation.user.lastName}
                        </Typography>
                        <IconButton className={classes.button} aria-label="MPV">
                            {reservation.mpv ? <LocalShippingIcon /> : <LocalShippingIcon color="disabled" />}
                        </IconButton>
                        <Typography variant="subheading">Selected services</Typography>
                        {reservation.services.map(service => (
                            <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                        ))}
                    </div>
                    <Divider className={classes.divider} />
                    <div className={classes.actions}>
                        <Button onClick={this.handleClose} variant="fab" color="primary" aria-label="Start wash" className={classes.button} autoFocus>
                            <LocalCarWashIcon />
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>
        );
    }
}

CarwashDetailsDialog.propTypes = {
    classes: PropTypes.object.isRequired,
    reservation: PropTypes.object.isRequired,
    fullScreen: PropTypes.bool.isRequired,
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
};

export default withStyles(styles)(withMobileDialog()(CarwashDetailsDialog));
