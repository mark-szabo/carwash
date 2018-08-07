import React, { Component } from 'react';
import PropTypes from 'prop-types';
import { withStyles } from '@material-ui/core/styles';
import Card from '@material-ui/core/Card';
import CardActions from '@material-ui/core/CardActions';
import CardContent from '@material-ui/core/CardContent';
import CardMedia from '@material-ui/core/CardMedia';
import Button from '@material-ui/core/Button';
import Typography from '@material-ui/core/Typography';
import Grow from '@material-ui/core/Grow';

const styles = {
    card: {
        maxWidth: 345,
    },
    media: {
        height: 0,
        paddingTop: '48.83%', // 512:250
    },
};

class ReservationCard extends Component {
    render() {
        const { classes, reservation } = this.props;
        return (
            <Grow in={true}>
                <Card className={classes.card}>
                    <CardMedia
                        className={classes.media}
                        image={`/images/state${reservation.state}.png`}
                    />
                    <CardContent>
                        <Typography gutterBottom variant="headline" component="h2">
                            {new Intl.DateTimeFormat('en-US',
                                {
                                    month: 'long',
                                    day: '2-digit',
                                    hour: '2-digit',
                                    minute: '2-digit'
                                }).format(new Date(reservation.dateFrom))}
          </Typography>
                        <Typography component="p">
                            {reservation.vehiclePlateNumber}
          </Typography>
                    </CardContent>
                    <CardActions>
                        <Button size="small" color="primary">
                            Share
          </Button>
                        <Button size="small" color="primary">
                            Learn More
          </Button>
                    </CardActions>
                </Card>
            </Grow>
        );
    }
}

ReservationCard.propTypes = {
    classes: PropTypes.object.isRequired,
};

export default withStyles(styles)(ReservationCard);