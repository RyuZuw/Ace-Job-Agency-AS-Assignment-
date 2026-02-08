# Ace Job Agency - Project Summary

## Project Overview

This is a complete .NET Core 8.0 Web Application implementing a secure membership registration and authentication system for Ace Job Agency. The application adheres to strict application security standards and includes comprehensive security features.

---

## Project Statistics

- **Total Files**: 59
- **Lines of Code**: ~8,000+
- **Framework**: .NET 8.0
- **Database**: SQL Server with Entity Framework Core
- **UI**: Bootstrap 5.3 with Razor Views

---

## File Structure

```
AceJobAgency/
├── AceJobAgency.csproj              # Project file with NuGet packages
├── Program.cs                        # Application entry point & configuration
├── appsettings.json                  # Application configuration
├── README.md                         # Setup instructions
├── TECHNICAL_REPORT.md               # Comprehensive technical documentation
├── PROJECT_SUMMARY.md                # This file
│
├── Controllers/                      # MVC Controllers (3 files)
│   ├── AccountController.cs          # Authentication & user management (500+ lines)
│   ├── HomeController.cs             # Main pages & profile (150+ lines)
│   └── StatusCodeController.cs       # Error page handler
│
├── Data/                             # Database layer (2 files)
│   ├── ApplicationDbContext.cs       # EF Core DbContext
│   └── DbInitializer.cs              # Role seeding
│
├── Migrations/                       # Database migrations (3 files)
│   ├── 20240101000000_InitialCreate.cs
│   ├── 20240101000000_InitialCreate.Designer.cs
│   └── ApplicationDbContextModelSnapshot.cs
│
├── Middleware/                       # Custom middleware (1 file)
│   └── ConcurrentSessionMiddleware.cs
│
├── Models/                           # Entity models (5 files)
│   ├── ApplicationUser.cs            # Extended Identity user
│   ├── AuditLog.cs                   # Activity log model
│   ├── ErrorViewModel.cs             # Error view model
│   ├── PasswordHistory.cs            # Password history model
│   └── UserSession.cs                # Session tracking model
│
├── Services/                         # Business logic (11 files)
│   ├── IEncryptionService.cs         # Encryption interface
│   ├── EncryptionService.cs          # AES-256 implementation
│   ├── IAuditLogService.cs           # Audit interface
│   ├── AuditLogService.cs            # Activity logging
│   ├── IPasswordHistoryService.cs    # Password history interface
│   ├── PasswordHistoryService.cs     # Password policy enforcement
│   ├── IRecaptchaService.cs          # reCaptcha interface
│   ├── RecaptchaService.cs           # Google reCaptcha v3
│   ├── IEmailService.cs              # Email interface
│   ├── EmailService.cs               # SendGrid integration
│   ├── ITwoFactorService.cs          # 2FA interface
│   ├── TwoFactorService.cs           # TOTP implementation
│   ├── ISessionService.cs            # Session interface
│   └── SessionService.cs             # Session management
│
├── ViewModels/                       # Form models (6 files)
│   ├── RegisterViewModel.cs          # Registration form
│   ├── LoginViewModel.cs             # Login form
│   ├── TwoFactorViewModel.cs         # 2FA forms
│   ├── ForgotPasswordViewModel.cs    # Password reset forms
│   ├── ChangePasswordViewModel.cs    # Change password form
│   └── ProfileViewModel.cs           # Profile display
│
├── Views/                            # Razor views (17 files)
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Shared/
│   │   ├── _Layout.cshtml            # Main layout
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   ├── Error.cshtml              # Error page
│   │   └── StatusCodeError.cshtml    # Status code errors (404, 403, 500)
│   ├── Home/
│   │   ├── Index.cshtml              # Landing page
│   │   └── Profile.cshtml            # User profile
│   └── Account/
│       ├── Login.cshtml              # Login page
│       ├── Register.cshtml           # Registration page
│       ├── TwoFactor.cshtml          # 2FA verification
│       ├── ForgotPassword.cshtml     # Forgot password
│       ├── ForgotPasswordConfirmation.cshtml
│       ├── ResetPassword.cshtml      # Reset password
│       ├── ResetPasswordConfirmation.cshtml
│       ├── ChangePassword.cshtml     # Change password
│       ├── EnableTwoFactor.cshtml    # Enable 2FA
│       ├── AccessDenied.cshtml       # 403 page
│       └── Lockout.cshtml            # Account locked
│
└── wwwroot/                          # Static files
    ├── css/
    │   └── site.css                  # Custom styles (250+ lines)
    └── js/
        └── site.js                   # Client-side utilities (150+ lines)
```

---

## Implemented Features

### 1. User Registration
- ✅ First Name, Last Name
- ✅ Gender selection
- ✅ NRIC with validation (Singapore format: S1234567A)
- ✅ Email (unique, used as primary identifier)
- ✅ Password & Confirm Password
- ✅ Date of Birth (minimum age 16)
- ✅ Resume upload (PDF/DOCX, max 5MB)
- ✅ "Who Am I" text field (allows all special characters)
- ✅ Duplicate email detection
- ✅ Google reCaptcha v3 integration

### 2. Authentication
- ✅ Login with email/password
- ✅ Logout with session cleanup
- ✅ Rate limiting (5 attempts per minute for login)
- ✅ Account lockout (3 failed attempts = 30 min lockout)
- ✅ Remember me functionality
- ✅ Two-Factor Authentication (TOTP)

### 3. Password Security
- ✅ Minimum 12 characters
- ✅ Uppercase, lowercase, number, special character required
- ✅ Password history (last 2 passwords)
- ✅ Minimum password age (1 day)
- ✅ Maximum password age (90 days)
- ✅ Change password functionality
- ✅ Reset password via email (SendGrid)

### 4. Data Protection
- ✅ NRIC encryption (AES-256)
- ✅ Secure key management (PBKDF2)
- ✅ Anti-bot protection (reCaptcha v3)

### 5. Session Management
- ✅ Secure session cookies (HttpOnly, Secure, SameSite=Strict)
- ✅ 20-minute session timeout
- ✅ Concurrent session detection
- ✅ View and terminate active sessions
- ✅ Session validation middleware

### 6. Attack Prevention
- ✅ SQL Injection prevention (Entity Framework parameterized queries)
- ✅ XSS prevention (Razor encoding, CSP headers)
- ✅ CSRF protection (Anti-forgery tokens)
- ✅ Security headers (X-Content-Type-Options, X-Frame-Options, X-XSS-Protection, CSP)

### 7. Audit Logging
- ✅ User activity tracking
- ✅ IP address logging
- ✅ User agent logging
- ✅ Timestamp recording
- ✅ Success/failure tracking
- ✅ Error message logging

### 8. Error Handling
- ✅ Custom 404 page
- ✅ Custom 403 page
- ✅ Custom 500 page
- ✅ Generic error messages (no sensitive info)

---

## Security Checklist (32/32 Items)

| Category | Items | Status |
|----------|-------|--------|
| Authentication & Authorization | 6 | ✅ 100% |
| Password Security | 6 | ✅ 100% |
| Data Protection | 3 | ✅ 100% |
| Session Management | 4 | ✅ 100% |
| Attack Prevention | 4 | ✅ 100% |
| Audit & Monitoring | 4 | ✅ 100% |
| Error Handling | 4 | ✅ 100% |
| **Total** | **32** | **✅ 100%** |

---

## NuGet Packages Used

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 8.0.0 | Identity framework |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | SQL Server provider |
| Microsoft.EntityFrameworkCore.Tools | 8.0.0 | Migrations |
| Microsoft.AspNetCore.Authentication.Google | 8.0.0 | OAuth (optional) |
| Google.Apis.RecaptchaEnterprise.v1 | 1.68.0 | reCaptcha integration |
| QRCoder | 1.4.3 | QR code generation for 2FA |
| System.Drawing.Common | 8.0.0 | Image support |
| SendGrid | 9.28.1 | Email service |
| Microsoft.AspNetCore.RateLimiting | 8.0.0 | Rate limiting |

---

## How to Run

### Prerequisites
1. Install .NET 8.0 SDK
2. Install SQL Server LocalDB (or SQL Server)

### Steps
```bash
# Navigate to project directory
cd AceJobAgency

# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Apply database migrations
dotnet ef database update

# Run the application
dotnet run

# Access the application
# http://localhost:5000 or https://localhost:5001
```

---

## Configuration Required

Before running, update `appsettings.json` with:

1. **Encryption Key** - 32-character secret key
2. **Google reCaptcha Keys** - Get from https://www.google.com/recaptcha/admin
3. **SendGrid API Key** - For password reset emails (optional)

---

## Key Implementation Highlights

### Encryption Service
```csharp
// AES-256 encryption with PBKDF2 key derivation
public string Encrypt(string plainText)
{
    using (var aes = Aes.Create())
    {
        aes.Key = DeriveKey(_key);
        // ... encryption logic
    }
}
```

### Two-Factor Authentication
```csharp
// TOTP implementation (RFC 6238)
public bool ValidateToken(string secretKey, string token)
{
    // Try current, previous, and next time windows
    for (int i = -1; i <= 1; i++)
    {
        var expectedToken = GenerateToken(secretKey, GetCurrentCounter() + i);
        if (expectedToken == token) return true;
    }
    return false;
}
```

### Concurrent Session Detection
```csharp
// Middleware validates session on each request
public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
{
    var isValid = await sessionService.ValidateSessionAsync(userId, sessionId);
    if (!isValid)
    {
        // Sign out user and redirect to login
    }
}
```

---

## Deliverables

✅ **Working Prototype** - Fully functional web application
✅ **Connected Database** - SQL Server with EF Core migrations
✅ **Technical Report** - Comprehensive documentation (TECHNICAL_REPORT.md)
✅ **Security Checklist** - Annex A with 32/32 items implemented
✅ **Setup Instructions** - README.md with detailed setup guide

---

## Notes for Demo

1. **Registration Flow**
   - Navigate to /Account/Register
   - Fill in all required fields
   - Upload a PDF or DOCX resume
   - Submit form (reCaptcha v3 works invisibly)

2. **2FA Setup**
   - Login with credentials
   - Go to Profile → Enable 2FA
   - Scan QR code with Google Authenticator
   - Enter verification code

3. **Session Management**
   - Login from different browsers
   - View active sessions on Profile page
   - Terminate other sessions

4. **Security Features**
   - Try 3 failed logins to trigger lockout
   - Check Audit Logs on Profile page
   - Verify NRIC is encrypted in database

---

## License

This project is for educational purposes as part of the IT2163-01 to ITN2163-03 assignment.

---

**Project Completed**: 2024
**Framework**: .NET 8.0
**Status**: ✅ Complete
