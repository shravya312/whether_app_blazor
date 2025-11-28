using Microsoft.AspNetCore.Components.Authorization;
using Supabase.Gotrue;
using System.Security.Claims;

namespace WeatherApp.Services
{
    public class SupabaseAuthStateProvider : AuthenticationStateProvider
    {
        private readonly SupabaseService _supabaseService;
        private User? _currentUser;

        public SupabaseAuthStateProvider(SupabaseService supabaseService)
        {
            _supabaseService = supabaseService;
            _currentUser = _supabaseService.GetCurrentUser();
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            _currentUser = _supabaseService.GetCurrentUser();
            return Task.FromResult(CreateAuthenticationState(_currentUser));
        }

        public void UpdateUser(User? user)
        {
            _currentUser = user;
            NotifyAuthenticationStateChanged(
                Task.FromResult(CreateAuthenticationState(user)));
        }

        public void ResetUser()
        {
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

                // Add role claim (you can extend this based on user metadata)
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

