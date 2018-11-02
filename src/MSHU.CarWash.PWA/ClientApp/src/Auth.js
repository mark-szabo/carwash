import AuthenticationContext from 'adal-angular';
import * as download from 'downloadjs';

const adalConfig = {
    clientId: '6e291d40-2613-4a74-9af5-790eb496a828',
    endpoints: {
        api: '6e291d40-2613-4a74-9af5-790eb496a828',
    },
    cacheLocation: 'localStorage',
    extraQueryParameter: '',
};

const authorizedTenantIds = [
    'bca200e7-1765-4001-977f-5363e5f7a63a', // carwash
    '72f988bf-86f1-41af-91ab-2d7cd011db47', // microsoft
    '42f7676c-f455-423c-82f6-dc2d99791af7', // sap
    '917332b6-5fee-4b92-9d05-812c7f08b9b9', // graphisoft
];

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

export function runWithAdal(app) {
    // it must run in iframe to for refreshToken (parsing hash and get token)
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
                }
                authContext.login();
            } else {
                const user = authContext.getCachedUser();
                console.log('User is authenticated.');
                // console.log(user);
                if (authorizedTenantIds.filter(id => id === user.profile.tid).length > 0) {
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
 * Parses the JSON returned by a network request
 * @param {object} response A response from a network request
 * @return {object} The parsed JSON, status from the response
 */
function parseJson(response) {
    // NoContent and Accepted would throw a JSON parsing error as they have no response body
    if (response.status === 204 || response.status === 202) {
        return {
            status: response.status,
            ok: response.ok,
            json: {},
        };
    }

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
        response.blob().then(blob => download(blob, filename, 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'));

        return {
            status: response.status,
            ok: response.ok,
            json: {},
        };
    }

    return new window.Promise((resolve, reject) =>
        response
            .json()
            .then(json =>
                resolve({
                    status: response.status,
                    ok: response.ok,
                    json,
                })
            )
            .catch(error => reject(error.message))
    );
}

/**
 * Requests a URL, returning a promise
 * @param {string} url The URL we want to request
 * @param {object} options The options we want to pass to "fetch"
 * @param {bool} errorIfOffline Indicates whether the function should reject the Promise if offline (default: false)
 * @return {Promise} The request promise
 */
export default function apiFetch(url, options, errorIfOffline = false) {
    if (errorIfOffline && !navigator.onLine) {
        return new window.Promise((_resolve, reject) => reject('You are offline.'));
    }

    return adalGetToken(adalConfig.endpoints.api).then(token => {
        const o = options || {};
        if (!(o.headers instanceof Headers)) o.headers = new Headers();
        o.headers.append('Content-Type', 'application/json');
        o.headers.append('Authorization', `Bearer ${token}`);

        return new window.Promise((resolve, reject) => {
            window
                .fetch(url, o)
                .catch(error => {
                    if (!navigator.onLine) {
                        console.error('NETWORK ERROR: Disconnected from network.');
                        return reject('You are offline.');
                    }
                    console.error(`NETWORK ERROR: ${error.message}`);
                    return reject('Network error. Are you offline?');
                })
                .then(parseJson)
                .then(
                    response => {
                        if (response.ok) {
                            return resolve(response.json);
                        }
                        // extract the error from the server's json
                        switch (response.status) {
                            case 400:
                                console.error(`BAD REQUEST: ${response.json}`);
                                return reject(`An error has occured: ${response.json}`);
                            case 401:
                                console.error(`UNAUTHORIZED: ${response.json}`);
                                return reject('You are not authorized. Please refresh the page!');
                            case 403:
                                console.error(`UNAUTHORIZED: ${response.json}`);
                                return reject("You don't have permission!");
                            case 404:
                                console.error(`NOT FOUND: ${response.json}`);
                                return reject('Not found.');
                            case 500:
                                console.error(`SERVER ERROR: ${response.json}`);
                                return reject('A server error has occured.');
                            default:
                                console.error(`UNKNOWN ERROR: ${response.json}`);
                                return reject(`An error has occured. ${response.json}`);
                        }
                    },
                    error => {
                        console.error(`JSON PARSING ERROR: ${error}`);
                        return reject('A server error has occured.');
                    }
                )
                .catch(error => {
                    console.error(`NETWORK ERROR: ${error.message}`);
                    return reject('Network error. Are you offline?');
                });
        });
    });
}

export const getToken = () => authContext.getCachedToken(authContext.config.clientId);
