# 🚀 Quick Start Guide

## ✅ Pre-Flight Check (All Done!)

Your configuration is already set up:
- ✅ MongoDB connection string configured
- ✅ OpenWeatherMap API key configured  
- ✅ Supabase URL and key configured
- ✅ `favorite_cities` table created in Supabase
- ✅ Ports match: API (5009) ↔ Client (5249)

## 🏃 Run the Project (2 Simple Steps)

### Step 1: Start the API

Open **Terminal/PowerShell 1**:

```powershell
cd WeatherApp.API
dotnet run
```

**Wait for this message:**
```
Now listening on: http://localhost:5009
```

✅ **Keep this terminal open!**

---

### Step 2: Start the Client

Open **Terminal/PowerShell 2** (new window):

```powershell
cd WeatherApp.Client
dotnet run
```

**Wait for this message:**
```
Now listening on: http://localhost:5249
```

✅ **Keep this terminal open too!**

---

### Step 3: Open Browser

Navigate to: **http://localhost:5249**

🎉 **You're ready to use the Weather App!**

## 🧪 Quick Test

1. **Test Weather Search:**
   - Type "London" in the search box
   - Click "Get Weather"
   - Should show current weather

2. **Test Auto-Location:**
   - Click "Use My Location" button
   - Grant browser permission
   - Should show weather for your location

3. **Test Authentication:**
   - Click "Sign Up" or "Sign In"
   - Create an account or sign in
   - Should redirect to home page

4. **Test Favorite Cities (after login):**
   - Search for a city
   - Click the heart icon ❤️
   - Go to Dashboard page
   - Should see your favorite city

## 📋 What to Expect

### First Run:
- API will connect to MongoDB (may take a few seconds)
- Client will connect to Supabase
- Browser may ask for location permission (for auto-location feature)

### Normal Operation:
- Weather searches work instantly
- Forecast data loads automatically
- Favorite cities save to Supabase
- Theme persists across sessions

## ⚠️ Common First-Run Issues

### "Unable to connect to weather service"
- **Fix**: Make sure API is running first (Step 1)
- Check API terminal for errors

### "MongoDB connection failed"
- **Fix**: Check internet connection
- Verify MongoDB Atlas IP whitelist includes your IP

### "Geolocation not working"
- **Fix**: Use `http://localhost:5249` (not HTTPS)
- Grant location permission when browser asks

### "Favorite cities not saving"
- **Fix**: Make sure you're signed in
- Verify Supabase table exists (already done ✅)

## 🛑 Stopping the Application

1. Press `Ctrl+C` in both terminal windows
2. Or close the terminal windows

## 📱 Access Points

- **Client App**: http://localhost:5249
- **API Swagger**: http://localhost:5009/swagger (when API is running)
- **API Health**: http://localhost:5009/api/weather/London (test endpoint)

## 🎯 Next Steps After Running

1. ✅ Test all features
2. ✅ Sign up/Sign in
3. ✅ Add favorite cities
4. ✅ Try the Analytics page
5. ✅ Test dark/light theme
6. ✅ Try auto-location

---

**That's it! You're all set! 🎉**

For detailed troubleshooting, see `PRE_RUN_CHECKLIST.md`

