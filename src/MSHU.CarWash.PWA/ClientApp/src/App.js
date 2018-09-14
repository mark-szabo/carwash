import React, { Component } from 'react';
import { Route } from 'react-router';
import { AppInsights } from 'applicationinsights-js';
import apiFetch from './Auth';
import registerPush from './PushService';
import { MuiThemeProvider, createMuiTheme } from '@material-ui/core/styles';
import Snackbar from '@material-ui/core/Snackbar';
import Layout from './components/Layout';
import Home from './components/Home';
import Reserve from './components/Reserve';
import Support from './components/Support';
import Admin from './components/Admin';
import Settings from './components/Settings';
import CarwashAdmin from './components/CarwashAdmin';
import NotificationDialog from './components/NotificationDialog';
import { NotificationChannel } from './Constants';
import Spinner from './components/Spinner';

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
        lastSettings: {},
        snackbarOpen: false,
        snackbarMessage: '',
        notificationDialogOpen: false,
    };

    componentDidMount() {
        apiFetch('api/reservations').then(
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
        );

        apiFetch('api/users/me').then(
            data => {
                this.setState({ user: data });

                if (data.notificationChannel === NotificationChannel.Push) {
                    this.openNotificationDialog();
                }

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

        if ('Notification' in window && Notification.permission === 'granted') {
            registerPush();
        }

        apiFetch('api/reservations/lastsettings').then(
            data => {
                if (Object.keys(data).length !== 0) {
                    let garage;
                    if (data.location) {
                        [garage] = data.location.split('/');
                    }
                    this.setState({
                        lastSettings: {
                            vehiclePlateNumber: data.vehiclePlateNumber,
                            garage,
                        },
                    });
                }
            },
            error => {
                this.props.openSnackbar(error);
            }
        );

        /* Call downloadAndSetup to download full ApplicationInsights script from CDN and initialize it with instrumentation key */
        AppInsights.downloadAndSetup({ instrumentationKey: 'd1ce1965-2171-4a11-9438-66114b31f88f' });
    }

    openSnackbar = message => {
        this.setState({
            snackbarOpen: true,
            snackbarMessage: message,
        });
    };

    handleSnackbarClose = () => {
        this.setState({
            snackbarOpen: false,
        });
    };

    openNotificationDialog = () => {
        this.setState({
            notificationDialogOpen: true,
        });
    };

    handleNotificationDialogClose = () => {
        this.setState({
            notificationDialogOpen: false,
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

    loadReservations = refresh => {
        if (refresh && !navigator.onLine) {
            this.openSnackbar('You are offline.');
            return;
        }

        apiFetch('api/reservations').then(
            data => {
                this.setState({
                    reservations: data,
                    reservationsLoading: false,
                });
                if (refresh) this.openSnackbar('Refreshed.');
            },
            error => {
                this.setState({ reservationsLoading: false });
                this.openSnackbar(error);
            }
        );
    };

    loadCompanyReservations = refresh => {
        if (refresh && !navigator.onLine) {
            this.openSnackbar('You are offline.');
            return;
        }

        apiFetch('api/reservations/company').then(
            data => {
                this.setState({ companyReservations: data, companyReservationsLoading: false });
                if (refresh) this.openSnackbar('Refreshed.');
            },
            error => {
                this.setState({ companyReservationsLoading: false });
                this.openSnackbar(error);
            }
        );
    };

    loadBacklog = refresh => {
        if (refresh && !navigator.onLine) {
            this.openSnackbar('You are offline.');
            return;
        }

        apiFetch('api/reservations/backlog').then(
            data => {
                const backlog = data;
                for (let i = 0; i < backlog.length; i++) {
                    backlog[i].startDate = new Date(backlog[i].startDate);
                }
                this.setState({ backlog, backlogLoading: false });
                if (refresh) this.openSnackbar('Refreshed.');
            },
            error => {
                this.setState({ backlogLoading: false });
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

        // Delete cached response for /api/users/me
        // Not perfect solution as it seems Safari does not supports this
        // https://developer.mozilla.org/en-US/docs/Web/API/Cache/delete#Browser_compatibility
        try {
            caches.open('api-cache').then(cache => {
                cache.delete('/api/users/me');
            });
        } catch (error) {
            console.error(`Cannot delete user data from cache: ${error}`);
        }
    };

    updateBacklogItem = backlogItem => {
        this.setState(state => {
            const backlog = state.backlog;
            const i = backlog.findIndex(item => item.id === backlogItem.id);
            backlog[i] = backlogItem;

            return { backlog };
        });
    };

    updateReservation = reservation => {
        this.setState(state => {
            const reservations = state.reservations;
            const i = reservations.findIndex(item => item.id === reservation.id);
            reservations[i] = reservation;

            return { reservations };
        });
    };

    updateCompanyReservation = reservation => {
        this.setState(state => {
            const companyReservations = state.companyReservations;
            const i = companyReservations.findIndex(item => item.id === reservation.id);
            companyReservations[i] = reservation;

            return { companyReservations };
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
            lastSettings,
        } = this.state;

        return (
            <MuiThemeProvider theme={theme}>
                <Layout user={user}>
                    <Route
                        exact
                        path="/"
                        navbarName="My reservations"
                        refresh={this.loadReservations}
                        render={props => (
                            <Home
                                reservations={reservations}
                                reservationsLoading={reservationsLoading}
                                removeReservation={this.removeReservation}
                                updateReservation={this.updateReservation}
                                lastSettings={lastSettings}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />
                        )}
                    />
                    <Route
                        exact
                        path="/reserve"
                        navbarName="Reserve"
                        render={props =>
                            Object.keys(user).length !== 0 ? (
                                <Reserve
                                    user={user}
                                    reservations={reservations}
                                    addReservation={this.addReservation}
                                    lastSettings={lastSettings}
                                    openSnackbar={this.openSnackbar}
                                    openNotificationDialog={this.openNotificationDialog}
                                    {...props}
                                />
                            ) : (
                                <Spinner />
                            )
                        }
                    />
                    <Route
                        path="/reserve/:id"
                        navbarName="Reserve"
                        render={props =>
                            Object.keys(user).length !== 0 ? (
                                <Reserve
                                    user={user}
                                    reservations={reservations}
                                    addReservation={this.addReservation}
                                    removeReservation={this.removeReservation}
                                    openSnackbar={this.openSnackbar}
                                    openNotificationDialog={this.openNotificationDialog}
                                    {...props}
                                />
                            ) : (
                                <Spinner />
                            )
                        }
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
                        refresh={this.loadCompanyReservations}
                        render={props => (
                            <Admin
                                reservations={companyReservations}
                                reservationsLoading={companyReservationsLoading}
                                removeReservation={this.removeReservationFromCompanyReservations}
                                updateReservation={this.updateCompanyReservation}
                                lastSettings={lastSettings}
                                openSnackbar={this.openSnackbar}
                                {...props}
                            />
                        )}
                    />
                    <Route
                        exact
                        path="/carwashadmin"
                        navbarName="CarWash admin"
                        refresh={this.loadBacklog}
                        render={props => (
                            <CarwashAdmin
                                backlog={backlog}
                                backlogLoading={backlogLoading}
                                backlogUpdateFound={backlogUpdateFound}
                                snackbarOpen={this.state.snackbarOpen}
                                openSnackbar={this.openSnackbar}
                                updateBacklogItem={this.updateBacklogItem}
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
                <NotificationDialog
                    open={this.state.notificationDialogOpen}
                    handleClose={this.handleNotificationDialogClose}
                    openSnackbar={this.openSnackbar}
                    updateUser={this.updateUser}
                />
            </MuiThemeProvider>
        );
    }
}
