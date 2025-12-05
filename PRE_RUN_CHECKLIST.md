# Pre-Run Checklist - Weather Application

Before running the project, ensure all these items are configured:

## ✅ 1. API Configuration (`WeatherApp.API/appsettings.json`)

Check that your API configuration file has:

```json
{
  "MongoDB": {
    "ConnectionString": "your-mongodb-connection-string",
    "DatabaseName": "WeatherApp"
  },
  "WeatherApi": {
    "Key": "your-openweathermap-api-key"
  }
}
```

### MongoDB Setup:
- [ ] Get MongoDB connection string from MongoDB Atlas (or use local MongoDB)
- [ ] Format: `mongodb+srv://username:password@cluster.mongodb.net/WeatherApp`
- [ ] Replace `username`, `password`, and `cluster` with your values
- [ ] Ensure IP whitelist includes your IP address in MongoDB Atlas

### OpenWeatherMap API Key:
- [ ] Sign up at https://openweathermap.org/api
- [ ] Get your free API key from the dashboard
- [ ] Paste it in `WeatherApi.Key` field
- [ ] Free tier allows 60 calls/minute

## ✅ 2. Client Configuration (`WeatherApp.Client/wwwroot/appsettings.json`)

Check that your client configuration has:

```json
{
  "ApiBaseUrl": "http://localhost:5009",
  "Supabase": {
    "Url": "your-supabase-project-url",
    "Key": "your-supabase-anon-key"
  }
}
```

### Supabase Setup:
- [ ] Get your Supabase project URL (format: `https://xxxxx.supabase.co`)
- [ ] Get your Supabase anon/public key from Settings > API
- [ ] Paste both values in the configuration
- [ ] Ensure the `favorite_cities` table is created (already done ✅)

## ✅ 3. Verify Ports

Check `Properties/launchSettings.json` in both projects:

### API Ports:
- [ ] API HTTP port: `5009` (or check your config)
- [ ] API HTTPS port: `5008` (if using HTTPS)

### Client Ports:
- [ ] Client HTTP port: `5249` (or check your config)
- [ ] Client HTTPS port: `7064` (if using HTTPS)

**Note**: Make sure `ApiBaseUrl` in client config matches the API port!

## ✅ 4. CORS Configuration

The API should allow requests from the client. Check `WeatherApp.API/Program.cs`:

```csharp
policy.WithOrigins(
    "https://localhost:7064",  // Client HTTPS
    "http://localhost:5249",   // Client HTTP
    // ... other ports
)
```

- [ ] Verify client ports are in the CORS allowed origins list
- [ ] If using different ports, update CORS configuration

## ✅ 5. Dependencies & Packages

Ensure all NuGet packages are restored:

```powershell
# In WeatherApp.API folder
dotnet restore

# In WeatherApp.Client folder
dotnet restore
```

- [ ] Run `dotnet restore` in both projects
- [ ] Check for any package errors

## ✅ 6. Build the Projects

Build both projects to check for compilation errors:

```powershell
# Build API
cd WeatherApp.API
dotnet build

# Build Client
cd ../WeatherApp.Client
dotnet build
```

- [ ] No compilation errors in API project
- [ ] No compilation errors in Client project

## ✅ 7. Database Setup

### MongoDB:
- [ ] MongoDB connection is working
- [ ] Database `WeatherApp` exists (will be created automatically)
- [ ] Collections will be created automatically on first use

### Supabase:
- [ ] `favorite_cities` table is created ✅
- [ ] Row Level Security policies are set up ✅
- [ ] Index on `user_id` is created ✅

## ✅ 8. Browser Requirements

For full functionality:
- [ ] Use a modern browser (Chrome, Edge, Firefox, Safari)
- [ ] For geolocation: Use HTTPS or localhost (required by browsers)
- [ ] Enable location permissions when prompted
- [ ] For PWA: Browser supports service workers

## ✅ 9. Network & Firewall

- [ ] Internet connection is available (for API calls)
- [ ] Firewall allows connections to:
  - MongoDB Atlas (if using cloud)
  - Supabase servers
  - OpenWeatherMap API
- [ ] No proxy blocking API requests

## ✅ 10. Optional: Environment Variables

If you prefer environment variables over appsettings.json:

### For API:
```powershell
$env:MongoDB__ConnectionString="your-connection-string"
$env:WeatherApi__Key="your-api-key"
```

### For Client:
Set in `wwwroot/appsettings.json` or use environment-specific files.

## 🚀 Quick Start Commands

Once everything is configured:

### Terminal 1 - Start API:
```powershell
cd WeatherApp.API
dotnet run
```

Wait for: `Now listening on: http://localhost:5009`

### Terminal 2 - Start Client:
```powershell
cd WeatherApp.Client
dotnet run
```

Wait for: `Now listening on: http://localhost:5249`

### Open Browser:
Navigate to: `http://localhost:5249`

## 🔍 Troubleshooting Common Issues

### Issue: "Unable to connect to the weather service"
- ✅ Check API is running on correct port
- ✅ Verify `ApiBaseUrl` in client config matches API port
- ✅ Check CORS configuration

### Issue: "MongoDB connection failed"
- ✅ Verify connection string is correct
- ✅ Check IP whitelist in MongoDB Atlas
- ✅ Ensure network connectivity

### Issue: "Supabase authentication failed"
- ✅ Verify Supabase URL and key are correct
- ✅ Check Supabase project is active
- ✅ Verify table exists

### Issue: "Geolocation not working"
- ✅ Use HTTPS or localhost (required by browsers)
- ✅ Grant location permissions when prompted
- ✅ Check browser console for errors

### Issue: "Favorite cities not saving"
- ✅ Verify `favorite_cities` table exists in Supabase
- ✅ Check Row Level Security policies are set
- ✅ Verify user is authenticated

## 📋 Final Checklist Before Running

- [ ] MongoDB connection string configured
- [ ] OpenWeatherMap API key configured
- [ ] Supabase URL and key configured
- [ ] `favorite_cities` table created in Supabase
- [ ] Ports match between API and Client config
- [ ] CORS configured correctly
- [ ] Projects build without errors
- [ ] Dependencies restored

## ✅ Ready to Run!

If all items above are checked, you're ready to run the project!

1. Start API first
2. Then start Client
3. Open browser to client URL
4. Test the application

---

**Need Help?** Check the main README.md for detailed setup instructions.

