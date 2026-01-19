// Configuration Injector - Creates appsettings.json from environment variables before Blazor loads
(function() {
    'use strict';
    
    // Get configuration from window object (set by Dockerfile script via inline script)
    // Or use default values
    const config = {
        ApiBaseUrl: window.APP_CONFIG?.ApiBaseUrl || 'https://weather-app-api-likx.onrender.com',
        Supabase: {
            Url: window.APP_CONFIG?.Supabase?.Url || 'https://wdzfgezvxydmmcyybnet.supabase.co',
            Key: window.APP_CONFIG?.Supabase?.Key || 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6IndkemZnZXp2eHlkbW1jeXlibmV0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQyOTgxODIsImV4cCI6MjA3OTg3NDE4Mn0.951h2te--jh6rGovH1fRIbr_5lyUkMQVxBVppzleD6U'
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

