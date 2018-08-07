import React, { Component } from 'react';
import { adalFetch } from '../Auth';
import ReservationCard from './ReservationCard';
import Grid from '@material-ui/core/Grid';

export class Home extends Component {
    displayName = Home.name

    constructor(props) {
        super(props);
        this.state = { reservations: [], loading: true };
    }

    componentDidMount() {
        adalFetch('api/reservations')
            .then(response => response.json())
            .then(data => {
                this.setState({ reservations: data, loading: false });
            });
    }

    render() {
        if (this.state.loading) {
            return (<p>Loading...</p>);
        } else {
            return (
                <Grid
                    container
                    direction="row"
                    justify="flex-start"
                    alignItems="flex-start"
                    spacing={16}
                    style={{ maxHeight: 'calc(100% - 24px - 16px)', overflow: 'auto' }}
                >
                    {this.state.reservations.map(reservation =>
                        <Grid item key={reservation.id} >
                            <ReservationCard reservation={reservation}/>
                        </Grid>
                    )}
                </Grid>
            );
        }
    }
}
