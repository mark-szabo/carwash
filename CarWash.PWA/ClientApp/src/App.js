import { Component } from 'react';
import PropTypes from 'prop-types';
import { Route, Switch } from 'react-router';
import { Workbox } from 'workbox-window';
import { ApplicationInsights } from '@microsoft/applicationinsights-web';
import { ReactPlugin } from '@microsoft/applicationinsights-react-js';
import { createBrowserHistory } from 'history';
import apiFetch, { getToken } from './Auth';
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
import SystemMessageBar from './components/SystemMessageBar';
import SystemMessagesAdmin from './components/SystemMessagesAdmin';
import { NotificationChannel, BacklogHubMethods, KeyLockerHubMethods } from './Constants';
import Spinner from './components/Spinner';
import { sleep } from './Helpers';
import Blockers from './components/Blockers';
import Analytics from './components/Analytics';
import ErrorBoundary from './components/ErrorBoundary';
import NotFound from './components/NotFound';
import PhoneNumberDialog from './components/PhoneNumberDialog';

// A theme with custom primary and secondary color.
const lightTheme = createTheme(
    adaptV4Theme({
        palette: {
            primary: {
                light: '#b5ffff',
                main: '#80d8ff',
                dark: '#49a7cc',
            },
            secondary: {
                light: '#99ffeb',
                main: '#80ffe6',
                dark: '#59b2a1',
            },
            background: {
                default: '#fafafa',
            },
            bw: {
                main: 'rgba(0, 0, 0, 0.54)',
            },
        },
        typography: {
            fontFamily: ['"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
            useNextVariants: true,
        },
    })
);

// DARK MODE
const darkTheme = createTheme(
    adaptV4Theme({
        palette: {
            mode: 'dark',
            primary: {
                light: '#b5ffff',
                main: '#80d8ff',
                dark: '#49a7cc',
            },
            secondary: {
                light: '#99ffeb',
                main: '#80ffe6',
                dark: '#59b2a1',
            },
            background: {
                default: '#1d1d1d',
                paper: '#323232',
            },
            bw: {
                main: '#fff',
            },
        },
        typography: {
            fontFamily: ['"Segoe UI"', 'Roboto', '"Helvetica Neue"', 'Arial', 'sans-serif'].join(','),
            useNextVariants: true,
        },
    })
);

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
    state = {
        version: '',
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
        phoneNumberDialogOpen: false,
        theme: window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? darkTheme : lightTheme,
        searchTerm: '',
        closedKeyLockerBoxIds: [],
    };

    componentDidMount() {
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
                this.setState({ theme: e.matches ? darkTheme : lightTheme });
            });
        }

        apiFetch('api/users/me').then(
            data => {
                this.setState({ user: data });

                // Show phone number dialog if missing
                if (!data.phoneNumber) {
                    this.setState({ phoneNumberDialogOpen: true });
                }

                this.loadReservations(false);

                this.loadLastSettings();

                if (data.notificationChannel === NotificationChannel.Push) {
                    this.openNotificationDialog();
                }

                if (data.isAdmin) {
                    this.loadCompanyReservations();
                }

                if (data.isCarwashAdmin) {
                    this.loadBacklog();
                }

                // Initiate SignalR connections
                this.connectToBacklogHub();
                this.connectToKeyLockerHub();
            },
            error => {
                this.openSnackbar(error);
                this.setState({ reservationsLoading: false });
            }
        );

        // Register service worker
        this.registerServiceWorker();

        const browserHistory = createBrowserHistory({ basename: '' });
        const reactPlugin = new ReactPlugin();
        const appInsights = new ApplicationInsights({
            config: {
                instrumentationKey: 'db8cb41a-462d-47f3-8d08-d4f7d5c5a0c7',
                extensions: [reactPlugin],
                extensionConfig: {
                    [reactPlugin.identifier]: { history: browserHistory },
                },
            },
        });

        appInsights.addTelemetryInitializer(envelope => {
            if (
                envelope.baseType === 'RemoteDependencyData' &&
                /POST .*\/hub\/backlog.*/.test(envelope.data.baseData.name)
            ) {
                return false;
            }
            return true;
        });

        appInsights.loadAppInsights();

        this.keyboardListener();
    }

    keyLockerHubConnection = null; // SignalR connection to the key locker Hub
    backlogHubConnection = null; // SignalR connection to the backlog Hub

    registerServiceWorker = async () => {
        if ('serviceWorker' in navigator) {
            const wb = new Workbox('/sw.js');

            wb.addEventListener('activated', event => {
                // `event.isUpdate` will be true if another version of the service
                // worker was controlling the page when this version was registered.
                if (event.isUpdate) {
                    this.openSnackbar('Application was successfully updated.');
                } else {
                    this.openSnackbar('Application is cached for offline use.');
                }
            });

            // Add an event listener to detect when the registered
            // service worker has installed but is waiting to activate.
            wb.addEventListener('waiting', event => {
                // Reload the page as soon as the previously waiting
                // service worker has taken control.
                wb.addEventListener('controlling', () => {
                    window.location.reload();
                });

                // When `event.wasWaitingBeforeRegister` is true, a previously
                // updated service worker is still waiting.
                if (event.wasWaitingBeforeRegister) {
                    this.openSnackbar(
                        'Update now! A new version is available.',
                        <Button color="secondary" size="small" onClick={() => wb.messageSkipWaiting()}>
                            Reload
                        </Button>
                    );
                } else {
                    this.openSnackbar(
                        'A new version is available.',
                        <Button color="secondary" size="small" onClick={() => wb.messageSkipWaiting()}>
                            Reload
                        </Button>
                    );
                }
            });

            wb.addEventListener('message', event => {
                if (event.data.type === 'CACHE_UPDATED') {
                    console.log(`A newer version of ${event.data.payload.updatedURL} is available!`);
                }
            });

            wb.register();

            const version = await wb.messageSW({ type: 'GET_VERSION' });
            this.setState({
                version,
            });

            // Register push notifications
            if ('Notification' in window && Notification.permission === 'granted') {
                registerPush();
            }
        } else {
            // Internet Explorer 6-11
            const isIE = /* @cc_on!@*/ false || !!document.documentMode;
            const surpassIEBlock = window.location.href.search('surpassieblock') !== -1;

            if (isIE && !surpassIEBlock) window.location = '/ieblock.html';
        }
    };

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

    handlePhoneNumberDialogClose = () => {
        this.setState({ phoneNumberDialogOpen: false });
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
                        // Treat incoming date as UTC, then convert to local time for display
                        backlog[i].startDate = moment.utc(backlog[i].startDate).local();
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
        caches
            .open('api-cache')
            .then(cache => {
                cache.delete('/api/users/me');
            })
            .catch(error => {
                console.error(`Cannot delete user data from cache: ${error}`);
            });
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

    connectToKeyLockerHub = () => {
        this.keyLockerHubConnection = new signalR.HubConnectionBuilder()
            .withUrl('/hub/keylocker', { accessTokenFactory: () => getToken() })
            .build();

        this.keyLockerHubConnection.on(KeyLockerHubMethods.KeyLockerBoxClosed, id => {
            console.log(`SignalR: key locker box closed (${id})`);
            this.setState(prevState => {
                const closedKeyLockerBoxIds = prevState.closedKeyLockerBoxIds || [];
                if (!closedKeyLockerBoxIds.includes(id)) {
                    return { closedKeyLockerBoxIds: [...closedKeyLockerBoxIds, id] };
                }
                return null;
            });
        });

        this.keyLockerHubConnection.on(KeyLockerHubMethods.KeyLockerBoxOpened, id => {
            console.log(`SignalR: key locker box opened (${id})`);
            this.setState(prevState => {
                const closedKeyLockerBoxIds = prevState.closedKeyLockerBoxIds || [];
                return { closedKeyLockerBoxIds: closedKeyLockerBoxIds.filter(lockerId => lockerId !== id) };
            });
        });

        this.keyLockerHubConnection.onclose(error => {
            console.error(`SignalR: Connection to the key locker hub was closed. Reconnecting... (${error})`);
            this.openSnackbar('Connection lost. Reconnecting...');
            sleep(5000).then(() => this.keyLockerHubConnection.start().catch(e => console.error(e.toString())));
        });

        this.keyLockerHubConnection.start().catch(e => console.error(e.toString()));
    };

    connectToBacklogHub = () => {
        if (!this.state.user.isCarwashAdmin) return;

        this.backlogHubConnection = new signalR.HubConnectionBuilder()
            .withUrl('/hub/backlog', { accessTokenFactory: () => getToken() })
            .build();

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
        this.backlogHubConnection.on(BacklogHubMethods.ReservationChatMessageSent, id => {
            console.log(`SignalR: a chat message was just received (${id})`);
            this.loadBacklog().then(() => this.openSnackbar('New message received!'));
        });

        this.backlogHubConnection.onclose(error => {
            console.error(`SignalR: Connection to the backlog hub was closed. Reconnecting... (${error})`);
            this.loadBacklog();
            sleep(5000).then(() => this.backlogHubConnection.start().catch(e => console.error(e.toString())));
        });

        this.backlogHubConnection.start().catch(e => console.error(e.toString()));
    };

    keyboardListener = () => {
        const keys = new Array(11);
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
                    "Nice catch! Shoot me a message - let's have a coffee!",
                    <Button href="https://www.linkedin.com/in/mark-szabo/" color="secondary" size="small">
                        Contact
                    </Button>
                );
            }
        });
    };

    handleSearchChange = event => {
        this.setState({ searchTerm: event.target.value });
    };

    render() {
        const { configuration } = this.props;

        const {
            version,
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
            searchTerm,
        } = this.state;

        return (
            <StyledEngineProvider injectFirst>
                <ThemeProvider theme={theme}>
                    <CssBaseline enableColorScheme />
                    <ErrorBoundary>
                        <Layout
                            user={user}
                            configuration={configuration}
                            version={version}
                            searchTerm={searchTerm}
                            handleSearchChange={this.handleSearchChange}
                        >
                            <Switch>
                                <Route
                                    exact
                                    path="/"
                                    navbarName="My reservations"
                                    refresh={this.loadReservations}
                                    render={props => (
                                        <ErrorBoundary>
                                            <SystemMessageBar messages={configuration.activeSystemMessages} />
                                            <Home
                                                reservations={reservations}
                                                configuration={configuration}
                                                reservationsLoading={reservationsLoading}
                                                removeReservation={this.removeReservation}
                                                updateReservation={this.updateReservation}
                                                lastSettings={lastSettings}
                                                openSnackbar={this.openSnackbar}
                                                dropoffDeepLink={props.location.hash === '#dropoffkey'}
                                                closedKeyLockerBoxIds={this.state.closedKeyLockerBoxIds}
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
                                                    configuration={configuration}
                                                    addReservation={this.addReservation}
                                                    lastSettings={lastSettings}
                                                    loadLastSettings={this.loadLastSettings}
                                                    updateUser={this.updateUser}
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
                                                    configuration={configuration}
                                                    addReservation={this.addReservation}
                                                    removeReservation={this.removeReservation}
                                                    loadLastSettings={this.loadLastSettings}
                                                    updateUser={this.updateUser}
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
                                            <Settings
                                                user={user}
                                                updateUser={this.updateUser}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
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
                                                configuration={configuration}
                                                reservationsLoading={companyReservationsLoading}
                                                removeReservation={this.removeReservationFromCompanyReservations}
                                                updateReservation={this.updateCompanyReservation}
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
                                                configuration={configuration}
                                                backlog={backlog}
                                                backlogLoading={backlogLoading}
                                                backlogUpdateFound={backlogUpdateFound}
                                                updateBacklogItem={this.updateBacklogItem}
                                                removeBacklogItem={this.removeBacklogItem}
                                                snackbarOpen={this.state.snackbarOpen}
                                                openSnackbar={this.openSnackbar}
                                                searchTerm={searchTerm}
                                                closedKeyLockerBoxIds={this.state.closedKeyLockerBoxIds}
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
                                            <Blockers
                                                user={user}
                                                snackbarOpen={this.state.snackbarOpen}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
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
                                    exact
                                    path="/system-messages"
                                    navbarName="System messages"
                                    render={props => (
                                        <ErrorBoundary>
                                            <SystemMessagesAdmin
                                                user={user}
                                                openSnackbar={this.openSnackbar}
                                                {...props}
                                            />
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
                        <PhoneNumberDialog
                            open={this.state.phoneNumberDialogOpen}
                            handleClose={this.handlePhoneNumberDialogClose}
                            openSnackbar={this.openSnackbar}
                            updateUser={this.updateUser}
                        />
                    </ErrorBoundary>
                </ThemeProvider>
            </StyledEngineProvider>
        );
    }
}

App.propTypes = {
    configuration: PropTypes.object.isRequired, // eslint-disable-line react/forbid-prop-types
};
