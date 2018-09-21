importScripts('https://storage.googleapis.com/workbox-cdn/releases/3.4.1/workbox-sw.js');

if (workbox) {
    console.log('Yay! Workbox is loaded 🎉');
} else {
    console.log("Boo! Workbox didn't load 😬");
}

workbox.core.setCacheNameDetails({
    prefix: 'carwash',
    suffix: 'v2',
    precache: 'precache',
    runtime: 'runtime',
});

// Don't forget to increase the revision number of index.html (aka. '/')
// as it is needed to include the newly genereted js and css files.
// Error would be thrown: Refused to execute script from '...' because its MIME type ('text/html') is not executable, and strict MIME type checking is enabled.
workbox.precaching.precacheAndRoute([
    { url: '/', revision: '2021' },
    { url: 'manifest.json', revision: '1' },
    { url: 'images/favicon-32x32.png', revision: '2' },
    { url: 'images/favicon-16x16.png', revision: '2' },
    { url: 'images/state0.png', revision: '2' },
    { url: 'images/state1.png', revision: '2' },
    { url: 'images/state2.png', revision: '2' },
    { url: 'images/state3.png', revision: '2' },
    { url: 'images/state4.png', revision: '2' },
    { url: 'images/state5.png', revision: '2' },
]);

const bgSyncPlugin = new workbox.backgroundSync.Plugin('bgSyncQueue');

// [NETWORK FIRST] Cache reservation list from 'GET /api/reservations'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations',
    workbox.strategies.networkFirst({
        cacheName: 'api-cache',
        plugins: [bgSyncPlugin],
    })
);

// [NETWORK FIRST] Cache company reservation list from 'GET /api/reservations/company'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations/company',
    workbox.strategies.networkFirst({
        cacheName: 'api-cache',
        plugins: [bgSyncPlugin],
    })
);

// [NETWORK FIRST] Cache backlog from 'GET /api/reservations/backlog'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations/backlog',
    workbox.strategies.networkFirst({
        cacheName: 'api-cache',
        plugins: [bgSyncPlugin],
    })
);

// [CACHE FIRST] Cache current user from 'GET /api/users/me'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/users/me',
    workbox.strategies.cacheFirst({
        cacheName: 'api-cache',
    })
);

// [CACHE FIRST] Cache current user from 'GET /api/reservations/lastsettings'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations/lastsettings',
    workbox.strategies.cacheFirst({
        cacheName: 'api-cache',
    })
);

// [CACHE FIRST] Cache Google Fonts
workbox.routing.registerRoute(
    new RegExp('https://fonts.(?:googleapis|gstatic).com/(.*)'),
    workbox.strategies.cacheFirst({
        cacheName: 'fonts-cache',
        plugins: [
            new workbox.expiration.Plugin({
                maxEntries: 30,
            }),
            new workbox.cacheableResponse.Plugin({
                statuses: [0, 200],
            }),
        ],
    })
);

// [CACHE FIRST] Cache Application Insights script
workbox.routing.registerRoute(
    /https:\/\/(.*).msecnd.net\/(.*)/,
    workbox.strategies.cacheFirst({
        cacheName: 'static-cache',
        plugins: [
            new workbox.expiration.Plugin({
                maxEntries: 30,
                maxAgeSeconds: 24 * 60 * 60, // 1 Day
            }),
            new workbox.cacheableResponse.Plugin({
                statuses: [0, 200],
            }),
        ],
    })
);

// [STALE WHILE REVALIDATE] Cache CSS and JS files
workbox.routing.registerRoute(
    /\.(?:js|css)$/,
    workbox.strategies.staleWhileRevalidate({
        cacheName: 'static-cache',
    })
);

// [CACHE FIRST] Cache image files
workbox.routing.registerRoute(
    /.*\.(?:png|jpg|jpeg|svg|gif)/,
    workbox.strategies.cacheFirst({
        cacheName: 'image-cache',
        plugins: [
            new workbox.expiration.Plugin({
                maxEntries: 60,
                maxAgeSeconds: 30 * 24 * 60 * 60, // 30 Days
            }),
        ],
    })
);

workbox.skipWaiting();
workbox.clientsClaim();

// Respond to a server push with a user notification
self.addEventListener('push', event => {
    if ('Notification' in window && Notification.permission === 'granted') {
        if (event.data) {
            const { title, lang = 'en', body, tag, timestamp, requireInteraction, actions, image } = event.data.json();

            const promiseChain = self.registration.showNotification(title, {
                lang,
                body,
                requireInteraction,
                tag: tag || undefined,
                timestamp: timestamp ? Date.parse(timestamp) : undefined,
                actions: actions || undefined,
                image: image || undefined,
                badge: '/images/notification72.png',
                icon: '/images/notificationicon512.png',
            });
            // Ensure the toast notification is displayed before exiting this function
            event.waitUntil(promiseChain);
        }
    }
});

// Respond to the user clicking the toast notification
self.addEventListener('notificationclick', event => {
    console.log('On notification click: ', event.notification.tag);
    event.notification.close();

    // This looks to see if the current is already open and focuses it
    event.waitUntil(
        clients
            .matchAll({
                type: 'window',
            })
            .then(clientList => {
                for (let i = 0; i < clientList.length; i++) {
                    const client = clientList[i];
                    if (client.url === 'https://carwashu.azurewebsites.net/' && 'focus' in client) return client.focus();
                }
                if (clients.openWindow) return clients.openWindow('/');
                return null;
            })
    );
});
