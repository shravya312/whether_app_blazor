using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace WeatherApp.Client.Services
{
    /// <summary>
    /// Default AuthenticationStateProvider used when Supabase is not configured.
    /// Returns an unauthenticated state.
    /// </summary>
    public class DefaultAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}

