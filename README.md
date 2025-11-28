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

- **User Authentication**: Sign up and sign in using Supabase Auth
- **Weather Search**: Search weather by city name with optional country code
- **Dynamic Popular Cities**: Top 5 most searched cities displayed dynamically
- **Real-time Weather Data**: Current weather conditions, temperature, humidity, and more
- **Case-Insensitive Search**: Smart city name normalization
- **Search Statistics**: Tracks city search frequency in MongoDB
- **Responsive UI**: Modern, clean interface built with Bootstrap

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

- **Client**: http://localhost:5249
- **API**: http://localhost:5009
- **Swagger UI**: http://localhost:5009/swagger (when API is running)

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
- **Protected Routes**: Weather features require authentication
- **User Profile**: View user information on the Users page

## 📊 Database Schema

### MongoDB Collections

1. **WeatherData**: Stores weather information
   - City name
   - Temperature, humidity, pressure
   - Weather description
   - Timestamp

2. **CitySearchStats**: Tracks search frequency
   - City name
   - Search count
   - Last searched timestamp

## 🎨 Key Features Explained

### Dynamic Popular Cities
The application tracks how many times each city is searched and displays the top 5 most popular cities in a dropdown. This list updates automatically as users search for different cities.

### Case-Insensitive Search
City names are normalized to Title Case (e.g., "bangalore" → "Bangalore") to ensure consistent searching and storage.

### Weather Data Caching
Weather data is stored in MongoDB to reduce API calls and improve performance.

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

