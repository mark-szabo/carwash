// This is the service worker with the Cache-first network

var CACHE = 'carwash-precache';
var precacheFiles = [
    /* Add an array of files to precache for your app */
    '/',
    'index.css',
    'bundle.js',
    'images/favicon-32x32.png',
    'images/favicon-16x16.png'
];

// Install stage sets up the cache-array to configure pre-cache content
self.addEventListener('install', function (evt) {
    console.log('The service worker is being installed.');
    evt.waitUntil(precache().then(function () {
        console.log('Skip waiting on install');
        return self.skipWaiting();
    }));
});


// Allow sw to control of current page
self.addEventListener('activate', function (event) {
    console.log('Claiming clients for current page');
    return self.clients.claim();
});

self.addEventListener('fetch', function (event) {
    console.log('The service worker is serving the asset: ' + event.request.url);

    // Handle only GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // This prevents some weird issue with Chrome DevTools and 'only-if-cached'
    // Fixes issue #385, also ref to:
    // - https://github.com/paulirish/caltrainschedule.io/issues/49
    // - https://bugs.chromium.org/p/chromium/issues/detail?id=823392
    if (event.request.cache === 'only-if-cached' && event.request.mode !== 'same-origin') {
        return;
    }

    // We pull files from the cache first thing so we can show them fast
    event.respondWith(
        caches.open(CACHE).then(function (cache) {
            return cache.match(event.request).then(function (matching) {
                return matching || fetch(event.request).catch(() => {
                     console.log('No internet connection found. App is running in offline mode.');
                }); // This is the fallback if it is not in the cache to go to the server and get it
            });
        })
    );
    
    // This is where we call the server to get the newest version of the file to use the next time we show view
    event.waitUntil(update(event.request));
});


function precache() {
    return caches.open(CACHE).then(function (cache) {
        return cache.addAll(precacheFiles);
    });
}

function update(request) {
    return caches.open(CACHE).then(function (cache) {
        return fetch(request).then(function (response) {
            return cache.put(request, response);
        });
    });
}
