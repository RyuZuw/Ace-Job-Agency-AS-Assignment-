using AceJobAgency.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace AceJobAgency.Middleware
{
    public class ConcurrentSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ConcurrentSessionMiddleware> _logger;

        public ConcurrentSessionMiddleware(RequestDelegate next, ILogger<ConcurrentSessionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
        {
            // Skip middleware for static files and authentication endpoints
            var path = context.Request.Path.Value?.ToLower();
            if (path != null && (path.StartsWith("/css/") || 
                                path.StartsWith("/js/") || 
                                path.StartsWith("/lib/") ||
                                path.StartsWith("/account/login") ||
                                path.StartsWith("/account/register") ||
                                path.StartsWith("/account/twofactor") ||
                                path.StartsWith("/account/forgotpassword") ||
                                path.StartsWith("/account/resetpassword") ||
                                path.StartsWith("/account/lockout")))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var sessionId = context.Session.GetString("SessionId");

                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
                {
                    var isValid = await sessionService.ValidateSessionAsync(userId, sessionId);
                    
                    if (!isValid)
                    {
                        _logger.LogWarning($"Invalid or expired session detected for user {userId}");
                        
                        // Sign out the user (Identity uses "Identity.Application" scheme)
                        await context.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
                        context.Session.Clear();
                        
                        // Redirect to login with message
                        context.Response.Redirect("/Account/Login?error=SessionExpired");
                        return;
                    }

                    // Update last activity
                    await sessionService.UpdateLastActivityAsync(userId, sessionId);
                }
            }

            await _next(context);
        }
    }
}
