using WeatherApp.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "https://localhost:7064",  // Client HTTPS
            "http://localhost:5249",     // Client HTTP
            "https://localhost:7001",    // Alternative client ports
            "http://localhost:5001",
            "https://localhost:5249"     // HTTPS variant
        };

        // Add Render client URL from environment variable if set
        var renderClientUrl = builder.Configuration["RENDER_CLIENT_URL"];
        if (!string.IsNullOrEmpty(renderClientUrl))
        {
            allowedOrigins.Add(renderClientUrl);
            allowedOrigins.Add(renderClientUrl.Replace("https://", "http://"));
            allowedOrigins.Add(renderClientUrl.Replace("http://", "https://"));
        }

        // Add any additional origins from configuration
        var additionalOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (additionalOrigins != null)
        {
            allowedOrigins.AddRange(additionalOrigins);
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add HttpClient
builder.Services.AddHttpClient();

// MongoDB Service
builder.Services.AddSingleton<MongoDbService>();

// Weather Service
builder.Services.AddScoped<WeatherService>();

// Email Service
builder.Services.AddScoped<EmailService>();

// Push Notification Service
builder.Services.AddSingleton<PushNotificationService>();

// ML Service (Singleton for model caching)
builder.Services.AddSingleton<MLService>();

// AIML Chatbot Service
builder.Services.AddScoped<AIMLService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Only use HTTPS redirection if HTTPS is available
// Skip HTTPS redirection when running on HTTP only to avoid warnings
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? 
           builder.Configuration["urls"] ?? 
           Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";

if (urls.Contains("https://", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

// Enable CORS (must be before UseAuthorization)
app.UseCors("AllowBlazorClient");

app.UseRouting();

// Note: No authentication required for API endpoints currently
// app.UseAuthorization(); // Commented out - API endpoints are public

// Add a root route that redirects to Swagger in development, or shows API info
app.MapGet("/", () => 
{
    if (app.Environment.IsDevelopment())
    {
        return Results.Redirect("/swagger");
    }
    return Results.Ok(new 
    { 
        message = "Weather App API", 
        version = "1.0",
        endpoints = new[] 
        { 
            "/swagger - API Documentation",
            "/api/weather/{city} - Get weather by city",
            "/api/push/vapid-public-key - Get VAPID public key"
        }
    });
});

app.MapControllers();

try
{
    app.Run();
}
catch (Microsoft.AspNetCore.Connections.AddressInUseException ex)
{
    Console.WriteLine($"\n\nERROR: The API port is already in use. Please ensure no other instance of the API is running or change the port.\nDetails: {ex.Message}\n\n");
}
catch (Exception ex)
{
    Console.WriteLine($"\n\nFATAL API ERROR: {ex.Message}\nStack Trace: {ex.StackTrace}\n\n");
}

public partial class Program { } // Made public for integration tests
