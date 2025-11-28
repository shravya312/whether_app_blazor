using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Exceptions;
using WeatherApp.Models;
using SupabaseClient = Supabase.Client;

namespace WeatherApp.Services
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
            
            // Extract error code from message if it's JSON
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
                catch
                {
                    // If parsing fails, check message content
                }
            }
            
            // Check for user already exists indicators
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
                // Store timestamp before signup to detect if user is new
                var beforeSignup = DateTime.UtcNow;
                
                var session = await _supabase.Auth.SignUp(email, password, new SignUpOptions
                {
                    Data = metadata
                });
                
                // Check if signup actually created a new user or returned an existing one
                // In some Supabase configs, SignUp might return existing user without error
                if (session?.User != null)
                {
                    var user = session.User;
                    bool isExistingUser = false;
                    
                    // Method 1: Check CreatedAt timestamp
                    // New users will have CreatedAt very close to now (within 3 seconds)
                    // Existing users will have CreatedAt from when they originally signed up
                    var createdAt = user.CreatedAt;
                    var timeSinceCreation = (DateTimeOffset.UtcNow - createdAt.ToUniversalTime()).TotalSeconds;
                    
                    // If user was created more than 3 seconds before our signup attempt,
                    // it means this is an existing user, not a new signup
                    if (timeSinceCreation > 3)
                    {
                        isExistingUser = true;
                    }
                    
                    // Method 2: Check if user has signed in before (LastSignInAt exists)
                    // New users won't have a LastSignInAt, existing users will
                    if (user.LastSignInAt != null)
                    {
                        // If LastSignInAt is different from CreatedAt, user has signed in before
                        var timeDiff = (user.LastSignInAt.Value - createdAt).TotalSeconds;
                        if (timeDiff > 5) // More than 5 seconds difference means they signed in before
                        {
                            isExistingUser = true;
                        }
                    }
                    
                    // Method 3: Check if email is already confirmed
                    // New signups typically require email confirmation first
                    if (user.EmailConfirmedAt != null)
                    {
                        var confirmTimeDiff = (user.EmailConfirmedAt.Value - createdAt).TotalSeconds;
                        // If email was confirmed more than 10 seconds after creation, likely existing user
                        // Or if confirmed before creation (shouldn't happen but check anyway)
                        if (confirmTimeDiff < -1 || confirmTimeDiff > 10)
                        {
                            isExistingUser = true;
                        }
                    }
                    
                    if (isExistingUser)
                    {
                        // This is an existing user - signup should not succeed
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
                // Re-throw our custom exceptions
                throw;
            }
            catch (GotrueException ex)
            {
                // Check for specific error codes that indicate user already exists
                var errorMessage = ex.Message.ToLower();
                var reason = ex.Reason.ToString().ToLower();
                var statusCode = ex.StatusCode;
                
                // Comprehensive check for existing user indicators
                bool isExistingUser = 
                    statusCode == 400 ||
                    statusCode == 422 ||
                    errorMessage.Contains("user already registered") ||
                    errorMessage.Contains("email already") ||
                    errorMessage.Contains("already exists") ||
                    errorMessage.Contains("already registered") ||
                    errorMessage.Contains("user_already_registered") ||
                    errorMessage.Contains("email_already_exists") ||
                    errorMessage.Contains("\"error_code\":\"user_already_registered\"") ||
                    errorMessage.Contains("\"error_code\":\"email_already_exists\"") ||
                    errorMessage.Contains("\"error_code\": \"user_already_registered\"") ||
                    errorMessage.Contains("\"error_code\": \"email_already_exists\"") ||
                    errorMessage.Contains("duplicate") ||
                    errorMessage.Contains("already") ||
                    reason.Contains("already") ||
                    reason.Contains("exists") ||
                    (ex.Message.Contains("error_code") && (ex.Message.Contains("already") || ex.Message.Contains("exists")));
                
                if (isExistingUser)
                {
                    throw new SupabaseAuthException(
                        "An account with this email already exists. Please sign in instead.",
                        statusCode,
                        "user_already_registered");
                }
                
                // Preserve the original exception with error code for better error handling
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
                // Get user from current session
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

        public async Task<User?> UpdateProfileAsync(string? displayName = null, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var attributes = new UserAttributes();
                if (!string.IsNullOrEmpty(displayName))
                {
                    attributes.Data = metadata ?? new Dictionary<string, object>();
                    attributes.Data["display_name"] = displayName;
                }
                else if (metadata != null)
                {
                    attributes.Data = metadata;
                }

                var user = await _supabase.Auth.Update(attributes);
                _cachedUser = user;
                return _cachedUser;
            }
            catch (GotrueException ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}

