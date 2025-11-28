# Blazor WebAssembly Setup Guide

## Project Structure

You now have **3 projects**:

1. **WeatherApp.API** - Backend Web API (ASP.NET Core)
2. **WeatherApp.Client** - Blazor WebAssembly Frontend
3. **WeatherApp** - Original Blazor Server (can be kept for reference or removed)

## Backend API (WeatherApp.API)

### Features:
- ✅ RESTful API endpoints
- ✅ MongoDB integration
- ✅ OpenWeatherMap API integration
- ✅ CORS configured for Blazor client
- ✅ Swagger/OpenAPI documentation

### Endpoints:
- `GET /api/weather/{city}?country={country}` - Get weather for a city
- `POST /api/weather/search` - Search weather (with request body)
- `GET /api/popularcities?limit={limit}` - Get popular cities

### Configuration:
- MongoDB connection string in `appsettings.json`
- OpenWeatherMap API key in `appsettings.json`
- CORS allows `https://localhost:7001` and `http://localhost:5001`

## Client (WeatherApp.Client)

### Features:
- ✅ Blazor WebAssembly
- ✅ API service classes to call backend
- ✅ Supabase authentication (client-side)
- ✅ Ready for component migration

### API Services:
- `WeatherApiService` - Calls weather endpoints
- `PopularCitiesApiService` - Calls popular cities endpoint

## Next Steps

1. **Copy Components & Pages** from WeatherApp to WeatherApp.Client:
   - `Pages/Auth.razor`
   - `Pages/Users.razor`
   - `Pages/Index.razor` (update to use API services)
   - `Components/WeatherDisplay.razor` (update to use API services)
   - `Components/RedirectToLogin.razor`
   - `Shared/AuthStatus.razor`
   - `Shared/NavMenu.razor`

2. **Copy Supabase Services**:
   - `Services/SupabaseService.cs`
   - `Services/SupabaseAuthStateProvider.cs`

3. **Update Components** to use:
   - `WeatherApiService` instead of `WeatherService`
   - `PopularCitiesApiService` instead of direct MongoDB calls

4. **Run Both Projects**:
   ```bash
   # Terminal 1 - API
   cd WeatherApp.API
   dotnet run
   
   # Terminal 2 - Client
   cd WeatherApp.Client
   dotnet run
   ```

## Ports

- **API**: `https://localhost:7000` (check launchSettings.json)
- **Client**: `https://localhost:7001` (check launchSettings.json)

## Important Notes

- The API must be running before the client
- CORS is configured to allow the client to call the API
- Authentication is handled client-side with Supabase
- All database operations happen in the API

