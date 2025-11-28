# ⚠️ IMPORTANT: Start the API First!

## The Problem
You're seeing `ERR_CONNECTION_REFUSED` because **the API backend is not running**.

The client (Blazor WASM) needs the API to be running to fetch weather data.

## Solution: Start the API

### Step 1: Open a NEW Terminal/PowerShell Window

### Step 2: Navigate to API and Start It
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.API"
dotnet run
```

### Step 3: Wait for This Message
You should see:
```
Now listening on: https://localhost:7089
Now listening on: http://localhost:5009
Application started. Press Ctrl+C to shut down.
```

### Step 4: Keep This Terminal Open
**DO NOT CLOSE** this terminal - the API must keep running!

## Step 5: Start the Client (In Another Terminal)

Open a **second** terminal window:

```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.Client"
dotnet run
```

## Quick Check

1. ✅ API running on `https://localhost:7089`
2. ✅ Client running on `http://localhost:5249`
3. ✅ Open browser to `http://localhost:5249`

## Troubleshooting

### If API won't start:
- Check if port 7089 is already in use
- Verify MongoDB connection string in `appsettings.json`
- Check OpenWeatherMap API key is set

### If still getting connection errors:
- Make sure API is running FIRST
- Check browser console for specific errors
- Verify `appsettings.json` in `wwwroot` has correct API URL

## Remember
**Always start the API before the Client!**

