using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using WeatherApp.Client;
using WeatherApp.Client.Services;
using Supabase;
using SupabaseClient = Supabase.Client;
using Microsoft.Extensions.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

try
{
    // Configure HttpClient to point to the API
    // Default to deployed API URL instead of localhost
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://weather-app-api-likx.onrender.com";
    Console.WriteLine($"API Base URL: {apiBaseUrl}");
    
    builder.Services.AddScoped(sp => new HttpClient 
    { 
        BaseAddress = new Uri(apiBaseUrl) 
    });

    // Add API Services
    builder.Services.AddScoped<WeatherApiService>();
    builder.Services.AddScoped<PopularCitiesApiService>();
    builder.Services.AddScoped<GeolocationService>();
    builder.Services.AddScoped<IpGeolocationService>();
    builder.Services.AddScoped<FavoriteCitiesService>();
    builder.Services.AddScoped<ThemeService>();
    builder.Services.AddScoped<AlertSettingsService>();
    builder.Services.AddScoped<AlertHistoryService>();
    builder.Services.AddScoped<WeatherAlertsService>();
    builder.Services.AddScoped<SearchedCitiesService>();
    builder.Services.AddScoped<EmailNotificationService>(provider =>
    {
        var httpClient = provider.GetRequiredService<HttpClient>();
        var jsRuntime = provider.GetRequiredService<IJSRuntime>();
        var config = provider.GetRequiredService<IConfiguration>();
        return new EmailNotificationService(httpClient, jsRuntime, config);
    });
    builder.Services.AddScoped<ChatbotService>();
    builder.Services.AddScoped<FavoriteCitiesMonitorService>(provider =>
    {
        var weatherApi = provider.GetRequiredService<WeatherApiService>();
        var alerts = provider.GetRequiredService<WeatherAlertsService>();
        var alertSettings = provider.GetRequiredService<AlertSettingsService>();
        var alertHistory = provider.GetRequiredService<AlertHistoryService>();
        var favorites = provider.GetRequiredService<FavoriteCitiesService>();
        var searched = provider.GetRequiredService<SearchedCitiesService>();
        var jsRuntime = provider.GetRequiredService<IJSRuntime>();
        var email = provider.GetService<EmailNotificationService>();
        var supabase = provider.GetService<SupabaseService>();
        var httpClient = provider.GetService<HttpClient>();
        var config = provider.GetService<IConfiguration>();
        
        return new FavoriteCitiesMonitorService(
            weatherApi, alerts, alertSettings, alertHistory, favorites, searched, jsRuntime, email, supabase, httpClient, config);
    });
    builder.Services.AddScoped<PushNotificationService>(provider =>
    {
        var jsRuntime = provider.GetRequiredService<IJSRuntime>();
        var httpClient = provider.GetRequiredService<HttpClient>();
        var config = provider.GetRequiredService<IConfiguration>();
        return new PushNotificationService(jsRuntime, httpClient, config);
    });

    // Add Authorization
    builder.Services.AddAuthorizationCore();

    // Supabase Configuration
    // Use defaults if not found in appsettings.json (fallback values)
    var supabaseUrl = builder.Configuration["Supabase:Url"] ?? "https://wdzfgezvxydmmcyybnet.supabase.co";
    var supabaseKey = builder.Configuration["Supabase:Key"] ?? "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndkemZnZXp2eHlkbW1jeXlibmV0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQyOTgxODIsImV4cCI6MjA3OTg3NDE4Mn0.951h2te--jh6rGovH1fRIbr_5lyUkMQVxBVppzleD6U";

    // Always register SupabaseAuthStateProvider (it handles null SupabaseService gracefully)
    builder.Services.AddScoped<SupabaseAuthStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
        sp.GetRequiredService<SupabaseAuthStateProvider>());

    if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
    {
        builder.Services.AddScoped<SupabaseClient>(provider => 
            new SupabaseClient(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false // Disable realtime to avoid connection issues
            }));
        
        builder.Services.AddScoped<SupabaseService>();
    }
    else
    {
        Console.WriteLine("Warning: Supabase configuration not found - authentication will work but user features disabled");
        // Register SupabaseService with null client - it will handle null gracefully
        builder.Services.AddScoped<SupabaseService>(provider => new SupabaseService(null));
    }

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error during startup: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}
