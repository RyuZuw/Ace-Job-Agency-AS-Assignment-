namespace AceJobAgency.Services
{
    public interface IPasswordHistoryService
    {
        Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword);
        Task AddPasswordToHistoryAsync(string userId, string passwordHash);
        Task<bool> IsPasswordAgeValidAsync(string userId, int minimumAgeDays);
        Task<bool> IsPasswordExpiredAsync(string userId, int maximumAgeDays);
    }
}
