import React, { Component } from 'react';
import { Route, Switch } from 'react-router';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';
import { createBrowserHistory } from 'history';
import apiFetch from './Auth';
import registerPush from './PushService';
import { ThemeProvider, StyledEngineProvider, createTheme, adaptV4Theme } from '@mui/material/styles';
import Snackbar from '@mui/material/Snackbar';
import Button from '@mui/material/Button';
import CssBaseline from '@mui/material/CssBaseline';
import moment from 'moment';
import * as signalR from '@microsoft/signalr';
import Layout from './components/Layout';
import Home from './components/Home';
import Reserve from './components/Reserve';
import Support from './components/Support';
import Admin from './components/Admin';
import Settings from './components/Settings';
import CarwashAdmin from './components/CarwashAdmin';
import NotificationDialog from './components/NotificationDialog';
import { NotificationChannel, BacklogHubMethods } from './Constants';
import Spinner from './components/Spinner';
import { sleep } from './Helpers';
import Blockers from './components/Blockers';
import Analytics from './components/Analytics';
import ErrorBoundary from './components/ErrorBoundary';
import NotFound from './components/NotFound';

// A theme with custom primary and secondary color.
const lightTheme = createTheme(adaptV4Theme({
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
        useNextVariants: true,
    },
}));

// DARK MODE
const darkTheme = createTheme(adaptV4Theme({
    palette: {
        mode: 'dark',
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
        background: {
            default: '#1d1d1d',
            paper: '#323232',
        },
    },
    typography: {
        fontFamily: ['"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
        useNextVariants: true,
    },
}));

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
        snackbarAction: null,
        notificationDialogOpen: false,
        theme: window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? darkTheme : lightTheme,
    };

    componentDidMount() {
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
                this.setState({ theme: e.matches ? darkTheme : lightTheme });
            });
        }

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

                // Initiate SignalR connection to Backlog Hub
                this.connectToBacklogHub();
            },
            error => {
                this.openSnackbar(error);
            }
        );

        if ('Notification' in window && Notification.permission === 'granted') {
            registerPush();
        }

        this.loadLastSettings();

        const browserHistory = createBrowserHistory({ basename: '' });
        const reactPlugin = new ReactPlugin();
        const appInsights = new ApplicationInsights({
            config: {
                instrumentationKey: 'd1ce1965-2171-4a11-9438-66114b31f88f',
                extensions: [reactPlugin],
                extensionConfig: {
                    [reactPlugin.identifier]: { history: browserHistory },
                },
            },
        });
        appInsights.loadAppInsights();

        this.keyboardListener();
    }

    backlogHubConnection;

    openSnackbar = (message, action) => {
        this.setState({
            snackbarOpen: true,
            snackbarMessage: message,
            snackbarAction: action,
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
        } else if (this.state.user.isAdmin) {
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
            return null;
        }

        return apiFetch('api/reservations/backlog').then(
            data => {
                const backlog = data;
                for (let i = 0; i < backlog.length; i++) {
                    backlog[i].startDate = moment(backlog[i].startDate);
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

    loadLastSettings = () => {
        apiFetch('api/reservations/lastsettings').then(
            data => {
                if (Object.keys(data).length !== 0) {
                    let garage;
                    if (data.location) {
                        [garage] = data.location.split('/');
                    }
                    this.setState({
                        lastSettings: {
                            services: data.services || [],
                            vehiclePlateNumber: data.vehiclePlateNumber,
                            garage,
                        },
                    });
                }
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

        // Delete cached response for /api/users/me
        // Not perfect solution as it seems Safari does not support this
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

    removeBacklogItem = backlogItemId => {
        this.setState(state => {
            let backlog = state.backlog;
            backlog = backlog.filter(item => item.id !== backlogItemId);

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

    invokeBacklogHub = (method, id) => {
        this.backlogHubConnection.invoke(method, id);
    };

    connectToBacklogHub = () => {
        this.backlogHubConnection = new signalR.HubConnectionBuilder().withUrl('/hub/backlog').build();

        if (this.state.user.isCarwashAdmin) {
            this.backlogHubConnection.on(BacklogHubMethods.ReservationCreated, id => {
                console.log(`SignalR: new reservation (${id})`);
                this.loadBacklog().then(() => this.openSnackbar('New reservation!'));
            });
            this.backlogHubConnection.on(BacklogHubMethods.ReservationUpdated, id => {
                console.log(`SignalR: a reservation was just updated (${id})`);
                this.loadBacklog();
            });
            this.backlogHubConnection.on(BacklogHubMethods.ReservationDeleted, id => {
                console.log(`SignalR: a reservation was just deleted (${id})`);
                this.loadBacklog().then(() => this.openSnackbar('A reservation was just deleted.'));
            });
            this.backlogHubConnection.on(BacklogHubMethods.ReservationDropoffConfirmed, id => {
                console.log(`SignalR: a key was just dropped off (${id})`);
                this.loadBacklog().then(() => this.openSnackbar('A key was just dropped off!'));
            });
        }

        this.backlogHubConnection.onclose(error => {
            console.error(`SignalR: Connection to the hub was closed. Reconnecting... (${error})`);
            if (this.state.user.isCarwashAdmin) this.loadBacklog();
            sleep(5000).then(() => this.backlogHubConnection.start().catch(e => console.error(e.toString())));
        });

        this.backlogHubConnection.start().catch(e => console.error(e.toString()));
    };

    keyboardListener = () => {
        const keys = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        document.addEventListener('keydown', event => {
            keys.shift();
            keys.push(event.key);
            if (
                keys[0] === 'ArrowUp' &&
                keys[1] === 'ArrowUp' &&
                keys[2] === 'ArrowDown' &&
                keys[3] === 'ArrowDown' &&
                keys[4] === 'ArrowLeft' &&
                keys[5] === 'ArrowRight' &&
                keys[6] === 'ArrowLeft' &&
                keys[7] === 'ArrowRight' &&
                keys[8] === 'b' &&
                keys[9] === 'a' &&
                keys[10] === 'Enter'
            ) {
                this.openSnackbar(
                    'Nice catch! Shoot me a message - I owe you a bier!',
                    <Button href="https://www.linkedin.com/in/mark-szabo/" color="secondary" size="small">
                        Contact
                    </Button>
                );
            }
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
            theme,
        } = this.state;

        return (
            <StyledEngineProvider injectFirst>
                <ThemeProvider theme={theme}>
                    <CssBaseline enableColorScheme />
                    <ErrorBoundary>
                        <Layout user={user}>
                            <Switch>
                                <Route
                                    exact
                                    path="/"
                                    navbarName="My reservations"
                                    refresh={this.loadReservations}
                                    render={props => (
                                        <ErrorBoundary>
                                            <Home
                                                reservations={reservations}
                                                reservationsLoading={reservationsLoading}
                                                removeReservation={this.removeReservation}
                                                updateReservation={this.updateReservation}
                                                invokeBacklogHub={this.invokeBacklogHub}
                                                lastSettings={lastSettings}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/reserve"
                                    navbarName="Reserve"
                                    render={props =>
                                        Object.keys(user).length !== 0 ? (
                                            <ErrorBoundary>
                                                <Reserve
                                                    user={user}
                                                    reservations={reservations}
                                                    addReservation={this.addReservation}
                                                    invokeBacklogHub={this.invokeBacklogHub}
                                                    lastSettings={lastSettings}
                                                    loadLastSettings={this.loadLastSettings}
                                                    openSnackbar={this.openSnackbar}
                                                    openNotificationDialog={this.openNotificationDialog}
                                                    {...props}
                                                />
                                            </ErrorBoundary>
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
                                            <ErrorBoundary>
                                                <Reserve
                                                    user={user}
                                                    reservations={reservations}
                                                    addReservation={this.addReservation}
                                                    removeReservation={this.removeReservation}
                                                    invokeBacklogHub={this.invokeBacklogHub}
                                                    loadLastSettings={this.loadLastSettings}
                                                    openSnackbar={this.openSnackbar}
                                                    openNotificationDialog={this.openNotificationDialog}
                                                    {...props}
                                                />
                                            </ErrorBoundary>
                                        ) : (
                                            <Spinner />
                                        )
                                    }
                                />
                                <Route
                                    exact
                                    path="/settings"
                                    navbarName="Settings"
                                    render={props => (
                                        <ErrorBoundary>
                                            <Settings user={user} updateUser={this.updateUser} openSnackbar={this.openSnackbar} {...props} />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/admin"
                                    navbarName="Admin"
                                    refresh={this.loadCompanyReservations}
                                    render={props => (
                                        <ErrorBoundary>
                                            <Admin
                                                reservations={companyReservations}
                                                reservationsLoading={companyReservationsLoading}
                                                removeReservation={this.removeReservationFromCompanyReservations}
                                                updateReservation={this.updateCompanyReservation}
                                                invokeBacklogHub={this.invokeBacklogHub}
                                                lastSettings={lastSettings}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/carwashadmin"
                                    navbarName="CarWash admin"
                                    refresh={this.loadBacklog}
                                    render={props => (
                                        <ErrorBoundary>
                                            <CarwashAdmin
                                                backlog={backlog}
                                                backlogLoading={backlogLoading}
                                                backlogUpdateFound={backlogUpdateFound}
                                                updateBacklogItem={this.updateBacklogItem}
                                                removeBacklogItem={this.removeBacklogItem}
                                                invokeBacklogHub={this.invokeBacklogHub}
                                                snackbarOpen={this.state.snackbarOpen}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/blockers"
                                    navbarName="Blockers"
                                    render={props => (
                                        <ErrorBoundary>
                                            <Blockers user={user} snackbarOpen={this.state.snackbarOpen} openSnackbar={this.openSnackbar} {...props} />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/analytics"
                                    navbarName="Analytics"
                                    render={props => (
                                        <ErrorBoundary>
                                            <Analytics {...props} />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    exact
                                    path="/support"
                                    navbarName="Support"
                                    render={props => (
                                        <ErrorBoundary>
                                            <Support {...props} />
                                        </ErrorBoundary>
                                    )}
                                />
                                <Route
                                    path="/"
                                    navbarName="Not found"
                                    render={props => (
                                        <ErrorBoundary>
                                            <NotFound {...props} />
                                        </ErrorBoundary>
                                    )}
                                />
                            </Switch>
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
                            action={this.state.snackbarAction}
                        />
                        <NotificationDialog
                            open={this.state.notificationDialogOpen}
                            handleClose={this.handleNotificationDialogClose}
                            openSnackbar={this.openSnackbar}
                            updateUser={this.updateUser}
                        />
                    </ErrorBoundary>
                </ThemeProvider>
            </StyledEngineProvider>
        );
    }
}

/**
<!-- Messenger Chat Plugin Code -->
<div id="fb-root"></div>

<!-- Your Chat Plugin code -->
<div id="fb-customer-chat" class="fb-customerchat"></div>

<script>
    var chatbox = document.getElementById("fb-customer-chat");
    chatbox.setAttribute("page_id", "268972003778293");
    chatbox.setAttribute("attribution", "biz_inbox");
</script>

<!-- Your SDK code -->
<script>
    window.fbAsyncInit = function () {
        FB.init({
            xfbml: true,
            version: "v16.0",
        });
    };

    (function (d, s, id) {
        var js,
            fjs = d.getElementsByTagName(s)[0];
        if (d.getElementById(id)) return;
        js = d.createElement(s);
        js.id = id;
        js.src = "https://connect.facebook.net/hu_HU/sdk/xfbml.customerchat.js";
        fjs.parentNode.insertBefore(js, fjs);
    })(document, "script", "facebook-jssdk");
</script>
 */
