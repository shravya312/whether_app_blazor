const CACHE_NAME = 'weather-app-v2';
const urlsToCache = [
  '/',
  '/css/app.css',
  '/css/bootstrap/bootstrap.min.css',
  '/js/geolocation.js',
  '/js/theme.js',
  '/manifest.json',
  '/favicon.png',
  '/icon-192.png'
];

// Install event - cache resources
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('Opened cache');
        return cache.addAll(urlsToCache);
      })
  );
});

// Fetch event - serve from cache, fallback to network
self.addEventListener('fetch', (event) => {
  event.respondWith(
    caches.match(event.request)
      .then((response) => {
        // Return cached version or fetch from network
        return response || fetch(event.request);
      })
  );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames.map((cacheName) => {
          if (cacheName !== CACHE_NAME) {
            console.log('Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
});

// Background sync for offline weather updates and email queue
self.addEventListener('sync', (event) => {
  if (event.tag === 'background-weather-sync') {
    event.waitUntil(syncWeatherData());
  } else if (event.tag === 'email-sync') {
    event.waitUntil(syncEmailQueue());
  }
});

async function syncWeatherData() {
  try {
    console.log('Syncing weather data...');
    
    // Get cached favorite cities from IndexedDB or localStorage
    const cache = await caches.open(CACHE_NAME);
    
    // Sync favorite cities weather data
    const favoriteCities = await getCachedFavoriteCities();
    
    if (favoriteCities && favoriteCities.length > 0) {
      for (const city of favoriteCities) {
        try {
          // Fetch fresh weather data for each favorite city
          const weatherUrl = `/api/weather/${encodeURIComponent(city.City)}?country=${encodeURIComponent(city.Country || '')}`;
          const response = await fetch(weatherUrl);
          
          if (response.ok) {
            // Cache the response
            await cache.put(weatherUrl, response.clone());
            console.log(`Synced weather for ${city.City}`);
          }
        } catch (error) {
          console.error(`Error syncing ${city.City}:`, error);
        }
      }
    }
    
    console.log('Weather data sync completed');
  } catch (error) {
    console.error('Error syncing weather data:', error);
  }
}

async function getCachedFavoriteCities() {
  try {
    // Try to get from IndexedDB or localStorage via postMessage
    return new Promise((resolve) => {
      const channel = new BroadcastChannel('weather-sync');
      channel.postMessage({ type: 'getFavoriteCities' });
      
      channel.onmessage = (event) => {
        if (event.data.type === 'favoriteCities') {
          resolve(event.data.cities);
        }
      };
      
      // Timeout after 1 second
      setTimeout(() => resolve([]), 1000);
    });
  } catch (error) {
    console.error('Error getting cached favorite cities:', error);
    return [];
  }
}

// Periodic background sync - check weather every hour
self.addEventListener('periodicsync', (event) => {
  if (event.tag === 'weather-update') {
    event.waitUntil(syncWeatherData());
  }
});

// Push event - handle push notifications (works even when app is closed)
self.addEventListener('push', (event) => {
  console.log('Push notification received:', event);
  
  let notificationData = {
    title: 'Weather Alert',
    body: 'You have a new weather alert',
    icon: '/favicon.ico',
    badge: '/icon-192.png',
    tag: 'weather-alert',
    requireInteraction: true,
    data: {}
  };

  // Parse push data if available
  if (event.data) {
    try {
      const data = event.data.json();
      notificationData = {
        title: data.title || 'Weather Alert',
        body: data.body || data.message || 'You have a new weather alert',
        icon: data.icon || '/favicon.ico',
        badge: '/icon-192.png',
        tag: data.tag || data.id || 'weather-alert',
        requireInteraction: true,
        data: data.data || {},
        actions: data.actions || []
      };
    } catch (e) {
      // If not JSON, try text
      const text = event.data.text();
      if (text) {
        notificationData.body = text;
      }
    }
  }

  event.waitUntil(
    self.registration.showNotification(notificationData.title, {
      body: notificationData.body,
      icon: notificationData.icon,
      badge: notificationData.badge,
      tag: notificationData.tag,
      requireInteraction: notificationData.requireInteraction,
      data: notificationData.data,
      actions: notificationData.actions,
      vibrate: [200, 100, 200],
      timestamp: Date.now()
    })
  );
});

// Notification click event - handle when user clicks notification
self.addEventListener('notificationclick', (event) => {
  console.log('Notification clicked:', event);
  
  event.notification.close();

  // Get notification data
  const notificationData = event.notification.data || {};
  const urlToOpen = notificationData.url || '/';

  event.waitUntil(
    clients.matchAll({
      type: 'window',
      includeUncontrolled: true
    }).then((clientList) => {
      // Check if there's already a window open
      for (let i = 0; i < clientList.length; i++) {
        const client = clientList[i];
        if (client.url === urlToOpen && 'focus' in client) {
          return client.focus();
        }
      }
      
      // If no window is open, open a new one
      if (clients.openWindow) {
        return clients.openWindow(urlToOpen);
      }
    })
  );
});

// Notification close event
self.addEventListener('notificationclose', (event) => {
  console.log('Notification closed:', event);
});

// Email queue sync function - Continuously attempts to send queued emails
// This will automatically succeed when connection is restored
async function syncEmailQueue() {
  try {
    console.log('🔄 Email sync triggered - Processing email queue...');
    
    // Get API base URL from environment or use default
    // Try to extract from registration scope, fallback to common ports
    let apiBaseUrl = 'http://localhost:5009';
    try {
      const scope = self.registration.scope;
      // Extract base URL and replace port
      const url = new URL(scope);
      apiBaseUrl = `${url.protocol}//${url.hostname}:5009`;
    } catch (e) {
      console.log('Using default API URL:', apiBaseUrl);
    }
    
    // Open IndexedDB
    const dbName = 'WeatherAppEmailQueue';
    const dbVersion = 1;
    const storeName = 'emails';
    
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(dbName, dbVersion);
      
      request.onsuccess = async () => {
        const db = request.result;
        
        try {
          // Get all pending emails (including failed ones that can be retried)
          const transaction = db.transaction([storeName], 'readonly');
          const store = transaction.objectStore(storeName);
          const index = store.index('status');
          
          // Get pending emails
          const pendingRequest = index.getAll('pending');
          
          pendingRequest.onsuccess = async () => {
            const pendingEmails = pendingRequest.result || [];
            
            // Also get failed emails that haven't exceeded retry limit
            const failedRequest = index.getAll('failed');
            
            failedRequest.onsuccess = async () => {
              const failedEmails = (failedRequest.result || []).filter(e => 
                (e.retryCount || 0) < 5
              );
              
              const allEmails = [...pendingEmails, ...failedEmails];
              console.log(`📧 Found ${allEmails.length} emails to process (${pendingEmails.length} pending, ${failedEmails.length} retryable)`);
              
              if (allEmails.length === 0) {
                console.log('✅ No emails to process');
                resolve();
                return;
              }
              
              let sent = 0;
              let failed = 0;
              let queued = 0;
              
              for (const email of allEmails) {
                try {
                  // Update status to sending
                  const updateTransaction = db.transaction([storeName], 'readwrite');
                  const updateStore = updateTransaction.objectStore(storeName);
                  email.status = 'sending';
                  email.retryCount = (email.retryCount || 0) + 1;
                  email.lastAttempt = Date.now();
                  
                  await new Promise((resolveUpdate) => {
                    const updateRequest = updateStore.put(email);
                    updateRequest.onsuccess = () => resolveUpdate();
                    updateRequest.onerror = () => resolveUpdate();
                  });
                  
                  console.log(`📤 Attempting to send email ${email.id} to ${email.toEmail} (attempt ${email.retryCount})...`);
                  
                  // Attempt to send email via API
                  // This will fail if offline, but Background Sync will retry automatically
                  const response = await fetch(`${apiBaseUrl}/api/notifications/email/weather-alert`, {
                    method: 'POST',
                    headers: {
                      'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                      ToEmail: email.toEmail,
                      City: email.city,
                      Country: email.country,
                      AlertMessage: email.alertMessage,
                      AlertType: email.alertType
                    })
                  });
                  
                  if (response.ok) {
                    // Email sent successfully - delete from queue
                    const deleteTransaction = db.transaction([storeName], 'readwrite');
                    const deleteStore = deleteTransaction.objectStore(storeName);
                    await new Promise((resolveDelete) => {
                      const deleteRequest = deleteStore.delete(email.id);
                      deleteRequest.onsuccess = () => resolveDelete();
                      deleteRequest.onerror = () => resolveDelete();
                    });
                    sent++;
                    console.log(`✅ Email ${email.id} sent successfully to ${email.toEmail}`);
                  } else {
                    // Failed to send - will retry on next sync
                    const errorText = await response.text();
                    const failTransaction = db.transaction([storeName], 'readwrite');
                    const failStore = failTransaction.objectStore(storeName);
                    
                    if (email.retryCount >= 5) {
                      email.status = 'failed';
                      email.lastError = `Max retries exceeded: ${errorText}`;
                      failed++;
                      console.error(`❌ Email ${email.id} failed permanently after ${email.retryCount} attempts`);
                    } else {
                      // Reset to pending for retry
                      email.status = 'pending';
                      email.lastError = errorText;
                      queued++;
                      console.log(`⏳ Email ${email.id} queued for retry (attempt ${email.retryCount}/5): ${errorText.substring(0, 50)}`);
                    }
                    
                    await new Promise((resolveFail) => {
                      const failRequest = failStore.put(email);
                      failRequest.onsuccess = () => resolveFail();
                      failRequest.onerror = () => resolveFail();
                    });
                  }
                } catch (error) {
                  // Network error or other exception - will retry on next sync
                  console.error(`⚠️ Error processing email ${email.id}:`, error.message);
                  
                  const errorTransaction = db.transaction([storeName], 'readwrite');
                  const errorStore = errorTransaction.objectStore(storeName);
                  
                  if (email.retryCount >= 5) {
                    email.status = 'failed';
                    email.lastError = `Max retries exceeded: ${error.message}`;
                    failed++;
                  } else {
                    email.status = 'pending';
                    email.lastError = error.message;
                    queued++;
                  }
                  
                  await new Promise((resolveError) => {
                    const errorRequest = errorStore.put(email);
                    errorRequest.onsuccess = () => resolveError();
                    errorRequest.onerror = () => resolveError();
                  });
                }
              }
              
              console.log(`📊 Email sync completed: ${sent} sent ✅, ${queued} queued for retry ⏳, ${failed} failed permanently ❌`);
              
              // If there are still pending emails, register another sync
              // This ensures continuous retry until all emails are sent
              if (queued > 0) {
                console.log(`🔄 Registering another sync for ${queued} queued emails...`);
                self.registration.sync.register('email-sync').catch(err => {
                  console.log('Could not register additional sync:', err);
                });
              }
              
              resolve();
            };
            
            failedRequest.onerror = () => {
              console.error('Failed to get failed emails:', failedRequest.error);
              resolve(); // Don't reject, just continue
            };
          };
          
          pendingRequest.onerror = () => {
            console.error('Failed to get pending emails:', pendingRequest.error);
            reject(pendingRequest.error);
          };
        } catch (error) {
          console.error('Error in email sync:', error);
          reject(error);
        }
      };
      
      request.onerror = () => {
        console.error('Failed to open email queue database:', request.error);
        reject(request.error);
      };
      
      request.onupgradeneeded = (event) => {
        const database = event.target.result;
        if (!database.objectStoreNames.contains(storeName)) {
          const objectStore = database.createObjectStore(storeName, {
            keyPath: 'id',
            autoIncrement: true
          });
          objectStore.createIndex('timestamp', 'timestamp', { unique: false });
          objectStore.createIndex('status', 'status', { unique: false });
          objectStore.createIndex('toEmail', 'toEmail', { unique: false });
        }
      };
    });
  } catch (error) {
    console.error('❌ Error syncing email queue:', error);
    // Don't reject - allow Background Sync to retry
    throw error; // Re-throw so Background Sync can retry
  }
}

