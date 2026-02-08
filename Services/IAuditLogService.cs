namespace AceJobAgency.Services
{
    public interface IAuditLogService
    {
        Task LogActivityAsync(string userId, string action, string controller, string? description = null, bool isSuccess = true, string? errorMessage = null);
        Task<IEnumerable<Models.AuditLog>> GetUserActivityAsync(string userId, int count = 50);
    }
}
