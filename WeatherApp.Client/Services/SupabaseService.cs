using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using SupabaseClient = Supabase.Client;

namespace WeatherApp.Client.Services
{
    public class SupabaseAuthException : Exception
    {
        public string? ErrorCode { get; }
        public string? Reason { get; }
        public int? StatusCode { get; }

        public SupabaseAuthException(string message, int? statusCode = null, string? reason = null) 
            : base(message)
        {
            StatusCode = statusCode;
            Reason = reason;
            
            var messageLower = message.ToLower();
            if (message.Contains("error_code"))
            {
                try
                {
                    var jsonStart = message.IndexOf('{');
                    if (jsonStart >= 0)
                    {
                        var jsonPart = message.Substring(jsonStart);
                        if (jsonPart.Contains("\"error_code\""))
                        {
                            var errorCodeStart = jsonPart.IndexOf("\"error_code\"") + 13;
                            var errorCodeEnd = jsonPart.IndexOf('"', errorCodeStart);
                            if (errorCodeEnd > errorCodeStart)
                            {
                                ErrorCode = jsonPart.Substring(errorCodeStart, errorCodeEnd - errorCodeStart);
                            }
                        }
                    }
                }
                catch { }
            }
            
            if (ErrorCode == null && (
                messageLower.Contains("already") || 
                messageLower.Contains("exists") || 
                messageLower.Contains("user_already_registered") ||
                messageLower.Contains("email_already_exists") ||
                reason?.ToLower().Contains("already") == true ||
                reason == "user_already_registered"))
            {
                ErrorCode = "user_already_registered";
            }
        }
    }

    public class SupabaseService
    {
        private readonly SupabaseClient _supabase;
        private User? _cachedUser;

        public SupabaseService(SupabaseClient supabase)
        {
            _supabase = supabase;
            _cachedUser = _supabase.Auth.CurrentUser;
        }

        public async Task<User?> SignUpAsync(string email, string password, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var session = await _supabase.Auth.SignUp(email, password, new SignUpOptions
                {
                    Data = metadata
                });
                
                if (session?.User != null)
                {
                    var user = session.User;
                    bool isExistingUser = false;
                    
                    var createdAt = user.CreatedAt;
                    var timeSinceCreation = (DateTimeOffset.UtcNow - createdAt.ToUniversalTime()).TotalSeconds;
                    
                    if (timeSinceCreation > 3)
                    {
                        isExistingUser = true;
                    }
                    
                    if (user.LastSignInAt != null)
                    {
                        var timeDiff = (user.LastSignInAt.Value - createdAt).TotalSeconds;
                        if (timeDiff > 5)
                        {
                            isExistingUser = true;
                        }
                    }
                    
                    if (user.EmailConfirmedAt != null)
                    {
                        var confirmTimeDiff = (user.EmailConfirmedAt.Value - createdAt).TotalSeconds;
                        if (confirmTimeDiff < -1 || confirmTimeDiff > 10)
                        {
                            isExistingUser = true;
                        }
                    }
                    
                    if (isExistingUser)
                    {
                        throw new SupabaseAuthException(
                            "An account with this email already exists. Please sign in instead.",
                            400,
                            "user_already_registered");
                    }
                }
                
                _cachedUser = session?.User;
                return _cachedUser;
            }
            catch (SupabaseAuthException)
            {
                throw;
            }
            catch (GotrueException ex)
            {
                var errorMessage = ex.Message.ToLower();
                var reason = ex.Reason.ToString().ToLower();
                var statusCode = ex.StatusCode;
                
                bool isExistingUser = 
                    statusCode == 400 ||
                    statusCode == 422 ||
                    errorMessage.Contains("user already registered") ||
                    errorMessage.Contains("email already") ||
                    errorMessage.Contains("already exists") ||
                    errorMessage.Contains("already registered") ||
                    errorMessage.Contains("user_already_registered") ||
                    errorMessage.Contains("email_already_exists") ||
                    errorMessage.Contains("duplicate") ||
                    errorMessage.Contains("already") ||
                    reason.Contains("already") ||
                    reason.Contains("exists");
                
                if (isExistingUser)
                {
                    throw new SupabaseAuthException(
                        "An account with this email already exists. Please sign in instead.",
                        statusCode,
                        "user_already_registered");
                }
                
                throw new SupabaseAuthException(ex.Message, ex.StatusCode, ex.Reason.ToString());
            }
        }

        public async Task<User?> SignInAsync(string email, string password)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(email, password);
                _cachedUser = session?.User;
                return _cachedUser;
            }
            catch (GotrueException ex)
            {
                throw new SupabaseAuthException(ex.Message, ex.StatusCode, ex.Reason.ToString());
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
                await _supabase.Auth.SignOut();
                _cachedUser = null;
            }
            catch (GotrueException ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public User? GetCurrentUser()
        {
            return _supabase.Auth.CurrentUser ?? _cachedUser;
        }

        public async Task<User?> GetUserAsync()
        {
            try
            {
                var session = _supabase.Auth.CurrentSession;
                if (session != null && !string.IsNullOrEmpty(session.AccessToken))
                {
                    var user = await _supabase.Auth.GetUser(session.AccessToken);
                    _cachedUser = user;
                    return _cachedUser;
                }
                return _cachedUser;
            }
            catch
            {
                return _cachedUser;
            }
        }
    }
}

