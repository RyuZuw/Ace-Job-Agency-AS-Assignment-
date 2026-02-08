using AceJobAgency.Data;
using AceJobAgency.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AceJobAgency.Services
{
    public class PasswordHistoryService : IPasswordHistoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public PasswordHistoryService(
            ApplicationDbContext context,
            UserManager<Models.ApplicationUser> userManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<bool> IsPasswordInHistoryAsync(string userId, string newPassword)
        {
            var historyCount = int.Parse(_configuration["PasswordPolicy:HistoryCount"] ?? "2");
            
            var recentPasswords = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.CreatedAt)
                .Take(historyCount)
                .ToListAsync();

            foreach (var history in recentPasswords)
            {
                var result = _userManager.PasswordHasher.VerifyHashedPassword(
                    null!, history.PasswordHash, newPassword);
                
                if (result == PasswordVerificationResult.Success)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task AddPasswordToHistoryAsync(string userId, string passwordHash)
        {
            var passwordHistory = new PasswordHistory
            {
                UserId = userId,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            _context.PasswordHistories.Add(passwordHistory);
            
            // Keep only the last N passwords (HistoryCount + 1 for current)
            var historyCount = int.Parse(_configuration["PasswordPolicy:HistoryCount"] ?? "2");
            var oldPasswords = await _context.PasswordHistories
                .Where(ph => ph.UserId == userId)
                .OrderByDescending(ph => ph.CreatedAt)
                .Skip(historyCount + 1)
                .ToListAsync();

            _context.PasswordHistories.RemoveRange(oldPasswords);
            
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsPasswordAgeValidAsync(string userId, int minimumAgeDays)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChange;
            return timeSinceLastChange.TotalDays >= minimumAgeDays;
        }

        public async Task<bool> IsPasswordExpiredAsync(string userId, int maximumAgeDays)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var timeSinceLastChange = DateTime.UtcNow - user.LastPasswordChange;
            return timeSinceLastChange.TotalDays > maximumAgeDays;
        }
    }
}
