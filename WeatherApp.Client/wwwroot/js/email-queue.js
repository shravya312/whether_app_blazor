// Email Queue Manager for Offline Email Support
// Uses IndexedDB to store emails when offline and syncs when online

const EMAIL_DB_NAME = 'WeatherAppEmailQueue';
const EMAIL_DB_VERSION = 1;
const EMAIL_STORE_NAME = 'emails';

let db = null;

// Initialize IndexedDB
async function initEmailDB() {
  if (db) return db;
  
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(EMAIL_DB_NAME, EMAIL_DB_VERSION);
    
    request.onerror = () => {
      console.error('Failed to open email queue database:', request.error);
      reject(request.error);
    };
    
    request.onsuccess = () => {
      db = request.result;
      console.log('Email queue database opened');
      resolve(db);
    };
    
    request.onupgradeneeded = (event) => {
      const database = event.target.result;
      
      // Create object store if it doesn't exist
      if (!database.objectStoreNames.contains(EMAIL_STORE_NAME)) {
        const objectStore = database.createObjectStore(EMAIL_STORE_NAME, {
          keyPath: 'id',
          autoIncrement: true
        });
        
        // Create indexes for efficient querying
        objectStore.createIndex('timestamp', 'timestamp', { unique: false });
        objectStore.createIndex('status', 'status', { unique: false });
        objectStore.createIndex('toEmail', 'toEmail', { unique: false });
        
        console.log('Email queue object store created');
      }
    };
  });
}

// Add email to queue
async function queueEmail(emailData) {
  try {
    const database = await initEmailDB();
    
    const emailRecord = {
      toEmail: emailData.toEmail,
      city: emailData.city,
      country: emailData.country || '',
      alertMessage: emailData.alertMessage || '',
      alertType: emailData.alertType || 'Weather Alert',
      timestamp: Date.now(),
      status: 'pending', // pending, sending, sent, failed
      retryCount: 0,
      lastError: null,
      lastAttempt: null,
      createdAt: new Date().toISOString()
    };
    
    return new Promise((resolve, reject) => {
      const transaction = database.transaction([EMAIL_STORE_NAME], 'readwrite');
      const store = transaction.objectStore(EMAIL_STORE_NAME);
      const request = store.add(emailRecord);
      
      request.onsuccess = () => {
        console.log('Email queued successfully:', emailRecord);
        resolve(request.result);
      };
      
      request.onerror = () => {
        console.error('Failed to queue email:', request.error);
        reject(request.error);
      };
    });
  } catch (error) {
    console.error('Error queueing email:', error);
    throw error;
  }
}

// Get all pending emails
async function getPendingEmails() {
  try {
    const database = await initEmailDB();
    
    return new Promise((resolve, reject) => {
      const transaction = database.transaction([EMAIL_STORE_NAME], 'readonly');
      const store = transaction.objectStore(EMAIL_STORE_NAME);
      const index = store.index('status');
      const request = index.getAll('pending');
      
      request.onsuccess = () => {
        resolve(request.result || []);
      };
      
      request.onerror = () => {
        console.error('Failed to get pending emails:', request.error);
        reject(request.error);
      };
    });
  } catch (error) {
    console.error('Error getting pending emails:', error);
    return [];
  }
}

// Update email status
async function updateEmailStatus(id, status, error = null) {
  try {
    const database = await initEmailDB();
    
    return new Promise((resolve, reject) => {
      const transaction = database.transaction([EMAIL_STORE_NAME], 'readwrite');
      const store = transaction.objectStore(EMAIL_STORE_NAME);
      const getRequest = store.get(id);
      
      getRequest.onsuccess = () => {
        const email = getRequest.result;
        if (email) {
          email.status = status;
          email.lastError = error;
          if (status === 'sending') {
            email.retryCount = (email.retryCount || 0) + 1;
          }
          
          const updateRequest = store.put(email);
          updateRequest.onsuccess = () => {
            console.log(`Email ${id} status updated to ${status}`);
            resolve(email);
          };
          updateRequest.onerror = () => reject(updateRequest.error);
        } else {
          reject(new Error('Email not found'));
        }
      };
      
      getRequest.onerror = () => reject(getRequest.error);
    });
  } catch (error) {
    console.error('Error updating email status:', error);
    throw error;
  }
}

// Delete email from queue
async function deleteEmail(id) {
  try {
    const database = await initEmailDB();
    
    return new Promise((resolve, reject) => {
      const transaction = database.transaction([EMAIL_STORE_NAME], 'readwrite');
      const store = transaction.objectStore(EMAIL_STORE_NAME);
      const request = store.delete(id);
      
      request.onsuccess = () => {
        console.log(`Email ${id} deleted from queue`);
        resolve();
      };
      
      request.onerror = () => {
        console.error('Failed to delete email:', request.error);
        reject(request.error);
      };
    });
  } catch (error) {
    console.error('Error deleting email:', error);
    throw error;
  }
}

// Get count of pending emails
async function getPendingEmailCount() {
  try {
    const emails = await getPendingEmails();
    return emails.length;
  } catch (error) {
    console.error('Error getting pending email count:', error);
    return 0;
  }
}

// Check if online
function isOnline() {
  return navigator.onLine;
}

// Process email queue - send all pending emails
async function processEmailQueue(apiBaseUrl) {
  if (!isOnline()) {
    console.log('Offline - cannot process email queue');
    return { sent: 0, failed: 0 };
  }
  
  try {
    const pendingEmails = await getPendingEmails();
    console.log(`Processing ${pendingEmails.length} pending emails...`);
    
    let sent = 0;
    let failed = 0;
    
    for (const email of pendingEmails) {
      try {
        // Update status to sending
        await updateEmailStatus(email.id, 'sending');
        
        // Send email via API
        const apiUrl = apiBaseUrl || 'http://localhost:5009';
        const response = await fetch(`${apiUrl}/api/notifications/email/weather-alert`, {
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
          // Email sent successfully
          await deleteEmail(email.id);
          sent++;
          console.log(`Email sent successfully to ${email.toEmail}`);
        } else {
          // Failed to send
          const errorText = await response.text();
          await updateEmailStatus(email.id, 'failed', errorText);
          failed++;
          console.error(`Failed to send email to ${email.toEmail}:`, errorText);
          
          // If retry count exceeds 5, mark as failed permanently
          if (email.retryCount >= 5) {
            await updateEmailStatus(email.id, 'failed', 'Max retries exceeded');
          }
        }
      } catch (error) {
        console.error(`Error processing email ${email.id}:`, error);
        await updateEmailStatus(email.id, 'failed', error.message);
        failed++;
      }
    }
    
    console.log(`Email queue processed: ${sent} sent, ${failed} failed`);
    return { sent, failed };
  } catch (error) {
    console.error('Error processing email queue:', error);
    return { sent: 0, failed: 0 };
  }
}

// Export functions to window for C# interop
window.EmailQueue = {
  queueEmail,
  getPendingEmails,
  updateEmailStatus,
  deleteEmail,
  getPendingEmailCount,
  isOnline,
  processEmailQueue
};

// Auto-process queue when coming online
window.addEventListener('online', async () => {
  console.log('🌐 Connection restored - triggering email sync...');
  
  // Trigger service worker sync for email queue
  if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
    try {
      const registration = await navigator.serviceWorker.ready;
      await registration.sync.register('email-sync');
      console.log('✅ Email sync registered - emails will be sent automatically');
    } catch (error) {
      console.log('Could not register email sync, trying direct processing:', error);
      // Fallback to direct processing
      const apiBaseUrl = window.location.origin.replace(/:\d+$/, ':5009');
      await processEmailQueue(apiBaseUrl);
    }
  } else {
    // Fallback if service worker sync not available
    const apiBaseUrl = window.location.origin.replace(/:\d+$/, ':5009');
    await processEmailQueue(apiBaseUrl);
  }
});

// Log when going offline
window.addEventListener('offline', () => {
  console.log('⚠️ Connection lost - emails will be queued and sent automatically when online');
});

