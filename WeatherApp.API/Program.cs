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
        policy.WithOrigins(
                "https://localhost:7064",  // Client HTTPS
                "http://localhost:5249",     // Client HTTP
                "https://localhost:7001",    // Alternative client ports
                "http://localhost:5001",
                "http://localhost:5249",     // Ensure HTTP is allowed
                "https://localhost:5249")     // HTTPS variant
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS (must be before UseAuthorization)
app.UseCors("AllowBlazorClient");

app.UseRouting();

// Note: No authentication required for API endpoints currently
// app.UseAuthorization(); // Commented out - API endpoints are public

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
