importScripts('https://storage.googleapis.com/workbox-cdn/releases/3.4.1/workbox-sw.js');

if (workbox) {
    console.log('Yay! Workbox is loaded 🎉');
} else {
    console.log("Boo! Workbox didn't load 😬");
}

workbox.precaching.precacheAndRoute([
    { url: '/', revision: '1' },
    { url: 'manifest.json', revision: '1' },
    { url: 'images/favicon-32x32.png', revision: '1' },
    { url: 'images/favicon-16x16.png', revision: '1' },
    { url: 'images/state0.png', revision: '1' },
    { url: 'images/state1.png', revision: '1' },
    { url: 'images/state2.png', revision: '1' },
    { url: 'images/state3.png', revision: '1' },
    { url: 'images/state4.png', revision: '1' },
    { url: 'images/state5.png', revision: '1' },
]);

const bgSyncPlugin = new workbox.backgroundSync.Plugin('bgSyncQueue');

// [NETWORK FIRST] Cache reservation list from 'GET /api/reservations'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations',
    workbox.strategies.networkFirst({
        plugins: [bgSyncPlugin],
    })
);

// [NETWORK FIRST] Cache company reservation list from 'GET /api/reservations/company'
workbox.routing.registerRoute(
    ({ url }) => url.pathname === '/api/reservations/company',
    workbox.strategies.networkFirst({
        plugins: [bgSyncPlugin],
    })
);

// [CACHE FIRST] Cache current user from 'GET /api/users/me'
workbox.routing.registerRoute(({ url }) => url.pathname === '/api/users/me', workbox.strategies.cacheFirst());

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
