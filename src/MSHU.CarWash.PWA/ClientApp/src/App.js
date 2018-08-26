import React, { Component } from 'react';
import { Route } from 'react-router';
import { AppInsights } from 'applicationinsights-js';
import apiFetch from './Auth';
import { MuiThemeProvider, createMuiTheme } from '@material-ui/core/styles';
import Snackbar from '@material-ui/core/Snackbar';
import Layout from './components/Layout';
import Home from './components/Home';
import Reserve from './components/Reserve';
import Support from './components/Support';
import Admin from './components/Admin';
import Settings from './components/Settings';
import CarwashAdmin from './components/CarwashAdmin';

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
        fontFamily: ['"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
    },
});

function getSafeString(obj) {
    return JSON.stringify(obj)
        .replace(/'|"|{|}|\[|\]/g, ' ')
        .replace(/ :/g, ':')
        .replace(/ ,/g, ',')
        .replace(/:./g, ': ')
        .replace(/,./g, ', ')
        .trim();
}

export default class App extends Component {
    displayName = App.name;

    state = {
        user: {},
        reservations: [],
        reservationsLoading: true,
        companyReservations: [],
        companyReservationsLoading: true,
        backlog: [],
        backlogLoading: true,
        backlogUpdateFound: false,
        snackbarOpen: false,
        snackbarMessage: '',
    };

    componentDidMount() {
        apiFetch('api/reservations')
            .then(
                data => {
                    this.setState({
                        reservations: data,
                        reservationsLoading: false,
                    });
                },
                error => {
                    this.setState({ reservationsLoading: false });
                    this.openSnackbar(error);
                }
            )
            .then(() => {
                apiFetch('api/users/me').then(
                    data => {
                        this.setState({ user: data });

                        if (data.isAdmin) {
                            this.loadCompanyReservations();
                        }

                        if (data.isCarwashAdmin) {
                            this.loadBacklog();
                        }
                    },
                    error => {
                        this.openSnackbar(error);
                    }
                );
            });

        /* Call downloadAndSetup to download full ApplicationInsights script from CDN and initialize it with instrumentation key */
        AppInsights.downloadAndSetup({ instrumentationKey: 'd1ce1965-2171-4a11-9438-66114b31f88f' });
    }

    openSnackbar = message => {
        this.setState({
            snackbarOpen: true,
            snackbarMessage: message,
        });
    };

    addReservation = reservation => {
        if (reservation.userId === this.state.user.id) {
            this.setState(state => {
                const reservations = [...state.reservations];
                reservations.unshift(reservation);

                return { reservations };
            });
        } else {
            this.setState({
                companyReservationsLoading: true,
            });
            this.loadCompanyReservations();
        }
    };

    removeReservation = reservationId => {
        this.setState(state => {
            let reservations = [...state.reservations];
            reservations = reservations.filter(r => r.id !== reservationId);

            return { reservations };
        });
    };

    removeReservationFromCompanyReservations = reservationId => {
        this.setState(state => {
            let companyReservations = [...state.companyReservations];
            companyReservations = companyReservations.filter(r => r.id !== reservationId);

            return { companyReservations };
        });
    };

    loadCompanyReservations = () => {
        apiFetch('api/reservations/company').then(
            data => {
                this.setState({ companyReservations: data, companyReservationsLoading: false });
            },
            error => {
                this.openSnackbar(error);
            }
        );
    };

    loadBacklog = () => {
        apiFetch('api/reservations/backlog').then(
            data => {
                const backlog = data;
                for (let i = 0; i < backlog.length; i++) {
                    backlog[i].startDate = new Date(backlog[i].startDate);
                }
                this.setState({ backlog, backlogLoading: false });
            },
            error => {
                this.openSnackbar(error);
            }
        );
    };

    updateUser = (key, value) => {
        this.setState(state => {
            const user = state.user;
            user[key] = value;

            return { user };
        });
    };

    handleSnackbarClose = () => {
        this.setState({
            snackbarOpen: false,
        });
    };

    render() {
        const {
            user,
            reservations,
            reservationsLoading,
            companyReservations,
            companyReservationsLoading,
            backlog,
            backlogLoading,
            backlogUpdateFound,
        } = this.state;

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
                            />
                        )}
                    />
                    <Route
                        exact
                        path="/reserve"
                        navbarName="Reserve"
                        render={props => (
                            <Reserve user={user} reservations={reservations} addReservation={this.addReservation} openSnackbar={this.openSnackbar} {...props} />
                        )}
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
                            />
                        )}
                    />
                    <Route
                        path="/settings"
                        navbarName="Settings"
                        render={props => <Settings user={user} updateUser={this.updateUser} openSnackbar={this.openSnackbar} {...props} />}
                    />
                    <Route
                        exact
                        path="/admin"
                        navbarName="Admin"
                        render={props => (
                            <Admin
                                reservations={companyReservations}
                                reservationsLoading={companyReservationsLoading}
                                removeReservation={this.removeReservationFromCompanyReservations}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />
                        )}
                    />
                    <Route
                        exact
                        path="/carwashadmin"
                        navbarName="CarWash admin"
                        render={props => (
                            <CarwashAdmin
                                backlog={backlog}
                                backlogLoading={backlogLoading}
                                backlogUpdateFound={backlogUpdateFound}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />
                        )}
                    />
                    <Route exact path="/support" navbarName="Support" component={Support} />
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
                    message={<span id="message-id">{getSafeString(this.state.snackbarMessage)}</span>}
                />
            </MuiThemeProvider>
        );
    }
}
