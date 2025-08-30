importScripts('https://storage.googleapis.com/workbox-cdn/releases/7.3.0/workbox-sw.js');

if (workbox) {
    console.log('Yay! Workbox is loaded 🎉');
} else {
    console.log("Boo! Workbox didn't load 😬");
}

const { NetworkFirst, StaleWhileRevalidate, CacheFirst } = workbox.strategies;
const { BackgroundSyncPlugin } = workbox.backgroundSync;
const { ExpirationPlugin } = workbox.expiration;
const { CacheableResponsePlugin } = workbox.cacheableResponse;
const { BroadcastUpdatePlugin } = workbox.broadcastUpdate;

workbox.core.setCacheNameDetails({
    prefix: 'carwash',
    suffix: 'v3',
    precache: 'precache',
    runtime: 'runtime',
});

const VERSION = '2.6.0';
console.log(`Build: ${VERSION}`);
workbox.precaching.cleanupOutdatedCaches();
workbox.precaching.precacheAndRoute([
    { url: '/', revision: VERSION.replace(/\./g, '') },
    { url: 'manifest.json', revision: '2' },
    { url: 'images/favicon-32x32.png', revision: '2' },
    { url: 'images/favicon-16x16.png', revision: '2' },
    { url: 'images/state0.png', revision: '2' },
    { url: 'images/state1.png', revision: '2' },
    { url: 'images/state2.png', revision: '2' },
    { url: 'images/state3.png', revision: '2' },
    { url: 'images/state4.png', revision: '2' },
    { url: 'images/state5.png', revision: '2' },
    { url: 'api/.well-known/vapidpublickey', revision: '2' },
]);
