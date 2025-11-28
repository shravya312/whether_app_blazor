# How to Run the Blazor WebAssembly Project

## Prerequisites
- .NET 8 SDK installed
- MongoDB Atlas connection (already configured)
- OpenWeatherMap API key (already configured)
- Supabase credentials (already configured)

## Step-by-Step Instructions

### Option 1: Run Both Projects in Separate Terminals (Recommended)

#### Step 1: Open First Terminal - Start the API Backend

1. Open **PowerShell** or **Command Prompt**
2. Navigate to the project root:
   ```powershell
   cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
   ```
3. Navigate to the API project:
   ```powershell
   cd WeatherApp.API
   ```
4. Run the API:
   ```powershell
   dotnet run
   ```
5. Wait for the API to start. You should see:
   ```
   Now listening on: https://localhost:7089
   Now listening on: http://localhost:5009
   ```
6. **Keep this terminal open** - The API must keep running!

#### Step 2: Open Second Terminal - Start the Client

1. Open a **NEW** PowerShell or Command Prompt window
2. Navigate to the project root:
   ```powershell
   cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
   ```
3. Navigate to the Client project:
   ```powershell
   cd WeatherApp.Client
   ```
4. Run the Client:
   ```powershell
   dotnet run
   ```
5. Wait for the client to start. You should see:
   ```
   Now listening on: https://localhost:7001
   ```
6. The browser should automatically open to `https://localhost:7001`

### Option 2: Run Both Projects in Same Terminal (Sequential)

**Note:** This is not recommended as you'll need to stop one to run the other.

1. Open PowerShell or Command Prompt
2. Navigate to API and run it:
   ```powershell
   cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.API"
   dotnet run
   ```
3. In a **new terminal**, navigate to Client and run it:
   ```powershell
   cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor\WeatherApp.Client"
   dotnet run
   ```

## Quick Commands (Copy & Paste)

### Terminal 1 - API:
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
cd WeatherApp.API
dotnet run
```

### Terminal 2 - Client:
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
cd WeatherApp.Client
dotnet run
```

### Alternative: Use PowerShell Scripts

You can also use the provided scripts:

**Terminal 1 - API:**
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
.\run-api.ps1
```

**Terminal 2 - Client:**
```powershell
cd "C:\Users\Shravya H Jain\Downloads\blazor_whether\whether_app_blazor"
.\run-client.ps1
```

## What to Expect

### API Terminal Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7089
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5009
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Client Terminal Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7064
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

## Accessing the Applications

- **Client (Blazor WASM)**: https://localhost:7064 (or http://localhost:5249)
- **API Swagger Documentation**: https://localhost:7089/swagger (or http://localhost:5009/swagger)

## Troubleshooting

### Error: "Port already in use"
- Stop any running instances
- Or change the port in `launchSettings.json`

### Error: "Cannot connect to API"
- Make sure the API is running first
- Check that the API URL in `appsettings.json` matches the API port
- Verify CORS is configured correctly

### Error: "Build failed"
- Run `dotnet restore` first
- Check that all NuGet packages are installed
- Verify .NET 8 SDK is installed: `dotnet --version`

## Stopping the Applications

- Press `Ctrl+C` in each terminal to stop the applications
- Always stop the Client first, then the API

## Testing the Setup

1. **Test API**: Open https://localhost:7089/swagger
   - Try: `GET /api/popularcities`
   - Try: `GET /api/weather/{city}` (e.g., `/api/weather/London`)

2. **Test Client**: Open https://localhost:7001
   - Should load the Blazor WebAssembly app
   - Check browser console for any errors

## Important Notes

- ⚠️ **API must be running before Client**
- ⚠️ **Keep both terminals open while developing**
- ⚠️ **Use HTTPS URLs** (the default)
- ⚠️ **Accept SSL certificates** if prompted by browser

