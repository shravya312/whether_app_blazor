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
    subscribe: async function(userId, apiBaseUrl) {
        try {
            // Check if service worker is supported
            if (!('serviceWorker' in navigator)) {
                throw new Error('Service Worker is not supported in this browser');
            }
            
            if (!('PushManager' in window)) {
                throw new Error('Push Manager is not supported in this browser');
            }

            console.log('Service Worker and Push Manager are supported');

            // Register service worker if not already registered
            let registration = null;
            try {
                registration = await navigator.serviceWorker.ready;
                console.log('Service worker already ready');
            } catch (e) {
                console.log('Service worker not ready, registering...', e);
            }

            if (!registration) {
                console.log('Registering service worker...');
                registration = await navigator.serviceWorker.register('/service-worker.js', { scope: '/' });
                console.log('Service worker registered, waiting for ready...');
                // Wait for service worker to be ready
                registration = await navigator.serviceWorker.ready;
                console.log('Service worker is now ready');
            }

            // Check if already subscribed
            let subscription = await registration.pushManager.getSubscription();
            
            if (!subscription) {
                console.log('Not subscribed yet, getting VAPID key...');
                // Get VAPID public key from server
                const apiUrl = apiBaseUrl || 'http://localhost:5009';
                console.log(`Fetching VAPID key from: ${apiUrl}/api/PushNotification/vapid-public-key`);
                
                const vapidResponse = await fetch(`${apiUrl}/api/PushNotification/vapid-public-key`);
                
                if (!vapidResponse.ok) {
                    const errorText = await vapidResponse.text();
                    throw new Error(`Failed to get VAPID key: ${vapidResponse.status} - ${errorText}`);
                }
                
                let vapidPublicKey = await vapidResponse.text();
                console.log('VAPID key received (raw):', vapidPublicKey);
                console.log('VAPID key length:', vapidPublicKey.length);
                console.log('VAPID key char codes:', Array.from(vapidPublicKey.substring(0, 20)).map(c => c.charCodeAt(0)));
                
                // Clean the key - remove any whitespace, newlines, and non-printable characters
                vapidPublicKey = vapidPublicKey
                    .trim()
                    .replace(/\s/g, '')  // Remove all whitespace
                    .replace(/\n/g, '')   // Remove newlines
                    .replace(/\r/g, '')  // Remove carriage returns
                    .replace(/[^\x20-\x7E]/g, ''); // Remove any non-printable ASCII characters
                
                console.log('VAPID key cleaned:', vapidPublicKey.substring(0, 30) + '...');
                console.log('VAPID key cleaned length:', vapidPublicKey.length);
                console.log('VAPID key contains only valid chars:', /^[A-Za-z0-9\-_]+$/.test(vapidPublicKey));

                if (!vapidPublicKey || vapidPublicKey.length === 0) {
                    throw new Error('VAPID public key is empty after cleaning');
                }

                // Convert VAPID key to Uint8Array
                console.log('Converting VAPID key to Uint8Array...');
                const applicationServerKey = this.urlBase64ToUint8Array(vapidPublicKey);

                // Subscribe to push
                console.log('Subscribing to push notifications...');
                subscription = await registration.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: applicationServerKey
                });
                console.log('Successfully subscribed to push notifications');
            } else {
                console.log('Already subscribed to push notifications');
            }

            // Convert subscription to JSON
            const subscriptionJson = subscription.toJSON();
            
            const result = {
                endpoint: subscriptionJson.endpoint || subscription.endpoint,
                keys: {
                    p256dh: subscriptionJson.keys?.p256dh || this.arrayBufferToBase64(subscription.getKey('p256dh')),
                    auth: subscriptionJson.keys?.auth || this.arrayBufferToBase64(subscription.getKey('auth'))
                }
            };
            
            console.log('Subscription result prepared:', result.endpoint);
            return result;
        } catch (error) {
            console.error('Error subscribing to push:', error);
            console.error('Error details:', {
                message: error.message,
                stack: error.stack,
                name: error.name
            });
            // Return error details so C# can display them
            throw error;
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
        try {
            if (!base64String || typeof base64String !== 'string') {
                throw new Error('VAPID key is not a valid string');
            }
            
            console.log('Starting VAPID key conversion, input length:', base64String.length);
            
            // Step 1: Clean the string - remove ALL non-base64 characters
            let cleaned = base64String
                .trim()
                .replace(/[\s\n\r\t]/g, '') // Remove all whitespace
                .replace(/[^A-Za-z0-9\-_]/g, ''); // Keep only base64 URL-safe characters
            
            if (cleaned.length === 0) {
                throw new Error('VAPID key is empty after cleaning');
            }
            
            console.log('After cleaning - length:', cleaned.length, 'first 30:', cleaned.substring(0, 30));
            
            // Step 2: Convert URL-safe base64 to standard base64
            // URL-safe: - becomes +, _ becomes /
            let base64 = cleaned
                .replace(/\-/g, '+')
                .replace(/_/g, '/');
            
            console.log('After URL-safe conversion - length:', base64.length, 'first 30:', base64.substring(0, 30));
            
            // Step 3: Add padding (base64 must be multiple of 4)
            const remainder = base64.length % 4;
            if (remainder !== 0) {
                const paddingNeeded = 4 - remainder;
                base64 += '='.repeat(paddingNeeded);
                console.log('Added', paddingNeeded, 'padding characters');
            }
            
            console.log('Final base64 length:', base64.length);
            
            // Step 4: Validate - must only contain valid base64 characters
            const base64Regex = /^[A-Za-z0-9+/=]+$/;
            if (!base64Regex.test(base64)) {
                const invalid = base64.match(/[^A-Za-z0-9+/=]/g);
                console.error('Invalid characters after conversion:', invalid);
                throw new Error(`Invalid base64 characters found: ${invalid?.join(', ')}`);
            }
            
            console.log('Validation passed, attempting atob decode...');
            
            // Step 5: Decode using atob
            let rawData;
            try {
                rawData = window.atob(base64);
                console.log('atob decode successful, binary length:', rawData.length);
            } catch (atobError) {
                console.error('atob failed:', atobError);
                console.error('Base64 string that failed:', base64.substring(0, 50));
                // Try alternative: decode each character manually
                throw new Error(`Base64 decode failed: ${atobError.message}. Key may be corrupted.`);
            }
            
            // Step 6: Convert to Uint8Array
            const outputArray = new Uint8Array(rawData.length);
            for (let i = 0; i < rawData.length; i++) {
                const code = rawData.charCodeAt(i);
                if (code > 255) {
                    throw new Error(`Invalid character code ${code} at position ${i}`);
                }
                outputArray[i] = code;
            }
            
            console.log('Successfully converted to Uint8Array, length:', outputArray.length);
            return outputArray;
        } catch (error) {
            console.error('=== VAPID KEY CONVERSION ERROR ===');
            console.error('Error type:', error.name);
            console.error('Error message:', error.message);
            console.error('Input type:', typeof base64String);
            console.error('Input length:', base64String?.length);
            console.error('Input preview:', base64String?.substring(0, 50));
            if (base64String) {
                const charCodes = Array.from(base64String.substring(0, 50)).map(c => `${c}(${c.charCodeAt(0)})`);
                console.error('First 50 char codes:', charCodes.join(' '));
            }
            throw error;
        }
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

