import React from 'react';
import AuthenticationContext from 'adal-angular';

const adalConfig = {
    clientId: '6e291d40-2613-4a74-9af5-790eb496a828',
    endpoints: {
        api: 'aa1032d5-839c-4f0f-824a-6f8cf44f34ad',
    },
    cacheLocation: 'localStorage',
};

const authorizedTenantIds = [
    'bca200e7-1765-4001-977f-5363e5f7a63a', //carwash
    '72f988bf-86f1-41af-91ab-2d7cd011db47', //microsoft
    '', //sap
    '' //graphisoft
];

//https://github.com/salvoravida/react-adal

const authContext = new AuthenticationContext(adalConfig);

function adalGetToken(authContext, resourceGuiId) {
    return new Promise((resolve, reject) => {
        authContext.acquireToken(resourceGuiId, (message, token, msg) => {
            if (!msg) resolve(token);
            else reject({ message, msg });
        });
    });
}

export function runWithAdal(app) {
    //it must run in iframe to for refreshToken (parsing hash and get token)
    authContext.handleWindowCallback();

    //prevent iframe double app !!!
    if (window === window.parent) {
        if (!authContext.isCallback(window.location.hash)) {
            if (!authContext.getCachedToken(authContext.config.clientId) || !authContext.getCachedUser()) {
                authContext.login();
            } else {
                const user = authContext.getCachedUser();
                console.log(user);
                if (authorizedTenantIds.filter(id => id === user.profile.tid).length > 0) {
                    app();
                } else {
                    console.log(`Tenant ${user.profile.tid} is not athorized to use this application!`);
                }
            }
        }
    }
}

function adalFetch(authContext, resourceGuiId, fetch, url, options) {
    return adalGetToken(authContext, resourceGuiId).then((token) => {
        const o = options || {};
        if (!o.headers) o.headers = {};
        o.headers.Authorization = `Bearer ${token}`;
        return fetch(url, o);
    });
}

const withAdalLogin = (authContext, resourceId) => {
    return function (WrappedComponent, renderLoading, renderError) {
        return class extends React.Component {
            constructor(props) {
                super(props);
                this.state = {
                    logged: false,
                    error: null,
                };
            }

            componentWillMount = () => {
                adalGetToken(authContext, resourceId)
                    .then(() => this.setState({ logged: true }))
                    .catch((error) => {
                        const { msg } = error;
                        console.log(error);
                        if (msg === 'login required') {
                            authContext.login();
                        } else {
                            this.setState({ error });
                        }
                    });
            };

            render() {
                if (this.state.logged) return <WrappedComponent {...this.props} />;
                if (this.state.error) return typeof renderError === 'function' ? renderError(this.state.error) : null;
                return typeof renderLoading === 'function' ? renderLoading() : null;
            }
        };
    };
};

export const getToken = () => {
    return authContext.getCachedToken(authContext.config.clientId);
};

export const adalApiFetch = (fetch, url, options) =>
    adalFetch(authContext, adalConfig.endpoints.api, fetch, url, options);

export const withAdalLoginApi = withAdalLogin(authContext, adalConfig.endpoints.api);