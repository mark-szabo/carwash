import React from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Button from '@material-ui/core/Button';
import Dialog from '@material-ui/core/Dialog';
import DialogActions from '@material-ui/core/DialogActions';
import DialogContent from '@material-ui/core/DialogContent';
import withMobileDialog from '@material-ui/core/withMobileDialog';
import Typography from '@material-ui/core/Typography';
import Chip from '@material-ui/core/Chip';
import Divider from '@material-ui/core/Divider';

const styles = theme => ({
    chip: {
        margin: '8px 8px 0 0',
    },
    divider: {
        margin: '24px 0',
    },
});

function getServiceName(service) {
    switch (service) {
        case 0:
            return 'exterior';
        case 1:
            return 'interior';
        case 2:
            return 'carpet';
        case 3:
            return 'spot cleaning';
        case 4:
            return 'vignette removal';
        case 5:
            return 'polishing';
        case 6:
            return "AC cleaning 'ozon'";
        case 7:
            return "AC cleaning 'bomba'";
        case 8:
            return 'bug removal';
        case 9:
            return 'wheel cleaning';
        case 10:
            return 'tire care';
        case 11:
            return 'leather care';
        case 12:
            return 'plastic care';
        case 13:
            return 'prewash';
        default:
            return 'no info';
    }
}

class ResponsiveDialog extends React.Component {
    render() {
        const { reservation, open, handleClose, fullScreen, classes } = this.props;

        return (
            <Dialog open={open} onClose={handleClose} fullScreen={fullScreen}>
                <DialogContent>
                    <Typography>
                        {reservation.vehiclePlateNumber}
                        {reservation.user.company}
                        {reservation.location}
                        {reservation.user.firstName} {reservation.user.lastName}
                    </Typography>
                    <Divider className={classes.divider} />
                    <Typography variant="subheading">Selected services</Typography>
                    {reservation.services.map(service => (
                        <Chip label={getServiceName(service)} className={classes.chip} key={service} />
                    ))}
                </DialogContent>
                <DialogActions>
                    <Button onClick={this.handleClose} color="primary">
                        Disagree
                    </Button>
                    <Button onClick={this.handleClose} color="primary" autoFocus>
                        Start wash
                    </Button>
                </DialogActions>
            </Dialog>
        );
    }
}

ResponsiveDialog.propTypes = {
    classes: PropTypes.object.isRequired,
    reservation: PropTypes.object.isRequired,
    fullScreen: PropTypes.bool.isRequired,
    open: PropTypes.bool.isRequired,
    handleClose: PropTypes.func.isRequired,
};

export default withStyles(styles)(withMobileDialog()(ResponsiveDialog));
