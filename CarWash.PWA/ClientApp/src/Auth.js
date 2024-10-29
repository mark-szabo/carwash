import AuthenticationContext from 'adal-angular';
import * as download from 'downloadjs';

const adalConfig = {
    clientId: '212c82d9-446d-48f5-8f95-88ebe2e78dbd',
    endpoints: {
        api: '212c82d9-446d-48f5-8f95-88ebe2e78dbd',
    },
    cacheLocation: 'localStorage',
    extraQueryParameter: '',
    redirectUri: window.location.origin,
};

// https://github.com/salvoravida/react-adal

let authContext = new AuthenticationContext(adalConfig);

function adalGetToken(resourceGuid) {
    return new Promise((resolve, reject) => {
        authContext.acquireToken(resourceGuid, (message, token, msg) => {
            if (!msg) resolve(token);
            else {
                console.error(message);
                reject(message);
            }
        });
    });
}

export function runWithAdal(configuration, app) {
    // the app must run in an iframe to get a refreshToken (parsing hash and getting token)
    authContext.handleWindowCallback();

    // prevent iframe double app !!!
    if (window === window.parent) {
        if (!authContext.isCallback(window.location.hash)) {
            console.log('Authentication...');
            if (!authContext.getCachedToken(authContext.config.clientId) || !authContext.getCachedUser()) {
                console.log('No token found or token is expired. Redirecting...');
                if (authContext.getCachedUser()) {
                    const user = authContext.getCachedUser();
                    const loginHint = user.profile.upn ? user.profile.upn : user.profile.email;
                    adalConfig.extraQueryParameter = `login_hint=${encodeURIComponent(loginHint)}`;
                    authContext = new AuthenticationContext(adalConfig);
                    console.log(`User was prevously logged in with ${loginHint}. Using this email as login hint.`);
                    if (!navigator.onLine) {
                        console.error('Disconnected from network. Aborting login. Offline mode.');
                        app();
                        return;
                    }
                }
                authContext.login();
            } else {
                const user = authContext.getCachedUser();
                console.log('User is authenticated.');
                // console.log(user);
                if (configuration.companies.filter(c => c.tenantId === user.profile.tid).length > 0) {
                    app();
                } else {
                    console.error(`Tenant ${user.profile.tid} is not athorized to use this application!`);
                }
            }
        }
    }
}

export function signOut() {
    authContext.logOut();
}

/**
 * Requests a URL, returning the deserialized response body
 * @param {string} url The URL we want to request
 * @param {object} options The options we want to pass to "fetch"
 * @param {bool} errorIfOffline Indicates whether the function should reject the Promise if offline (default: false)
 * @return {Promise} The deserialized response body (Promise)
 */
export default async function apiFetch(url, options, errorIfOffline = false) {
    if (errorIfOffline && !navigator.onLine) {
        console.error('NETWORK ERROR: Disconnected from network.');
        return Promise.reject('You are offline.');
    }

    const token = await adalGetToken(adalConfig.endpoints.api);
    const o = options || {};
    if (!(o.headers instanceof Headers)) o.headers = new Headers();
    o.headers.append('Content-Type', 'application/json');
    o.headers.append('Authorization', `Bearer ${token}`);

    let response;
    try {
        response = await window.fetch(url, o);
    } catch (e) {
        if (!navigator.onLine) {
            console.error('NETWORK ERROR: Disconnected from network.');
            return Promise.reject('You are offline.');
        }

        console.error(`NETWORK ERROR: ${e.message}`);
        return Promise.reject('Network error. Are you offline?');
    }

    // Check for HTTP error codes
    if (!response.ok) {
        switch (response.status) {
            case 400:
                console.error(`BAD REQUEST: ${url}`);
                console.info(response);
                try {
                    const errorMessage = await response.json();
                    if (!(typeof errorMessage === 'string' || errorMessage instanceof String)) {
                        return Promise.reject(`An error has occured: ${Object.values(errorMessage?.errors)[0]}`);
                    }
                    return Promise.reject(`An error has occured: ${errorMessage}`);
                } catch (jsonError) {
                    return Promise.reject('An error has occured.');
                }
            case 401:
                console.error(`UNAUTHORIZED: ${url}`);
                console.info(response);
                return Promise.reject('You are not authorized. Please refresh the page!');
            case 403:
                console.error(`FORBIDDEN: ${url}`);
                console.info(response);
                return Promise.reject('You do not have permission!');
            case 404:
                console.error(`NOT FOUND: ${url}`);
                console.info(response);
                return Promise.reject('Not found.');
            case 500:
                console.error(`SERVER ERROR: ${url}`);
                console.info(response);
                return Promise.reject('A server error has occured.');
            default:
                console.error(`UNKNOWN ERROR: ${url}`);
                console.info(response);
                return Promise.reject('An error has occured.');
        }
    }

    // NoContent and Accepted would throw a JSON parsing error as they have no response body
    if (response.status === 204 || response.status === 202) return {};

    // Excel export file download
    if (response.headers.get('content-type').indexOf('application/vnd.openxmlformats-officedocument.spreadsheetml.sheet') !== -1) {
        let filename = 'carwash-export.xlsx';
        const disposition = response.headers.get('content-disposition');
        if (disposition && disposition.indexOf('attachment') !== -1) {
            const filenameRegex = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/;
            const matches = filenameRegex.exec(disposition);
            if (matches && matches[1]) {
                filename = matches[1].replace(/['"]/g, '');
            }
        }

        const blob = await response.blob();
        download(blob, filename, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');

        return {};
    }

    try {
        const body = await response.json();
        return body;
    } catch (jsonError) {
        console.error(`JSON PARSING ERROR: ${jsonError.message}`);
        return Promise.reject('A server error has occured.');
    }
}

export const getToken = () => authContext.getCachedToken(authContext.config.clientId);
