using AceJobAgency.Models;
using AceJobAgency.Services;
using AceJobAgency.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AceJobAgency.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEncryptionService _encryptionService;
        private readonly IAuditLogService _auditLogService;
        private readonly IPasswordHistoryService _passwordHistoryService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly IEmailService _emailService;
        private readonly ITwoFactorService _twoFactorService;
        private readonly ISessionService _sessionService;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEncryptionService encryptionService,
            IAuditLogService auditLogService,
            IPasswordHistoryService passwordHistoryService,
            IRecaptchaService recaptchaService,
            IEmailService emailService,
            ITwoFactorService twoFactorService,
            ISessionService sessionService,
            IWebHostEnvironment environment,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _encryptionService = encryptionService;
            _auditLogService = auditLogService;
            _passwordHistoryService = passwordHistoryService;
            _recaptchaService = recaptchaService;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
            _sessionService = sessionService;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.RecaptchaSiteKey = _recaptchaService.GetSiteKey();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewBag.RecaptchaSiteKey = _recaptchaService.GetSiteKey();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate reCaptcha
            if (!string.IsNullOrEmpty(model.RecaptchaToken))
            {
                var recaptchaValid = await _recaptchaService.ValidateTokenAsync(model.RecaptchaToken);
                if (!recaptchaValid)
                {
                    ModelState.AddModelError(string.Empty, "reCaptcha verification failed. Please try again.");
                    return View(model);
                }
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // Check if account is locked out
            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError(string.Empty, "Account is locked out. Please try again later.");
                await _auditLogService.LogActivityAsync(user.Id, "Login", "Account", "Account locked out", false, "Account is locked out");
                return View(model);
            }

            // Check password (use CheckPasswordAsync to avoid built-in 2FA check)
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            
            if (!isPasswordValid)
            {
                // Increment access failed count for lockout
                await _userManager.AccessFailedAsync(user);
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                await _auditLogService.LogActivityAsync(user.Id, "Login", "Account", "Failed login attempt", false, "Invalid password");
                return View(model);
            }
            
            // Reset access failed count on successful password
            await _userManager.ResetAccessFailedCountAsync(user);

            // Check password maximum age
            var maxAgeDays = int.Parse(_configuration["PasswordPolicy:MaximumAgeDays"] ?? "90");
            if ((DateTime.UtcNow - user.LastPasswordChange).TotalDays > maxAgeDays)
            {
                await _auditLogService.LogActivityAsync(user.Id, "Login", "Account", "Password expired", false, "Password has exceeded maximum age");
                TempData["ErrorMessage"] = "Your password has expired. Please change your password.";
                
                // Sign in temporarily to allow password change
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("ChangePassword");
            }

            // Check if 2FA is enabled
            if (user.IsTwoFactorEnabled)
            {
                // Store user info in session for 2FA verification
                HttpContext.Session.SetString("2FA_UserId", user.Id);
                HttpContext.Session.SetString("2FA_RememberMe", model.RememberMe.ToString());
                HttpContext.Session.SetString("2FA_ReturnUrl", returnUrl ?? "");
                return RedirectToAction("TwoFactor");
            }

            // Sign in the user
            await _signInManager.SignInAsync(user, model.RememberMe);
            
            // Create session record
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
            await _sessionService.CreateSessionAsync(user.Id, sessionId);
            await _sessionService.TerminateAllSessionsAsync(user.Id, sessionId);

            await _auditLogService.LogActivityAsync(user.Id, "Login", "Account", "User logged in successfully");
            _logger.LogInformation($"User {user.Email} logged in successfully");

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TwoFactor()
        {
            var userId = HttpContext.Session.GetString("2FA_UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactor(TwoFactorViewModel model)
        {
            var userId = HttpContext.Session.GetString("2FA_UserId");
            var rememberMeStr = HttpContext.Session.GetString("2FA_RememberMe");
            var rememberMe = bool.TryParse(rememberMeStr, out bool rm) && rm;
            var returnUrl = HttpContext.Session.GetString("2FA_ReturnUrl");

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Validate 2FA code
            if (!_twoFactorService.ValidateToken(user.TwoFactorSecret!, model.Code))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code.");
                await _auditLogService.LogActivityAsync(user.Id, "TwoFactor", "Account", "Invalid 2FA code", false);
                return View(model);
            }

            // Clear 2FA session data
            HttpContext.Session.Remove("2FA_UserId");
            HttpContext.Session.Remove("2FA_RememberMe");
            HttpContext.Session.Remove("2FA_ReturnUrl");

            // Sign in the user
            await _signInManager.SignInAsync(user, rememberMe);
            
            // Create session record
            var sessionId = Guid.NewGuid().ToString();
            HttpContext.Session.SetString("SessionId", sessionId);
            await _sessionService.CreateSessionAsync(user.Id, sessionId);
            await _sessionService.TerminateAllSessionsAsync(user.Id, sessionId);

            await _auditLogService.LogActivityAsync(user.Id, "TwoFactor", "Account", "2FA verification successful");
            _logger.LogInformation($"User {user.Email} logged in with 2FA");

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            ViewBag.RecaptchaSiteKey = _recaptchaService.GetSiteKey();
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("register")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.RecaptchaSiteKey = _recaptchaService.GetSiteKey();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate reCaptcha
            var recaptchaValid = await _recaptchaService.ValidateTokenAsync(model.RecaptchaToken);
            if (!recaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "reCaptcha verification failed. Please try again.");
                return View(model);
            }

            // Check if email already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View(model);
            }

            // Check if NRIC already exists (use deterministic hash for comparison
            // because AES encryption with random IV produces different ciphertext each time)
            var nricHash = _encryptionService.ComputeHash(model.NRIC);
            var allUsers = await _userManager.Users.ToListAsync();
            var existingNricUser = allUsers.FirstOrDefault(u =>
            {
                try { return _encryptionService.ComputeHash(_encryptionService.Decrypt(u.NRIC)) == nricHash; }
                catch { return false; }
            });
            if (existingNricUser != null)
            {
                ModelState.AddModelError("NRIC", "An account with this NRIC already exists.");
                return View(model);
            }

            // Handle resume upload with security validation
            string? resumePath = null;
            if (model.Resume != null && model.Resume.Length > 0)
            {
                // Validate file extension (server-side)
                var allowedExtensions = new[] { ".pdf", ".docx" };
                var extension = Path.GetExtension(model.Resume.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("Resume", "Only PDF and DOCX files are allowed.");
                    return View(model);
                }

                // Validate file size (server-side, max 5MB)
                if (model.Resume.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("Resume", "File size must not exceed 5MB.");
                    return View(model);
                }

                // Validate content type
                var allowedContentTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
                if (!allowedContentTypes.Contains(model.Resume.ContentType))
                {
                    ModelState.AddModelError("Resume", "Invalid file type.");
                    return View(model);
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "resumes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Use GUID only for filename to prevent path traversal
                var safeFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, safeFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Resume.CopyToAsync(fileStream);
                }

                resumePath = $"/uploads/resumes/{safeFileName}";
            }

            // Create user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                NRIC = _encryptionService.Encrypt(model.NRIC),
                Gender = model.Gender,
                DateOfBirth = model.DateOfBirth,
                ResumePath = resumePath,
                WhoAmI = System.Net.WebUtility.HtmlEncode(model.WhoAmI),  // Sanitize user input
                LastPasswordChange = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Add password to history
                await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash!);

                // Add user to Member role
                await _userManager.AddToRoleAsync(user, "Member");

                await _auditLogService.LogActivityAsync(user.Id, "Register", "Account", "User registered successfully");
                _logger.LogInformation($"User {user.Email} registered successfully");

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Create session record
                var sessionId = Guid.NewGuid().ToString();
                HttpContext.Session.SetString("SessionId", sessionId);
                await _sessionService.CreateSessionAsync(user.Id, sessionId);
                await _sessionService.TerminateAllSessionsAsync(user.Id, sessionId);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sessionId = HttpContext.Session.GetString("SessionId");

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(sessionId))
            {
                await _sessionService.TerminateSessionAsync(userId, sessionId);
            }

            await _signInManager.SignOutAsync();
            HttpContext.Session.Clear();

            if (!string.IsNullOrEmpty(userId))
            {
                await _auditLogService.LogActivityAsync(userId, "Logout", "Account", "User logged out");
            }

            _logger.LogInformation("User logged out");
            return RedirectToAction("Login");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var callbackUrl = Url.Action("ResetPassword", "Account", 
                new { email = model.Email, token = token }, 
                protocol: HttpContext.Request.Scheme,
                host: HttpContext.Request.Host.Value);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(model.Email, callbackUrl!);
                await _auditLogService.LogActivityAsync(user.Id, "ForgotPassword", "Account", "Password reset email sent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email");
                ModelState.AddModelError(string.Empty, "Failed to send password reset email. Please try again later.");
                return View(model);
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Check password history
            var isInHistory = await _passwordHistoryService.IsPasswordInHistoryAsync(user.Id, model.NewPassword);
            if (isInHistory)
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse a previous password.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                // Add new password to history
                await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash!);

                // Update last password change date
                user.LastPasswordChange = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _auditLogService.LogActivityAsync(user.Id, "ResetPassword", "Account", "Password reset successful");
                _logger.LogInformation($"User {user.Email} reset their password");

                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            // Check minimum password age
            var minAgeDays = int.Parse(_configuration["PasswordPolicy:MinimumAgeDays"] ?? "1");
            var isAgeValid = await _passwordHistoryService.IsPasswordAgeValidAsync(user.Id, minAgeDays);
            if (!isAgeValid)
            {
                ModelState.AddModelError(string.Empty, $"You must wait at least {minAgeDays} day(s) before changing your password again.");
                return View(model);
            }

            // Check password history
            var isInHistory = await _passwordHistoryService.IsPasswordInHistoryAsync(user.Id, model.NewPassword);
            if (isInHistory)
            {
                ModelState.AddModelError(string.Empty, "You cannot reuse a previous password.");
                return View(model);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Add new password to history
                await _passwordHistoryService.AddPasswordToHistoryAsync(user.Id, user.PasswordHash!);

                // Update last password change date
                user.LastPasswordChange = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // Re-sign in the user
                await _signInManager.RefreshSignInAsync(user);

                await _auditLogService.LogActivityAsync(user.Id, "ChangePassword", "Account", "Password changed successfully");
                _logger.LogInformation($"User {user.Email} changed their password");

                TempData["SuccessMessage"] = "Your password has been changed successfully.";
                return RedirectToAction("Profile", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EnableTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (user.IsTwoFactorEnabled)
            {
                return RedirectToAction("Profile", "Home");
            }

            // Generate new secret key
            var secretKey = _twoFactorService.GenerateSecretKey();
            var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email!, secretKey);
            var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);

            // Store secret key temporarily
            TempData["2FA_Secret"] = secretKey;

            var model = new EnableTwoFactorViewModel
            {
                SecretKey = secretKey,
                QrCodeImageBase64 = Convert.ToBase64String(qrCodeImage),
                ManualEntryKey = secretKey
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorViewModel model)
        {
            var secretKey = TempData["2FA_Secret"] as string;
            if (string.IsNullOrEmpty(secretKey))
            {
                return RedirectToAction("EnableTwoFactor");
            }

            if (!ModelState.IsValid)
            {
                // Regenerate QR code
                var user = await _userManager.GetUserAsync(User);
                var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user!.Email!, secretKey);
                var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
                
                model.SecretKey = secretKey;
                model.QrCodeImageBase64 = Convert.ToBase64String(qrCodeImage);
                model.ManualEntryKey = secretKey;
                
                TempData.Keep("2FA_Secret");
                return View(model);
            }

            // Validate the code
            if (!_twoFactorService.ValidateToken(secretKey, model.VerificationCode))
            {
                ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
                
                // Regenerate QR code
                var user = await _userManager.GetUserAsync(User);
                var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user!.Email!, secretKey);
                var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
                
                model.SecretKey = secretKey;
                model.QrCodeImageBase64 = Convert.ToBase64String(qrCodeImage);
                model.ManualEntryKey = secretKey;
                
                TempData.Keep("2FA_Secret");
                return View(model);
            }

            // Enable 2FA
            var currentUser = await _userManager.GetUserAsync(User);
            currentUser!.TwoFactorSecret = secretKey;
            currentUser.IsTwoFactorEnabled = true;
            await _userManager.UpdateAsync(currentUser);

            await _auditLogService.LogActivityAsync(currentUser.Id, "EnableTwoFactor", "Account", "2FA enabled successfully");
            _logger.LogInformation($"User {currentUser.Email} enabled 2FA");

            TempData["SuccessMessage"] = "Two-factor authentication has been enabled successfully.";
            return RedirectToAction("Profile", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.TwoFactorSecret = null;
            user.IsTwoFactorEnabled = false;
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.UpdateAsync(user);

            await _auditLogService.LogActivityAsync(user.Id, "DisableTwoFactor", "Account", "2FA disabled");
            _logger.LogInformation($"User {user.Email} disabled 2FA");

            TempData["SuccessMessage"] = "Two-factor authentication has been disabled.";
            return RedirectToAction("Profile", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
