namespace AceJobAgency.Services
{
    public interface ISessionService
    {
        Task<string> CreateSessionAsync(string userId, string sessionId);
        Task<bool> ValidateSessionAsync(string userId, string sessionId);
        Task TerminateSessionAsync(string userId, string sessionId);
        Task TerminateAllSessionsAsync(string userId, string exceptSessionId);
        Task<IEnumerable<Models.UserSession>> GetActiveSessionsAsync(string userId);
        Task UpdateLastActivityAsync(string userId, string sessionId);
    }
}
