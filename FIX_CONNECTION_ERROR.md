# Fix: ERR_CONNECTION_REFUSED Error

## ✅ Good News: API is Running!

The API is already running (process 5712). The connection error might be because:

1. **API just started** - Give it a few seconds to fully initialize
2. **Client needs refresh** - Refresh your browser
3. **Port mismatch** - Verify the ports match

## Quick Fix Steps

### Step 1: Verify API is Running
Open in browser: `https://localhost:7089/swagger`

You should see the Swagger UI. If you see it, the API is running! ✅

### Step 2: Test API Directly
Try these URLs in your browser:

1. **Popular Cities:**
   ```
   https://localhost:7089/api/popularcities?limit=5
   ```

2. **Weather (test with London):**
   ```
   https://localhost:7089/api/weather/London
   ```

If these work, the API is fine! ✅

### Step 3: Refresh Client
1. Go to `http://localhost:5249`
2. Press `Ctrl+F5` (hard refresh)
3. Or close and reopen the browser

### Step 4: Check Browser Console
Press `F12` → Console tab
- Look for the actual error message
- Check if it's CORS or connection error

## If Still Not Working

### Check API Terminal
Look for these messages:
```
Now listening on: https://localhost:7089
Application started. Press Ctrl+C to shut down.
```

### Check Client Configuration
Verify `wwwroot/appsettings.json` has:
```json
{
  "ApiBaseUrl": "https://localhost:7089"
}
```

### Common Solutions

1. **Accept SSL Certificate**
   - When opening `https://localhost:7089`, browser may ask to accept certificate
   - Click "Advanced" → "Proceed to localhost"

2. **CORS Issue**
   - Make sure client URL (`http://localhost:5249`) is in CORS allowed origins
   - Check `WeatherApp.API/Program.cs`

3. **API Not Fully Started**
   - Wait 10-15 seconds after starting API
   - Check API terminal for "Application started" message

## Test Commands

### Test API from Command Line
```powershell
# Test popular cities
Invoke-WebRequest -Uri "https://localhost:7089/api/popularcities?limit=5" -UseBasicParsing

# Test weather
Invoke-WebRequest -Uri "https://localhost:7089/api/weather/London" -UseBasicParsing
```

If these work, the API is fine and the issue is with the client connection.

## Still Having Issues?

1. **Stop both** (Ctrl+C in both terminals)
2. **Start API first** - Wait for "Application started"
3. **Then start Client** - Wait for "Application started"
4. **Refresh browser** - Hard refresh (Ctrl+F5)

