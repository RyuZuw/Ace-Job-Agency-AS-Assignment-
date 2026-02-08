using AceJobAgency.Data;
using AceJobAgency.Models;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Services
{
    public class SessionService : ISessionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SessionService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> CreateSessionAsync(string userId, string sessionId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            
            var userSession = new UserSession
            {
                UserId = userId,
                SessionId = sessionId,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserSessions.Add(userSession);
            await _context.SaveChangesAsync();

            return sessionId;
        }

        public async Task<bool> ValidateSessionAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && 
                                          s.SessionId == sessionId && 
                                          s.IsActive);

            if (session == null)
                return false;

            // Check if session has expired (20 minutes of inactivity)
            if (DateTime.UtcNow - session.LastActivity > TimeSpan.FromMinutes(20))
            {
                session.IsActive = false;
                session.ExpiredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return false;
            }

            return true;
        }

        public async Task TerminateSessionAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);

            if (session != null)
            {
                session.IsActive = false;
                session.ExpiredAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task TerminateAllSessionsAsync(string userId, string exceptSessionId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && 
                           s.SessionId != exceptSessionId && 
                           s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.ExpiredAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserSession>> GetActiveSessionsAsync(string userId)
        {
            return await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.LastActivity)
                .ToListAsync();
        }

        public async Task UpdateLastActivityAsync(string userId, string sessionId)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.SessionId == sessionId);

            if (session != null)
            {
                session.LastActivity = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
