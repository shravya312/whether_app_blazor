// Web Push API Integration for Background Notifications

window.PushNotification = {
    // Request notification permission
    requestPermission: async function() {
        if (!('Notification' in window)) {
            console.log('This browser does not support notifications');
            return 'denied';
        }

        const permission = await Notification.requestPermission();
        console.log('Notification permission:', permission);
        return permission;
    },

    // Subscribe to push notifications
    subscribe: async function(userId) {
        try {
            // Check if service worker is supported
            if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
                console.log('Push messaging is not supported');
                return null;
            }

            // Register service worker if not already registered
            let registration = await navigator.serviceWorker.ready;
            if (!registration) {
                registration = await navigator.serviceWorker.register('/service-worker.js');
                await navigator.serviceWorker.ready;
            }

            // Check if already subscribed
            let subscription = await registration.pushManager.getSubscription();
            
            if (!subscription) {
                // Get VAPID public key from server
                const vapidPublicKey = await fetch('/api/push/vapid-public-key')
                    .then(res => res.text())
                    .catch(() => null);

                if (!vapidPublicKey) {
                    console.error('Failed to get VAPID public key');
                    return null;
                }

                // Convert VAPID key to Uint8Array
                const applicationServerKey = this.urlBase64ToUint8Array(vapidPublicKey);

                // Subscribe to push
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: applicationServerKey
                });
            }

            // Convert subscription to JSON
            const subscriptionJson = subscription.toJSON();
            
            return {
                endpoint: subscriptionJson.endpoint || subscription.endpoint,
                keys: {
                    p256dh: subscriptionJson.keys?.p256dh || this.arrayBufferToBase64(subscription.getKey('p256dh')),
                    auth: subscriptionJson.keys?.auth || this.arrayBufferToBase64(subscription.getKey('auth'))
                }
            };
        } catch (error) {
            console.error('Error subscribing to push:', error);
            return null;
        }
    },

    // Unsubscribe from push notifications
    unsubscribe: async function() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();
            
            if (subscription) {
                await subscription.unsubscribe();
                console.log('Unsubscribed from push notifications');
                return true;
            }
            return false;
        } catch (error) {
            console.error('Error unsubscribing from push:', error);
            return false;
        }
    },

    // Get current subscription
    getSubscription: async function() {
        try {
            const registration = await navigator.serviceWorker.ready;
            const subscription = await registration.pushManager.getSubscription();
            
            if (!subscription) {
                return null;
            }

            const subscriptionJson = subscription.toJSON();
            
            return {
                endpoint: subscriptionJson.endpoint || subscription.endpoint,
                keys: {
                    p256dh: subscriptionJson.keys?.p256dh || this.arrayBufferToBase64(subscription.getKey('p256dh')),
                    auth: subscriptionJson.keys?.auth || this.arrayBufferToBase64(subscription.getKey('auth'))
                }
            };
        } catch (error) {
            console.error('Error getting subscription:', error);
            return null;
        }
    },

    // Convert VAPID key from base64 URL to Uint8Array
    urlBase64ToUint8Array: function(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding)
            .replace(/\-/g, '+')
            .replace(/_/g, '/');

        const rawData = window.atob(base64);
        const outputArray = new Uint8Array(rawData.length);

        for (let i = 0; i < rawData.length; ++i) {
            outputArray[i] = rawData.charCodeAt(i);
        }
        return outputArray;
    },

    // Convert ArrayBuffer to base64
    arrayBufferToBase64: function(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return window.btoa(binary);
    }
};

