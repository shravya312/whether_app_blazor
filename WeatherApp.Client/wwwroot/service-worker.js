const CACHE_NAME = 'weather-app-v1';
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

// Background sync for offline weather updates
self.addEventListener('sync', (event) => {
  if (event.tag === 'background-weather-sync') {
    event.waitUntil(syncWeatherData());
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

