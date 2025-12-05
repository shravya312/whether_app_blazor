using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
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
    var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5009";
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
    builder.Services.AddScoped<WeatherAlertsService>();

    // Add Authorization
    builder.Services.AddAuthorizationCore();

    // Supabase Configuration
    var supabaseUrl = builder.Configuration["Supabase:Url"];
    var supabaseKey = builder.Configuration["Supabase:Key"];

    if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
    {
        builder.Services.AddScoped<SupabaseClient>(provider => 
            new SupabaseClient(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false // Disable realtime to avoid connection issues
            }));
        
        builder.Services.AddScoped<SupabaseService>();
        builder.Services.AddScoped<SupabaseAuthStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
            sp.GetRequiredService<SupabaseAuthStateProvider>());
    }
    else
    {
        Console.WriteLine("Warning: Supabase configuration not found");
    }

    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error during startup: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    throw;
}
