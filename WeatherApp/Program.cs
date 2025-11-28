using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using WeatherApp.Services;
using WeatherApp.Models;
using Supabase;
using SupabaseClient = Supabase.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpClient();
builder.Services.AddAuthorizationCore();

// MongoDB Configuration
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddScoped<WeatherService>();

// Supabase Configuration
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    builder.Services.AddScoped<SupabaseClient>(provider => 
        new SupabaseClient(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true
        }));
    
    builder.Services.AddScoped<SupabaseService>();
    builder.Services.AddScoped<SupabaseAuthStateProvider>();
    builder.Services.AddScoped<AuthenticationStateProvider>(sp => 
        sp.GetRequiredService<SupabaseAuthStateProvider>());
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
