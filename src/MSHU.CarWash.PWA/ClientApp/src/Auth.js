import AuthenticationContext from 'adal-angular';

const adalConfig = {
    clientId: '6e291d40-2613-4a74-9af5-790eb496a828',
    endpoints: {
        api: '6e291d40-2613-4a74-9af5-790eb496a828',
        graph: 'https://graph.microsoft.com',
    },
    cacheLocation: 'localStorage',
    extraQueryParameter: '',
};

const authorizedTenantIds = [
    'bca200e7-1765-4001-977f-5363e5f7a63a', // carwash
    '72f988bf-86f1-41af-91ab-2d7cd011db47', // microsoft
    '', // sap
    '', // graphisoft
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
                    adalConfig.extraQueryParameter = `login_hint=${encodeURIComponent(user.profile.upn)}`;
                    authContext = new AuthenticationContext(adalConfig);
                    console(`User was prevously logged in with ${user.profile.upn}. Using this email as login hint.`);
                }
                authContext.login();
            } else {
                const user = authContext.getCachedUser();
                console.log('User is authenticated.');
                // console.log(user);
                if (authorizedTenantIds.filter(id => id === user.profile.tid).length > 0) {
                    console.log('Getting Graph auth token...');
                    if (!authContext.getCachedToken(adalConfig.endpoints.graph)) {
                        console.log('Graph toke was not found in cache. Redirecting...');
                        authContext.acquireTokenRedirect(adalConfig.endpoints.graph, null, null);
                    } else {
                        console.log('Graph token was found in cache. Authenticated.');
                        app();
                    }
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
    // NoContent would throw a JSON parsing error
    if (response.status === 204) {
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
 * @return {Promise} The request promise
 */
export default function apiFetch(url, options) {
    return adalGetToken(adalConfig.endpoints.api).then(token =>
        adalGetToken(adalConfig.endpoints.graph).then(graphToken => {
            const o = options || {};
            if (!(o.headers instanceof Headers)) o.headers = new Headers();
            o.headers.append('Content-Type', 'application/json');
            o.headers.append('Authorization', `Bearer ${token}`);
            o.headers.append('X-Graph-Token', graphToken);

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
        })
    );
}

export const getToken = () => authContext.getCachedToken(authContext.config.clientId);
