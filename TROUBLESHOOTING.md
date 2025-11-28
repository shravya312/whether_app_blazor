# Troubleshooting Guide

## Error: ERR_CONNECTION_REFUSED

### Problem
The client cannot connect to the API because the API server is not running.

### Solution

**You MUST run BOTH projects:**

#### Terminal 1 - Start API (REQUIRED FIRST!)
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.API"
dotnet run
```

**Wait for:**
```
Now listening on: https://localhost:7089
Application started. Press Ctrl+C to shut down.
```

#### Terminal 2 - Start Client
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.Client"
dotnet run
```

**Wait for:**
```
Now listening on: http://localhost:5249
Application started. Press Ctrl+C to shut down.
```

### Verify API is Running

1. Open browser to: `https://localhost:7089/swagger`
2. You should see Swagger UI with API endpoints
3. Test: `GET /api/popularcities` - should return a list

### Common Issues

#### Issue 1: Port Already in Use
**Error:** Port 7089 is already in use

**Solution:**
- Stop any other application using port 7089
- Or change port in `launchSettings.json`

#### Issue 2: MongoDB Connection Failed
**Error:** Cannot connect to MongoDB

**Solution:**
- Check `appsettings.json` has correct MongoDB connection string
- Verify MongoDB Atlas cluster is running
- Check network/firewall settings

#### Issue 3: OpenWeatherMap API Key Invalid
**Error:** Weather API returns 401/403

**Solution:**
- Verify API key in `appsettings.json`
- Check OpenWeatherMap account is active
- Regenerate API key if needed

#### Issue 4: CORS Errors
**Error:** CORS policy blocking requests

**Solution:**
- Verify client URL is in CORS allowed origins
- Check `Program.cs` in API has correct CORS configuration

## Quick Test

### Test API Directly
```powershell
# In a new terminal
curl https://localhost:7089/api/popularcities?limit=5
```

Or open in browser:
```
https://localhost:7089/api/popularcities?limit=5
```

### Test Weather Endpoint
```
https://localhost:7089/api/weather/London
```

## Still Not Working?

1. **Check API terminal** for error messages
2. **Check client terminal** for error messages  
3. **Check browser console** (F12) for specific errors
4. **Verify both are running** on correct ports
5. **Restart both** projects

## Order Matters!

1. ✅ Start API first
2. ✅ Wait for "Application started"
3. ✅ Then start Client
4. ✅ Open browser to client URL

