# Weather Application

A modern weather application built with Blazor WebAssembly and .NET 8 Web API, featuring real-time weather data, user authentication, and dynamic popular cities tracking.

## 🚀 Tech Stack

- **Frontend**: Blazor WebAssembly (.NET 8)
- **Backend**: ASP.NET Core Web API (.NET 8)
- **Database**: MongoDB (Weather data & search statistics)
- **Authentication**: Supabase Auth
- **Weather API**: OpenWeatherMap
- **Language**: C#

## ✨ Features

### Core Features
- **User Authentication**: Sign up and sign in using Supabase Auth with role-based access control
- **Weather Search**: Search weather by city name with optional country code
- **IP-Based Location Detection**: Automatic weather detection using IP geolocation (fast and reliable)
- **Dynamic Popular Cities**: Top 5 most searched cities displayed dynamically based on MongoDB search statistics
- **Smart Favorite Cities**: Auto-adds manually searched cities to favorites, ordered by search frequency
- **Real-time Weather Data**: Current weather conditions with extended metrics (temperature, humidity, pressure, visibility, wind speed, cloudiness)
- **5-Day Forecast**: Detailed 5-day weather forecast with hourly breakdowns
- **Case-Insensitive Search**: Smart city name normalization
- **Location Mapping**: Intelligent city name mapping (replaces landmarks with city names)

### User-Specific Features
- **Non-Registered Users**: Basic current weather info, limited city search, basic forecast view
- **Registered Users**: 
  - Detailed weather metrics
  - **Smart Favorite Cities**: Automatically tracks and orders favorite cities by search frequency
  - Auto-add cities to favorites when manually searched
  - Extended forecast data
  - Custom dashboard
  - Weather analytics
  - Profile management

### Advanced Features
- **Weather Alerts**: Automatic detection of severe weather conditions (extreme heat/cold, high winds, thunderstorms, heavy rain/snow)
- **Weather Analytics**: 
  - Temperature trend analysis
  - Rainfall/humidity patterns
  - City comparison tool
  - Precipitation statistics
- **Dark/Light Theme**: Toggle between dark and light themes with persistent storage
- **Weather-Based UI Themes**: Dynamic color themes based on weather conditions (sunny, cloudy, rainy, stormy, snowy, foggy)
- **Progressive Web App (PWA)**: 
  - Offline functionality
  - Background sync
  - Install as desktop app
  - Cache management
  - Service worker for offline support
- **Responsive Design**: Modern, clean interface built with Bootstrap with enhanced mobile support
- **Search Statistics**: Tracks city search frequency in MongoDB

## 📁 Project Structure

```
whether_app_blazor/
├── WeatherApp.API/          # Backend Web API
│   ├── Controllers/         # API endpoints
│   ├── Models/              # Data models
│   ├── Services/            # Business logic
│   └── Program.cs           # API configuration
│
├── WeatherApp.Client/        # Blazor WebAssembly Frontend
│   ├── Components/          # Reusable UI components
│   ├── Pages/              # Application pages
│   ├── Services/           # API client services
│   ├── Layout/             # Layout components
│   └── Program.cs          # Client configuration
│
├── run-api.ps1             # PowerShell script to run API
├── run-client.ps1          # PowerShell script to run Client
└── .gitignore              # Git ignore rules
```

## 📋 Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MongoDB Atlas account (or local MongoDB instance)
- Supabase account
- OpenWeatherMap API key

## 🔧 Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd whether_app_blazor
```

### 2. Configure API Settings

Edit `WeatherApp.API/appsettings.json`:

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

### 3. Configure Client Settings

Edit `WeatherApp.Client/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5009",
  "Supabase": {
    "Url": "your-supabase-url",
    "Key": "your-supabase-anon-key"
  }
}
```

## 🏃 Running the Application

### Option 1: Using PowerShell Scripts

**Terminal 1 - Start API:**
```powershell
.\run-api.ps1
```

**Terminal 2 - Start Client:**
```powershell
.\run-client.ps1
```

### Option 2: Using dotnet CLI

**Terminal 1 - Start API:**
```powershell
cd WeatherApp.API
dotnet run
```

**Terminal 2 - Start Client:**
```powershell
cd WeatherApp.Client
dotnet run
```

### Access the Application

- **Client**: http://localhost:5249 (or https://localhost:7064)
- **API**: http://localhost:5009
- **Swagger UI**: http://localhost:5009/swagger (when API is running)

### Application Pages

- **Home** (`/`): Main weather search and display page
- **Dashboard** (`/dashboard`): Registered users' personalized dashboard with favorite cities (requires authentication)
- **Analytics** (`/analytics`): Weather analytics and city comparison (requires authentication)
- **Profile** (`/profile`): User profile management (requires authentication)
- **Users** (`/users`): User information display (requires authentication)
- **Auth** (`/auth`): Sign in/Sign up page

## 🔌 API Endpoints

### Weather Endpoints

#### Get Weather by City
```
GET /api/weather/{city}?country={countryCode}
```

**Example:**
```
GET /api/weather/London
GET /api/weather/Bangalore?country=IN
```

#### Get Weather by Location (Coordinates)
```
POST /api/weather/location
Content-Type: application/json

{
  "Latitude": 51.5074,
  "Longitude": -0.1278
}
```

#### Get 5-Day Forecast by City
```
GET /api/weather/forecast/{city}?country={countryCode}
```

#### Get 5-Day Forecast by Location
```
POST /api/weather/forecast/location
Content-Type: application/json

{
  "Latitude": 51.5074,
  "Longitude": -0.1278
}
```

#### Search Weather (POST)
```
POST /api/weather/search
Content-Type: application/json

{
  "City": "London",
  "Country": "GB"
}
```

### Popular Cities Endpoint

#### Get Popular Cities
```
GET /api/popularcities?limit=5
```

**Response:**
```json
["London", "New York", "Tokyo", "Bangalore", "Sydney"]
```

## 🔐 Authentication

The application uses Supabase Auth for user management:

- **Sign Up**: Create a new account with email and password
- **Sign In**: Login with existing credentials
- **Protected Routes**: Advanced features require authentication (Dashboard, Analytics, Profile)
- **User Profile**: View and manage user information on the Profile page
- **Role-Based Access**: Different features available for registered vs non-registered users

## 📊 Database Schema

### MongoDB Collections

1. **WeatherData**: Stores weather information
   - City name, Country
   - Temperature, FeelsLike, Humidity, Pressure
   - Visibility, WindSpeed, WindDirection, Cloudiness
   - Description, MainCondition, Icon
   - Latitude, Longitude
   - Timestamp

2. **CitySearchStats**: Tracks search frequency
   - City name
   - Search count
   - Last searched timestamp

### Supabase Tables

**favorite_cities** (Required for favorite cities feature):
```sql
CREATE TABLE favorite_cities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES auth.users(id),
  city TEXT NOT NULL,
  country TEXT NOT NULL,
  added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
  UNIQUE(user_id, city, country)
);

CREATE INDEX idx_favorite_cities_user_id ON favorite_cities(user_id);
```

**Note**: You need to create this table in your Supabase project for the favorite cities feature to work.

## 🎨 Key Features Explained

### Dynamic Popular Cities
The application tracks how many times each city is searched in MongoDB and displays the top 5 most popular cities in a dropdown. This list updates automatically after each search, showing cities ordered by search frequency.

### Case-Insensitive Search
City names are normalized to Title Case (e.g., "bangalore" → "Bangalore") to ensure consistent searching and storage.

### Weather Data Caching
Weather data is stored in MongoDB to reduce API calls and improve performance.

### IP-Based Location Detection
Uses IP geolocation service to automatically detect user's approximate location and fetch weather data. Faster and more reliable than browser geolocation, works without permission prompts.

### Location Mapping
Intelligently maps coordinates to city names, replacing landmarks (like "Kanija Bhavan") with proper city names (like "Bangalore") for better user experience.

### Smart Favorite Cities
- **Auto-Add**: Manually searched cities are automatically added to favorites
- **Search-Based Ordering**: Favorite cities are ordered by search frequency (most searched first)
- **Search Tracking**: Each favorite city tracks how many times it's been searched
- **Real-time Updates**: Favorite cities list updates automatically after each search
- **Quick Access**: Click any favorite city to instantly load its weather
- **Storage**: Data is stored locally using localStorage for fast access

### 5-Day Forecast
Provides detailed weather forecast for the next 5 days with 3-hour intervals, including temperature ranges, precipitation, and weather conditions.

### Weather Alerts
Automatically detects and displays alerts for severe weather conditions including extreme temperatures, high winds, thunderstorms, and heavy precipitation.

### Weather Analytics
Provides comprehensive analytics including:
- Temperature trends over time
- Humidity and precipitation patterns
- City-to-city weather comparison
- Statistical analysis of weather data

### Progressive Web App
The application can be installed as a PWA, works offline with cached data, and supports background sync for weather updates.

### Theme System
- Dark/Light mode toggle with persistent storage
- Weather-based dynamic themes that change colors based on current conditions

## 🛠️ Development

### Build the Projects

```powershell
# Build API
cd WeatherApp.API
dotnet build

# Build Client
cd ../WeatherApp.Client
dotnet build
```

### Run Tests (if available)

```powershell
dotnet test
```

## 📝 Configuration Files

- `WeatherApp.API/appsettings.json` - API configuration (MongoDB, Weather API)
- `WeatherApp.Client/wwwroot/appsettings.json` - Client configuration (API URL, Supabase)

**Note**: `appsettings.Development.json` files are excluded from Git for security.

## 🐛 Troubleshooting

### Port Already in Use
If you see "address already in use" error:
- Stop the running process (Ctrl+C)
- Or change the port in `Properties/launchSettings.json`

### API Connection Refused
- Ensure the API is running before starting the client
- Check that `ApiBaseUrl` in client config matches API URL
- Verify CORS settings in `WeatherApp.API/Program.cs`

### MongoDB Connection Issues
- Verify connection string in `appsettings.json`
- Check network connectivity to MongoDB Atlas
- Ensure IP whitelist includes your IP address

## 📄 License

See [LICENSE](LICENSE) file for details.

## 👥 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📧 Support

For issues and questions, please open an issue in the repository.

---

**Built with ❤️ using Blazor and .NET 8**

