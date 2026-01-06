// Background sync and periodic updates for weather data

// Register periodic background sync (if supported)
if ('serviceWorker' in navigator && 'periodicSync' in window.ServiceWorkerRegistration.prototype) {
  navigator.serviceWorker.ready.then(async (registration) => {
    try {
      // Request periodic sync permission
      const status = await navigator.permissions.query({ name: 'periodic-background-sync' });
      
      if (status.state === 'granted') {
        // Register periodic sync for weather updates (every hour)
        await registration.periodicSync.register('weather-update', {
          minInterval: 3600000 // 1 hour in milliseconds
        });
        console.log('Periodic background sync registered');
      }
    } catch (error) {
      console.log('Periodic sync not supported:', error);
    }
  });
}

// Register background sync for offline weather updates
if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
  navigator.serviceWorker.ready.then(async (registration) => {
    // Register background sync
    try {
      await registration.sync.register('background-weather-sync');
      console.log('Background sync registered');
    } catch (error) {
      console.log('Background sync registration failed:', error);
    }
  });
}

// Broadcast channel for communication with service worker
const syncChannel = new BroadcastChannel('weather-sync');

// Listen for requests from service worker
syncChannel.onmessage = async (event) => {
  if (event.data.type === 'getFavoriteCities') {
    try {
      // Get favorite cities from localStorage
      const userId = localStorage.getItem('currentUserId');
      if (userId) {
        const favoritesKey = `favorite_cities_${userId}`;
        const favoritesJson = localStorage.getItem(favoritesKey);
        
        if (favoritesJson) {
          const favorites = JSON.parse(favoritesJson);
          syncChannel.postMessage({
            type: 'favoriteCities',
            cities: favorites
          });
        }
      }
    } catch (error) {
      console.error('Error getting favorite cities:', error);
      syncChannel.postMessage({ type: 'favoriteCities', cities: [] });
    }
  }
};

// Function to trigger manual sync
window.triggerWeatherSync = async function() {
  if ('serviceWorker' in navigator) {
    const registration = await navigator.serviceWorker.ready;
    if (registration.sync) {
      await registration.sync.register('background-weather-sync');
      console.log('Manual weather sync triggered');
    }
  }
};

// Function to trigger email queue sync
window.triggerEmailSync = async function() {
  if ('serviceWorker' in navigator) {
    const registration = await navigator.serviceWorker.ready;
    if (registration.sync) {
      await registration.sync.register('email-sync');
      console.log('Manual email sync triggered');
    }
  }
};

// Auto-register email sync when online
if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
  window.addEventListener('online', async () => {
    console.log('Connection restored - triggering email sync...');
    try {
      await window.triggerEmailSync();
    } catch (error) {
      console.log('Email sync trigger failed:', error);
    }
  });
}

