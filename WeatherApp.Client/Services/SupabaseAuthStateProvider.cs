using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using System.Security.Claims;

namespace WeatherApp.Client.Services
{
    public class SupabaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly SupabaseService? _supabaseService;
        private User? _currentUser;

        public SupabaseAuthStateProvider(SupabaseService? supabaseService = null)
        {
            _supabaseService = supabaseService;
            _currentUser = _supabaseService?.GetCurrentUser();
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // If Supabase is not configured, return unauthenticated state
            if (_supabaseService == null)
            {
                return CreateAuthenticationState(null);
            }

            // Ensure we get the latest user state
            _currentUser = _supabaseService.GetCurrentUser();
            
            // If no user found, try to get from session
            if (_currentUser == null)
            {
                _currentUser = await _supabaseService.GetUserAsync();
            }
            
            return CreateAuthenticationState(_currentUser);
        }

        public void UpdateUser(User? user)
        {
            if (_supabaseService == null) return;
            
            _currentUser = user;
            NotifyAuthenticationStateChanged(
                Task.FromResult(CreateAuthenticationState(user)));
        }

        public void ResetUser()
        {
            if (_supabaseService == null) return;
            
            _currentUser = null;
            NotifyAuthenticationStateChanged(
                Task.FromResult(CreateAuthenticationState(null)));
        }

        private static AuthenticationState CreateAuthenticationState(User? user)
        {
            ClaimsIdentity identity;

            if (user == null)
            {
                identity = new ClaimsIdentity();
            }
            else
            {
                var userEmail = user.Email ?? string.Empty;
                var userId = user.Id ?? string.Empty;
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Name, userEmail.Length > 0 ? userEmail : userId),
                    new Claim(ClaimTypes.Email, userEmail)
                };

                if (user.UserMetadata != null && user.UserMetadata.TryGetValue("role", out var role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role.ToString() ?? "User"));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                }

                identity = new ClaimsIdentity(claims, "Supabase");
            }

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}

