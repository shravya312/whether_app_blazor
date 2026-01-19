// Configuration Injector - Creates appsettings.json from environment variables before Blazor loads
(function() {
    'use strict';
    
    // Helper function to check if a value is a placeholder (actual {{...}} string)
    // Only treat actual placeholder strings as placeholders, not null/undefined
    function isPlaceholder(value) {
        return value && typeof value === 'string' && value.startsWith('{{') && value.endsWith('}}');
    }
    
    // Get configuration from window object (set by Dockerfile script via inline script)
    // Fallback logic:
    // 1. If placeholder detected ({{ApiBaseUrl}}) → use localhost (local dev)
    // 2. If null/undefined → use production fallback
    // 3. Otherwise → use the actual value
    const apiBaseUrl = window.APP_CONFIG?.ApiBaseUrl;
    const supabaseUrl = window.APP_CONFIG?.Supabase?.Url;
    const supabaseKey = window.APP_CONFIG?.Supabase?.Key;
    
    const config = {
        ApiBaseUrl: isPlaceholder(apiBaseUrl) 
            ? 'http://localhost:5009'  // Placeholder detected = local development
            : (apiBaseUrl || 'https://weather-app-api-likx.onrender.com'),  // Null/undefined = production fallback
        Supabase: {
            Url: isPlaceholder(supabaseUrl)
                ? 'https://wdzfgezvxydmmcyybnet.supabase.co'  // Placeholder = use default
                : (supabaseUrl || 'https://wdzfgezvxydmmcyybnet.supabase.co'),  // Null/undefined = production fallback
            Key: isPlaceholder(supabaseKey)
                ? 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndkemZnZXp2eHlkbW1jeXlibmV0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQyOTgxODIsImV4cCI6MjA3OTg3NDE4Mn0.951h2te--jh6rGovH1fRIbr_5lyUkMQVxBVppzleD6U'  // Placeholder = use default
                : (supabaseKey || 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndkemZnZXp2eHlkbW1jeXlibmV0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQyOTgxODIsImV4cCI6MjA3OTg3NDE4Mn0.951h2te--jh6rGovH1fRIbr_5lyUkMQVxBVppzleD6U')  // Null/undefined = production fallback
        }
    };
    
    // Create appsettings.json as a Blob and make it available
    const jsonContent = JSON.stringify(config, null, 2);
    const blob = new Blob([jsonContent], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    
    // Override fetch for appsettings.json to return our config
    const originalFetch = window.fetch;
    window.fetch = function(input, init) {
        if (typeof input === 'string' && input.includes('appsettings.json')) {
            return Promise.resolve(new Response(jsonContent, {
                status: 200,
                statusText: 'OK',
                headers: {
                    'Content-Type': 'application/json',
                    'Cache-Control': 'no-cache, no-store, must-revalidate',
                    'Pragma': 'no-cache',
                    'Expires': '0'
                }
            }));
        }
        return originalFetch.apply(this, arguments);
    };
    
    console.log('Configuration injector loaded');
    console.log('API Base URL:', config.ApiBaseUrl);
    console.log('Supabase URL:', config.Supabase.Url);
})();

