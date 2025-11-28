using Supabase;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class SupabaseService
    {
        private readonly Client _supabase;

        public SupabaseService(Client supabase)
        {
            _supabase = supabase;
        }

        // Add methods for user authentication, data storage, etc.
    }
}

