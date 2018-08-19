import React, { Component } from 'react';
import { Route } from 'react-router';
import apiFetch from './Auth';
import { MuiThemeProvider, createMuiTheme } from '@material-ui/core/styles';
import Snackbar from '@material-ui/core/Snackbar';
import Layout from './components/Layout';
import Home from './components/Home';
import Reserve from './components/Reserve';
import Support from './components/Support';

// A theme with custom primary and secondary color.
const theme = createMuiTheme({
    palette: {
        primary: {
            light: '#b5ffff',
            main: '#80d8ff',
            dark: '#49a7cc',
        },
        secondary: {
            light: '#b5ffff',
            main: '#80d8ff',
            dark: '#49a7cc',
        },
    },
    typography: {
        fontFamily: [
            '"Segoe UI"',
            'Roboto',
            '"Helvetica Neue"',
            'Arial',
            'sans-serif'
        ].join(','),
    },
});

export default class App extends Component {
    displayName = App.name

    state = {
        user: {},
        reservations: [],
        reservationsLoading: true,
        snackbarOpen: false,
        snackbarMessage: '',
    };

    componentDidMount() {
        apiFetch('api/users/me')
            .then((data) => {
                this.setState({ user: data });
            }, (error) => {
                this.openSnackbar(error);
            });

        apiFetch('api/reservations')
            .then((data) => {
                this.setState({
                    reservations: data,
                    reservationsLoading: false
                });
            }, (error) => {
                this.setState({ reservationsLoading: false });
                this.openSnackbar(error);
            });
    }

    openSnackbar = (message) => {
        this.setState({
            snackbarOpen: true,
            snackbarMessage: message,
        });
    }

    addReservation = (reservation) => {
        this.setState(state => {
            let reservations = [...state.reservations];
            reservations.unshift(reservation);

            return { reservations };
        });
    }

    removeReservation = (reservationId) => {
        this.setState(state => {
            let reservations = [...state.reservations];
            reservations = reservations.filter(r => r.id !== reservationId);

            return { reservations };
        });
    }

    handleSnackbarClose = () => {
        this.setState({
            snackbarOpen: false,
        });
    };

    render() {
        const { user, reservations, reservationsLoading } = this.state;
        return (
            <MuiThemeProvider theme={theme}>
                <Layout user={user}>
                    <Route
                        exact
                        path="/"
                        navbarName="My reservations"
                        render={props => (
                            <Home
                                reservations={reservations}
                                reservationsLoading={reservationsLoading}
                                removeReservation={this.removeReservation}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />)}
                    />
                    <Route
                        exact
                        path="/reserve"
                        navbarName="Reserve"
                        render={props => (
                            <Reserve
                                user={user}
                                reservations={reservations}
                                addReservation={this.addReservation}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />)}
                    />
                    <Route
                        path="/reserve/:id"
                        navbarName="Reserve"
                        render={props => (
                            <Reserve
                                user={user}
                                reservations={reservations}
                                addReservation={this.addReservation}
                                removeReservation={this.removeReservation}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />)}
                    />
                    <Route
                        exact
                        path="/support"
                        navbarName="Support"
                        component={Support}
                    />
                </Layout>
                <Snackbar
                    anchorOrigin={{
                        vertical: 'bottom',
                        horizontal: 'left',
                    }}
                    open={this.state.snackbarOpen}
                    autoHideDuration={6000}
                    onClose={this.handleSnackbarClose}
                    ContentProps={{
                        'aria-describedby': 'message-id',
                    }}
                    message={<span id="message-id">{this.state.snackbarMessage}</span>}
                />
            </MuiThemeProvider>
        );
    }
}
