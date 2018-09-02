import apiFetch from './Auth';

// This function is needed because Chrome doesn't accept a base64 encoded string
// as value for applicationServerKey in pushManager.subscribe yet
// https://bugs.chromium.org/p/chromium/issues/detail?id=802280
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

export default function register() {
    // Subscribe this PWA to push notifications from the server
    navigator.serviceWorker.ready
        .then(registration =>
            // Check if the user has an existing subscription
            registration.pushManager.getSubscription().then(async subscription => {
                if (subscription) {
                    return subscription;
                }

                // Otherwise subscribe with the server public key
                const vapidPublicKey = await apiFetch('/api/push/vapidpublickey');
                const convertedVapidKey = urlBase64ToUint8Array(vapidPublicKey);

                return registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: convertedVapidKey,
                });
            })
        )
        .then(subscription => {
            // Send the subscription details to the server
            apiFetch('/api/push/register', {
                method: 'POST',
                headers: {
                    'Content-type': 'application/json',
                },
                body: JSON.stringify({
                    subscription,
                }),
            });
        });
}
