using AceJobAgency.Models;
using AceJobAgency.Services;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace AceJobAgency.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditLogService _auditLogService;
        private readonly ISessionService _sessionService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            UserManager<ApplicationUser> userManager,
            IEncryptionService encryptionService,
            IAuditLogService auditLogService,
            ISessionService sessionService,
            ILogger<HomeController> logger)
        {
            _userManager = userManager;
            _encryptionService = encryptionService;
            _auditLogService = auditLogService;
            _sessionService = sessionService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Profile");
            }

            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Decrypt NRIC for display
            string decryptedNRIC;
            try
            {
                decryptedNRIC = _encryptionService.Decrypt(user.NRIC);
            }
            catch
            {
                decryptedNRIC = "****";
                _logger.LogWarning($"Failed to decrypt NRIC for user {user.Email}");
            }

            // Get recent activity
            var activityLogs = await _auditLogService.GetUserActivityAsync(user.Id, 10);
            var recentActivity = activityLogs.Select(log => new ActivityLogViewModel
            {
                Action = log.Action,
                Description = log.Description ?? "N/A",
                Timestamp = log.Timestamp,
                IpAddress = log.IpAddress ?? "N/A",
                IsSuccess = log.IsSuccess
            }).ToList();

            // Get active sessions
            var activeSessions = await _sessionService.GetActiveSessionsAsync(user.Id);
            var currentSessionId = HttpContext.Session.GetString("SessionId");

            var model = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                NRIC = decryptedNRIC,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                ResumePath = user.ResumePath,
                ResumeFileName = user.ResumePath != null ? Path.GetFileName(user.ResumePath) : null,
                WhoAmI = user.WhoAmI,
                IsTwoFactorEnabled = user.IsTwoFactorEnabled,
                LastPasswordChange = user.LastPasswordChange,
                CreatedAt = user.LastPasswordChange, // Using LastPasswordChange as proxy for creation date
                RecentActivity = recentActivity
            };

            ViewBag.ActiveSessions = activeSessions;
            ViewBag.CurrentSessionId = currentSessionId;

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DownloadResume()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || string.IsNullOrEmpty(user.ResumePath))
            {
                return NotFound();
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ResumePath.TrimStart('/'));
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var mimeType = Path.GetExtension(filePath).ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            var fileName = Path.GetFileName(filePath);
            
            await _auditLogService.LogActivityAsync(user.Id, "DownloadResume", "Home", $"Downloaded resume: {fileName}");

            return PhysicalFile(filePath, mimeType, fileName);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateSession(string sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentSessionId = HttpContext.Session.GetString("SessionId");
            
            if (sessionId == currentSessionId)
            {
                // Cannot terminate current session from here
                TempData["ErrorMessage"] = "Cannot terminate your current session from this page.";
                return RedirectToAction("Profile");
            }

            await _sessionService.TerminateSessionAsync(user.Id, sessionId);
            await _auditLogService.LogActivityAsync(user.Id, "TerminateSession", "Home", $"Terminated session: {sessionId}");

            TempData["SuccessMessage"] = "Session terminated successfully.";
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TerminateAllOtherSessions()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var currentSessionId = HttpContext.Session.GetString("SessionId");
            await _sessionService.TerminateAllSessionsAsync(user.Id, currentSessionId!);
            await _auditLogService.LogActivityAsync(user.Id, "TerminateAllSessions", "Home", "Terminated all other sessions");

            TempData["SuccessMessage"] = "All other sessions have been terminated.";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
