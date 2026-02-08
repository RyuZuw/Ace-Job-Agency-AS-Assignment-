# Ace Job Agency - Technical Report

## Project Overview

This document provides a comprehensive technical report on the Ace Job Agency Membership Service, a secure .NET Core 8.0 Web Application implementing robust membership registration, authentication and advanced security features. All security checklist items have been implemented and tested.

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Security Features Implementation](#security-features-implementation)
3. [Database Schema](#database-schema)
4. [Configuration Guide](#configuration-guide)
5. [Security Checklist (Annex A)](#security-checklist-annex-a)

---

## Architecture Overview

### Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8.0 |
| Web Framework | ASP.NET Core MVC |
| Database | SQL Server (LocalDB) |
| ORM | Entity Framework Core 8.0 |
| Authentication | ASP.NET Core Identity |
| Email | SMTP (Mailtrap sandbox) |
| Anti-Bot | Google reCAPTCHA v3 |
| UI Framework | Bootstrap 5.3 |

### Project Structure

```
AceJobAgency/
├── Controllers/          # MVC Controllers
│   ├── AccountController.cs    # Authentication & user management
│   ├── HomeController.cs       # Main application pages (profile, sessions)
│   └── StatusCodeController.cs # Custom error page handler
├── Models/               # Entity models
│   ├── ApplicationUser.cs      # Extended Identity user model
│   ├── AuditLog.cs             # Audit trail entity
│   ├── PasswordHistory.cs      # Password history tracking entity
│   ├── UserSession.cs          # Session management entity
│   └── ErrorViewModel.cs       # Error view model
├── ViewModels/           # View models for forms
│   ├── RegisterViewModel.cs    # Registration with validation attributes
│   ├── LoginViewModel.cs       # Login form model
│   ├── TwoFactorViewModel.cs   # 2FA verification model
│   ├── ForgotPasswordViewModel.cs
│   ├── ChangePasswordViewModel.cs
│   └── ProfileViewModel.cs
├── Services/             # Business logic services
│   ├── EncryptionService.cs    # AES-256 encryption with random IV + HMAC hashing
│   ├── AuditLogService.cs      # Activity logging service
│   ├── PasswordHistoryService.cs # Password history & age enforcement
│   ├── RecaptchaService.cs     # Google reCAPTCHA v3 validation
│   ├── EmailService.cs         # SMTP email service (Mailtrap)
│   ├── TwoFactorService.cs     # TOTP 2FA service
│   └── SessionService.cs       # Database-backed session management
├── Middleware/           # Custom middleware
│   └── ConcurrentSessionMiddleware.cs  # Single-session enforcement
├── Data/                 # Database context & seeding
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs
├── Views/                # Razor views
│   ├── Account/          # Login, Register, 2FA, Password forms
│   ├── Home/             # Index, Profile pages
│   └── Shared/           # Layout, Error pages, ValidationScripts
└── wwwroot/              # Static files (CSS, JS, uploads)
```

---

## Security Features Implementation

### 1. Registration and User Data Management

#### 1.1 Saving Member Info into the Database
- **Files**: `Controllers/AccountController.cs` (Register action), `Models/ApplicationUser.cs`
- **Implementation**: The `Register` POST action collects all required fields (First Name, Last Name, Gender, NRIC, Email, Password, Date of Birth, Resume, Who Am I) and persists them via ASP.NET Core Identity's `UserManager.CreateAsync()`. Entity Framework Core handles all database operations with parameterized queries.
- **Fields stored**: FirstName, LastName, Gender, NRIC (encrypted), Email, PasswordHash (Identity-managed), DateOfBirth, ResumePath, WhoAmI (HTML-encoded), LastPasswordChange.

#### 1.2 Duplicate Email Detection
- **File**: `Controllers/AccountController.cs` (Register action)
- **Implementation**: Before creating a new user, the system checks:
  ```csharp
  var existingUser = await _userManager.FindByEmailAsync(model.Email);
  if (existingUser != null)
  {
      ModelState.AddModelError("Email", "An account with this email already exists.");
      return View(model);
  }
  ```
- **Additional**: Identity option `options.User.RequireUniqueEmail = true` enforces uniqueness at the framework level.

#### 1.3 Strong Password Requirements
- **Files**: `Program.cs` (Identity configuration), `ViewModels/RegisterViewModel.cs`, `Views/Account/Register.cshtml`
- **Server-side** (Identity configuration in `Program.cs`):
  ```csharp
  options.Password.RequireDigit = true;
  options.Password.RequiredLength = 12;
  options.Password.RequireNonAlphanumeric = true;
  options.Password.RequireUppercase = true;
  options.Password.RequireLowercase = true;
  options.Password.RequiredUniqueChars = 1;
  ```
- **Server-side** (ViewModel validation in `RegisterViewModel.cs`):
  ```csharp
  [StringLength(100, MinimumLength = 12, ErrorMessage = "Password must be at least 12 characters long")]
  [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{12,}$",
      ErrorMessage = "Password must contain at least one uppercase letter, ...")]
  ```
- **Client-side** (JavaScript in `Register.cshtml`): A real-time password strength indicator checks for all 5 criteria (length >= 12, lowercase, uppercase, digit, special char). The **Register button is disabled** until all criteria are met and the strength bar shows "Strong" (green). This provides immediate visual feedback.

#### 1.4 Encryption of Sensitive User Data (NRIC)
- **File**: `Services/EncryptionService.cs`
- **Algorithm**: AES-256-CBC with PBKDF2 key derivation (10,000 iterations, SHA-256)
- **Random IV**: Each encryption generates a cryptographically random 16-byte IV. The IV is prepended to the ciphertext and the combined output is Base64-encoded with a `v2:` prefix for versioning.
- **Backward compatibility**: Legacy records encrypted with a static IV (no prefix) can still be decrypted.
- **HMAC hashing**: A deterministic `ComputeHash()` method (HMAC-SHA256) enables duplicate NRIC detection without comparing ciphertext directly (since random IVs produce different ciphertext each time).
  ```csharp
  public string Encrypt(string plainText)
  {
      using (var aes = Aes.Create())
      {
          aes.Key = DeriveKey(_key);
          aes.GenerateIV();  // Random IV per encryption
          // IV prepended to ciphertext, result prefixed with "v2:"
      }
  }
  ```

#### 1.5 Password Hashing and Storage
- **Technology**: ASP.NET Core Identity automatically hashes passwords using PBKDF2 with HMAC-SHA256, 128-bit salt, 100,000 iterations (Identity v3 format). No plaintext passwords are ever stored.

#### 1.6 File Upload Restrictions
- **Files**: `Controllers/AccountController.cs` (Register action), `ViewModels/RegisterViewModel.cs`
- **Server-side validation** (controller):
  - Extension whitelist: `.pdf`, `.docx` only
  - Content-type validation: `application/pdf`, `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
  - Maximum file size: 5 MB
- **Client-side validation** (view model):
  - Custom `[AllowedExtensions]` attribute for `.pdf` and `.docx`
  - Custom `[MaxFileSize]` attribute for 5 MB limit
  - HTML `accept=".pdf,.docx"` attribute on the file input
- **Secure storage**: Files are renamed to a GUID-based filename to prevent path traversal attacks.

---

### 2. Session Management

#### 2.1 Secure Session Creation on Login
- **Files**: `Controllers/AccountController.cs`, `Services/SessionService.cs`
- **Implementation**: Upon successful authentication, a new `UserSession` record is created in the database with a unique GUID session ID, the user's IP address, user agent, and timestamps.
  ```csharp
  var sessionId = Guid.NewGuid().ToString();
  HttpContext.Session.SetString("SessionId", sessionId);
  await _sessionService.CreateSessionAsync(user.Id, sessionId);
  ```

#### 2.2 Session Timeout (20 Minutes)
- **Files**: `Program.cs`, `Services/SessionService.cs`, `wwwroot/js/site.js`
- **Server-side**: 
  - ASP.NET session cookie: `options.IdleTimeout = TimeSpan.FromMinutes(20)`
  - Authentication cookie: `options.ExpireTimeSpan = TimeSpan.FromMinutes(20)` with sliding expiration
  - Database-level: `SessionService.ValidateSessionAsync()` checks for 20-minute inactivity window
- **Client-side** (`site.js`): JavaScript timer warns the user 2 minutes before session expiry via a Bootstrap modal, then auto-redirects to the login page after 20 minutes of inactivity. Mouse movement, key presses, and clicks reset the timer.

#### 2.3 Session Timeout Redirect
- **Files**: `Middleware/ConcurrentSessionMiddleware.cs`, `wwwroot/js/site.js`
- On session expiry, the user is signed out and redirected to `/Account/Login?error=SessionExpired`.

#### 2.4 Detect and Handle Multiple Logins
- **Files**: `Controllers/AccountController.cs`, `Services/SessionService.cs`, `Middleware/ConcurrentSessionMiddleware.cs`
- **Single-session enforcement**: After a new session is created (login, 2FA verification, or registration), all **other** active sessions for that user are terminated:
  ```csharp
  await _sessionService.CreateSessionAsync(user.Id, sessionId);
  await _sessionService.TerminateAllSessionsAsync(user.Id, sessionId);
  ```
- **Middleware validation**: On every request, `ConcurrentSessionMiddleware` validates the current session against the database. If the session has been terminated (by a newer login), the user is signed out using `IdentityConstants.ApplicationScheme` and redirected to login.
- **Manual session management**: Users can view all active sessions on their Profile page and terminate individual sessions or all other sessions.

---

### 3. Login/Logout Security

#### 3.1 Login Functionality
- **File**: `Controllers/AccountController.cs`
- Uses `UserManager.CheckPasswordAsync()` for credential verification (avoids Identity's built-in 2FA check so we handle it custom).
- On success: resets failed access count, checks password expiry, checks 2FA status, then signs in.
- Login form disables browser autofill (`autocomplete="off"` on form and email, `autocomplete="new-password"` on password).

#### 3.2 Rate Limiting and Account Lockout
- **File**: `Program.cs`
- **Account lockout** (Identity):
  ```csharp
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
  options.Lockout.MaxFailedAccessAttempts = 3;
  options.Lockout.AllowedForNewUsers = true;
  ```
  - After 3 failed login attempts, the account is locked for 30 minutes.
  - Lockout is **automatic** — the account recovers after the lockout period expires.
- **Rate limiting** (ASP.NET Core Rate Limiter):
  - Login endpoint: 20 requests per minute (`[EnableRateLimiting("login")]`)
  - Register endpoint: 10 requests per 5 minutes (`[EnableRateLimiting("register")]`)

#### 3.3 Secure Logout
- **File**: `Controllers/AccountController.cs` (Logout action)
- **Implementation**:
  1. Terminates the user's session in the database (`SessionService.TerminateSessionAsync`)
  2. Signs out the user from Identity (`SignInManager.SignOutAsync`)
  3. Clears the server-side session (`HttpContext.Session.Clear()`)
  4. Logs the logout activity in the audit trail
  5. Redirects to the Login page

#### 3.4 Audit Logging
- **Files**: `Services/AuditLogService.cs`, `Models/AuditLog.cs`
- **Logged activities**: Login (success/fail), Logout, Registration, Password Change, Password Reset, 2FA Enable/Disable, Session Termination, Resume Download
- **Data captured**: UserId, Action name, Controller, Description, IP Address, User Agent, Timestamp, Success/Failure status, Error Message
- **Displayed on Profile**: The 10 most recent activity log entries are shown on the user's Profile page.

#### 3.5 Homepage After Login
- **File**: `Controllers/HomeController.cs`
- After successful login, user is redirected to the Profile page which displays: First Name, Last Name, Email, Decrypted NRIC, Gender, Date of Birth, Resume download link, Who Am I text, 2FA status, Last Password Change date, Recent Activity log, and Active Sessions.

---

### 4. Anti-Bot Protection (Google reCAPTCHA v3)

- **Files**: `Services/RecaptchaService.cs`, `Views/Account/Register.cshtml`, `Views/Shared/_Layout.cshtml`
- **Technology**: Google reCAPTCHA v3 (invisible, score-based)
- **Score threshold**: 0.5 (blocks suspicious automated traffic)
- **Client-side**: reCAPTCHA script loaded via `_Layout.cshtml`. On form submit, `grecaptcha.execute()` generates a token which is placed in a hidden form field.
- **Server-side**: The `RecaptchaService.ValidateTokenAsync()` method sends the token to Google's verification endpoint and checks the score.
  ```csharp
  var recaptchaValid = await _recaptchaService.ValidateTokenAsync(model.RecaptchaToken);
  if (!recaptchaValid)
  {
      ModelState.AddModelError(string.Empty, "reCaptcha verification failed.");
      return View(model);
  }
  ```

---

### 5. Input Validation and Sanitization

#### 5.1 SQL Injection Prevention
- **Method**: Entity Framework Core exclusive — all database operations use LINQ/EF Core which automatically generates parameterized queries. No raw SQL is used anywhere in the application.

#### 5.2 Cross-Site Request Forgery (CSRF) Protection
- **File**: `Program.cs`
- **Global filter**: `AutoValidateAntiforgeryTokenAttribute` is registered globally, requiring anti-forgery tokens on all POST/PUT/DELETE requests automatically.
  ```csharp
  options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
  ```
- **Views**: All forms include `@Html.AntiForgeryToken()` which generates the hidden token field.

#### 5.3 Cross-Site Scripting (XSS) Prevention
- **Razor encoding**: All Razor `@` expressions are automatically HTML-encoded by default.
- **Content Security Policy** (`Program.cs` middleware):
  ```csharp
  context.Response.Headers.Append("Content-Security-Policy", 
      "default-src 'self'; " +
      "script-src 'self' https://www.google.com https://www.gstatic.com https://cdn.jsdelivr.net 'unsafe-inline'; " +
      "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdn.jsdelivr.net; " +
      "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
      "img-src 'self' data: https:; " +
      "frame-src https://www.google.com;");
  ```
- **Security headers** (`Program.cs`):
  - `X-Content-Type-Options: nosniff` — prevents MIME sniffing
  - `X-Frame-Options: DENY` — prevents clickjacking
  - `X-XSS-Protection: 1; mode=block` — enables browser XSS filter
  - `Referrer-Policy: strict-origin-when-cross-origin`

#### 5.4 Input Validation (Client-Side and Server-Side)
- **Server-side** (`ViewModels/RegisterViewModel.cs`):
  - `[Required]`, `[StringLength]`, `[EmailAddress]`, `[RegularExpression]` attributes on all fields
  - Custom `[AllowedExtensions]` and `[MaxFileSize]` attributes for file uploads
  - Custom `ValidateDateOfBirth` method (minimum age 16)
  - NRIC regex: `^[STFG]\d{7}[A-Z]$`
  - Name regex: `^[a-zA-Z\s'-]+$` (prevents script injection via names)
- **Client-side** (`Views/`, `wwwroot/js/site.js`):
  - jQuery Unobtrusive Validation (via `_ValidationScriptsPartial.cshtml`) mirrors all server-side rules
  - Real-time visual feedback: valid fields turn green, invalid fields turn red on blur
  - Password strength indicator with 5-criteria check

#### 5.5 Error Messages for Improper Input
- Each validation attribute has a custom `ErrorMessage` string (e.g., "First Name is required", "Password must be at least 12 characters long", "Only PDF and DOCX files are allowed").
- `ModelState.AddModelError()` is used for business-logic errors (e.g., "An account with this email already exists.").
- Validation summary and per-field validation messages are displayed in all forms via `<span asp-validation-for>` and `<div asp-validation-summary>`.

#### 5.6 Encoding Before Database Save
- **File**: `Controllers/AccountController.cs` (Register action)
- The `WhoAmI` field (free-text user input) is HTML-encoded before storage to prevent stored XSS:
  ```csharp
  WhoAmI = System.Net.WebUtility.HtmlEncode(model.WhoAmI),
  ```
- Other fields are constrained by regex patterns that prevent special characters. NRIC is encrypted. Email is validated. Names only allow letters/spaces/hyphens/apostrophes.

---

### 6. Error Handling

#### 6.1 Graceful Error Handling
- **File**: `Program.cs`
- **Development**: `app.UseDeveloperExceptionPage()` for debugging.
- **Production**: `app.UseExceptionHandler("/Home/Error")` redirects to a generic error page.
- **Status code pages**: `app.UseStatusCodePagesWithReExecute("/StatusCode/{0}")` catches 404, 403, and other HTTP status codes.

#### 6.2 Custom Error Pages
- **File**: `Controllers/StatusCodeController.cs`, `Views/Shared/StatusCodeError.cshtml`, `Views/Shared/Error.cshtml`
- **404 Not Found**: Displays "Page Not Found" with a search icon, description, and links to Home and Login.
- **403 Forbidden**: Displays "Access Forbidden" with a shield icon and description.
- **500 Internal Server Error**: Displays a generic "An Error Occurred" message via `Error.cshtml` with a Request ID (no sensitive info exposed).
- All error pages use consistent Bootstrap 5 card styling with appropriate color-coded icons.
- **Access Denied**: Dedicated `Views/Account/AccessDenied.cshtml` view for authorization failures.
- **Lockout page**: `Views/Account/Lockout.cshtml` for locked-out users.

---

### 7. Advanced Security Features

#### 7.1 Automatic Account Recovery After Lockout
- **File**: `Program.cs`
- The lockout period is set to 30 minutes. After this period, the account is automatically unlocked by ASP.NET Core Identity — no manual intervention is required.
  ```csharp
  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
  ```

#### 7.2 Password History (Max 2)
- **Files**: `Services/PasswordHistoryService.cs`, `Models/PasswordHistory.cs`
- **Configuration**: `PasswordPolicy:HistoryCount = 2` in `appsettings.json`
- The last 2 password hashes are stored in the `PasswordHistories` table.
- When changing or resetting a password, `IsPasswordInHistoryAsync()` verifies the new password against stored hashes using Identity's `PasswordHasher.VerifyHashedPassword()`.
- Old entries beyond the history count are automatically pruned.
- **Enforced in**:
  - `ChangePassword` action — checks history before allowing change
  - `ResetPassword` action — checks history before allowing reset

#### 7.3 Change Password
- **Files**: `Controllers/AccountController.cs` (ChangePassword action), `Views/Account/ChangePassword.cshtml`
- Requires current password verification.
- Checks minimum password age before allowing change.
- Checks password history (cannot reuse last 2 passwords).
- Updates `LastPasswordChange` timestamp.
- Adds new password hash to history.
- Re-signs in the user after successful change.

#### 7.4 Reset Password (Email Link)
- **Files**: `Controllers/AccountController.cs` (ForgotPassword, ResetPassword actions), `Services/EmailService.cs`
- **Flow**:
  1. User enters their email on the Forgot Password page.
  2. System generates an Identity password reset token.
  3. A reset link is sent via SMTP email (Mailtrap) with a styled HTML template.
  4. The link uses `HttpContext.Request.Scheme` and `HttpContext.Request.Host.Value` to dynamically construct the correct URL.
  5. User clicks the link and sets a new password (subject to history check).
  6. `LastPasswordChange` timestamp is updated.
- **Security**: The system does not reveal whether an email exists — the user always sees a confirmation page.
- **Email provider**: SMTP via `System.Net.Mail.SmtpClient` configured for Mailtrap sandbox:
  ```json
  {
    "Email": {
      "SmtpHost": "sandbox.smtp.mailtrap.io",
      "SmtpPort": 587,
      "SmtpUseSsl": true
    }
  }
  ```

#### 7.5 Password Age Policies
- **Files**: `Services/PasswordHistoryService.cs`, `Controllers/AccountController.cs`, `appsettings.json`
- **Minimum age** (`MinimumAgeDays: 1`): Prevents rapid password changes. Checked in the `ChangePassword` action via `IsPasswordAgeValidAsync()`.
- **Maximum age** (`MaximumAgeDays: 90`): Forces periodic password renewal. Checked at login — if the password has exceeded the maximum age, the user is temporarily signed in and redirected to the Change Password page with a "Your password has expired" message.

#### 7.6 Two-Factor Authentication (2FA)
- **Files**: `Services/TwoFactorService.cs`, `Controllers/AccountController.cs`, `Views/Account/EnableTwoFactor.cshtml`, `Views/Account/TwoFactor.cshtml`
- **Technology**: Time-based One-Time Password (TOTP) per RFC 6238
- **Setup flow**:
  1. User navigates to Enable 2FA from their Profile.
  2. System generates a 20-byte secret key and QR code.
  3. User scans QR code with an authenticator app (Google Authenticator, Microsoft Authenticator, Authy).
  4. User enters the 6-digit code to verify and activate 2FA.
- **Login flow**: After password verification, user is redirected to the 2FA page. The 6-digit code is validated with ±1 time-step tolerance (30-second windows).
- **Disable**: Users can disable 2FA from their Profile page (clears secret key).

---

### 8. General Security Best Practices

#### 8.1 Secure Cookie Configuration
- **File**: `Program.cs`
```csharp
options.Cookie.HttpOnly = true;                          // Prevents JavaScript access
options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // HTTPS enforcement in production
options.Cookie.SameSite = SameSiteMode.Strict;           // Prevents CSRF via cookies
```

#### 8.2 HTTPS and HSTS
- **File**: `Program.cs`
- In production: `app.UseHsts()` and `app.UseHttpsRedirection()` enforce HTTPS.
- Development runs on HTTP for convenience (`localhost:5002`).

#### 8.3 Access Controls
- **`[Authorize]`** attribute on all protected actions (Profile, ChangePassword, EnableTwoFactor, Logout, TerminateSession).
- **`[AllowAnonymous]`** only on Login, Register, ForgotPassword, ResetPassword endpoints.
- Role-based: "Member" role assigned on registration via `_userManager.AddToRoleAsync(user, "Member")`.

#### 8.4 Secure Coding Practices
- Anti-forgery tokens on all state-changing forms (globally enforced)
- No sensitive information in error messages
- GUID-based file naming prevents path traversal
- Session data stored server-side (database), not in cookies
- Encryption keys managed via configuration (not hardcoded)

---

## Database Schema

### Entity Relationship Diagram

```
[ApplicationUser] 1───* [PasswordHistory]
[ApplicationUser] 1───* [AuditLog]
[ApplicationUser] 1───* [UserSession]
```

### Table Definitions

#### ApplicationUser (Extended IdentityUser)
| Column | Type | Description |
|--------|------|-------------|
| Id | string (PK) | User identifier (GUID) |
| FirstName | nvarchar(50) | User's first name |
| LastName | nvarchar(50) | User's last name |
| NRIC | nvarchar(500) | AES-256 encrypted NRIC (v2: prefix with random IV) |
| Gender | nvarchar(10) | Gender |
| DateOfBirth | datetime | Date of birth |
| ResumePath | nvarchar(500) | Path to uploaded resume file |
| WhoAmI | nvarchar(1000) | HTML-encoded user description |
| TwoFactorSecret | nvarchar(100) | TOTP secret key (null if 2FA disabled) |
| IsTwoFactorEnabled | bit | 2FA enabled flag |
| LastPasswordChange | datetime | Timestamp of last password change |

#### AuditLog
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment log entry ID |
| UserId | nvarchar (FK) | User identifier |
| Action | nvarchar(100) | Action performed (Login, Logout, Register, etc.) |
| Controller | nvarchar(100) | Controller name |
| Description | nvarchar(500) | Detailed description |
| IpAddress | nvarchar(50) | Client IP address |
| UserAgent | nvarchar(500) | Client browser user agent |
| Timestamp | datetime | When action occurred (UTC) |
| IsSuccess | bit | Whether action succeeded |
| ErrorMessage | nvarchar(500) | Error details if failed |

#### PasswordHistory
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment history entry ID |
| UserId | nvarchar (FK) | User identifier |
| PasswordHash | nvarchar(500) | PBKDF2 hashed password |
| CreatedAt | datetime | When password was set (UTC) |

#### UserSession
| Column | Type | Description |
|--------|------|-------------|
| Id | int (PK) | Auto-increment session ID |
| UserId | nvarchar (FK) | User identifier |
| SessionId | nvarchar(100) | Unique session GUID |
| IpAddress | nvarchar(50) | Client IP address |
| UserAgent | nvarchar(500) | Client browser user agent |
| CreatedAt | datetime | Session creation time (UTC) |
| LastActivity | datetime | Last activity timestamp (UTC) |
| IsActive | bit | Whether session is currently active |
| ExpiredAt | datetime | When session was terminated/expired |

---

## Configuration Guide

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AceJobAgencyDB;Trusted_Connection=True;"
  },
  "Encryption": {
    "Key": "<64-char hex key for AES-256>"
  },
  "Recaptcha": {
    "SiteKey": "<Google reCAPTCHA v3 site key>",
    "SecretKey": "<Google reCAPTCHA v3 secret key>"
  },
  "Email": {
    "SmtpHost": "sandbox.smtp.mailtrap.io",
    "SmtpPort": 587,
    "SmtpUseSsl": true,
    "SmtpUser": "<Mailtrap SMTP username>",
    "SmtpPassword": "<Mailtrap SMTP password>",
    "FromEmail": "noreply@acejobagency.com",
    "FromName": "Ace Job Agency"
  },
  "PasswordPolicy": {
    "MinimumAgeDays": 1,
    "MaximumAgeDays": 90,
    "HistoryCount": 2
  }
}
```

### Setup Instructions

1. **Prerequisites**: Install .NET 8.0 SDK and SQL Server LocalDB.

2. **Build the application**:
   ```bash
   cd AceJobAgency
   dotnet restore
   dotnet build
   ```

3. **Apply database migrations** (auto-applied on startup):
   ```bash
   dotnet ef database update
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```
   Accessible at `http://localhost:5002`.

---

## Security Checklist (Annex A)

### Registration and User Data Management

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 1 | Save member info into database | `UserManager.CreateAsync()` persists all fields | `AccountController.cs` | ✅ |
| 2 | Check for duplicate email | `FindByEmailAsync()` + `RequireUniqueEmail = true` | `AccountController.cs`, `Program.cs` | ✅ |
| 3 | Minimum 12 characters | `RequiredLength = 12` | `Program.cs` | ✅ |
| 4 | Combination of lower, upper, number, special | `RequireDigit`, `RequireUppercase`, `RequireLowercase`, `RequireNonAlphanumeric` | `Program.cs` | ✅ |
| 5 | Password strength feedback | Real-time strength bar (Weak/Medium/Strong) with 5-criteria check | `Register.cshtml` | ✅ |
| 6 | Client-side and server-side password checks | JS strength indicator (client) + Identity rules + Regex attribute (server) | `Register.cshtml`, `RegisterViewModel.cs`, `Program.cs` | ✅ |
| 7 | Encrypt sensitive data (NRIC) | AES-256-CBC with random IV per record, PBKDF2 key derivation | `EncryptionService.cs` | ✅ |
| 8 | Password hashing | PBKDF2 HMAC-SHA256 (Identity v3 format) | ASP.NET Core Identity | ✅ |
| 9 | File upload restrictions (.docx, .pdf) | Extension whitelist, content-type validation, 5 MB max | `AccountController.cs`, `RegisterViewModel.cs` | ✅ |

### Session Management

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 10 | Secure session on login | Database-backed `UserSession` with GUID, IP, UserAgent | `SessionService.cs`, `AccountController.cs` | ✅ |
| 11 | Session timeout | 20 min idle timeout (server + client timer) | `Program.cs`, `site.js` | ✅ |
| 12 | Redirect after timeout | Auto-redirect to `/Account/Login?error=SessionExpired` | `ConcurrentSessionMiddleware.cs`, `site.js` | ✅ |
| 13 | Handle multiple logins | Single-session enforcement: new login terminates all others | `AccountController.cs`, `SessionService.cs`, `ConcurrentSessionMiddleware.cs` | ✅ |

### Login/Logout Security

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 14 | Login functionality | `CheckPasswordAsync` + Identity sign-in | `AccountController.cs` | ✅ |
| 15 | Rate limiting / lockout | 3 attempts → 30 min lockout; rate limiter on endpoints | `Program.cs` | ✅ |
| 16 | Secure logout | `SignOutAsync` + `Session.Clear` + terminate DB session | `AccountController.cs` | ✅ |
| 17 | Audit logging | All user activities logged with IP, UserAgent, timestamps | `AuditLogService.cs` | ✅ |
| 18 | Redirect to homepage after login | Redirects to Profile page displaying user info | `AccountController.cs`, `HomeController.cs` | ✅ |

### Anti-Bot Protection

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 19 | Google reCAPTCHA v3 | Invisible score-based verification (threshold 0.5) | `RecaptchaService.cs`, `Register.cshtml` | ✅ |

### Input Validation and Sanitization

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 20 | Prevent SQL injection | Entity Framework Core parameterized queries (no raw SQL) | All controllers | ✅ |
| 21 | CSRF protection | Global `AutoValidateAntiForgeryToken` filter + `@Html.AntiForgeryToken()` | `Program.cs`, all views | ✅ |
| 22 | Prevent XSS | Razor auto-encoding + CSP headers + security headers | `Program.cs`, views | ✅ |
| 23 | Input sanitization & validation | Regex patterns, `[StringLength]`, `[EmailAddress]`, custom validators | `RegisterViewModel.cs`, `LoginViewModel.cs` | ✅ |
| 24 | Client-side and server-side validation | jQuery Unobtrusive Validation (client) + DataAnnotations (server) | ViewModels, views | ✅ |
| 25 | Error/warning messages | Custom `ErrorMessage` on all validation attributes + `ModelState` errors | ViewModels, controllers | ✅ |
| 26 | Encoding before database save | `WebUtility.HtmlEncode()` on free-text fields; regex constraints on others | `AccountController.cs` | ✅ |

### Error Handling

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 27 | Graceful error handling | `UseExceptionHandler` (production) + `UseStatusCodePagesWithReExecute` | `Program.cs` | ✅ |
| 28 | Custom 404 page | Styled "Page Not Found" with icon and navigation links | `StatusCodeError.cshtml` | ✅ |
| 29 | Custom 403 page | Styled "Access Forbidden" with shield icon | `StatusCodeError.cshtml` | ✅ |

### Advanced Security Features

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 30 | Auto recovery after lockout | Identity auto-unlocks after `DefaultLockoutTimeSpan` (30 min) | `Program.cs` | ✅ |
| 31 | Password history (max 2) | Last 2 hashes stored and checked on change/reset | `PasswordHistoryService.cs` | ✅ |
| 32 | Change password | Verifies current password + min age + history check | `AccountController.cs` | ✅ |
| 33 | Reset password via email | SMTP email with reset link + token-based reset | `EmailService.cs`, `AccountController.cs` | ✅ |
| 34 | Min/max password age | Min: 1 day; Max: 90 days; enforced at change and login | `PasswordHistoryService.cs`, `AccountController.cs` | ✅ |
| 35 | Two-Factor Authentication | TOTP (RFC 6238) with QR code + authenticator app support | `TwoFactorService.cs`, `AccountController.cs` | ✅ |

### General Security Best Practices

| # | Requirement | Implementation | File(s) | Status |
|---|-------------|----------------|---------|--------|
| 36 | HTTPS | HSTS + HTTPS redirect in production | `Program.cs` | ✅ |
| 37 | Access controls | `[Authorize]` / `[AllowAnonymous]` + role-based auth | Controllers | ✅ |
| 38 | Secure cookies | HttpOnly, Secure, SameSite=Strict | `Program.cs` | ✅ |
| 39 | Security headers | CSP, X-Frame-Options, X-Content-Type-Options, X-XSS-Protection | `Program.cs` | ✅ |
| 40 | Logging and monitoring | Comprehensive audit log + .NET Core ILogger | `AuditLogService.cs`, all controllers | ✅ |

---

## Demo Walkthrough

### 1. Registration Demo
1. Navigate to `/Account/Register`.
2. Fill in all fields with valid data (NRIC format: S1234567A).
3. Observe password strength indicator updating in real time — the Register button is disabled until password is "Strong" (green).
4. Upload a `.pdf` or `.docx` resume (max 5 MB).
5. Submit — reCAPTCHA runs invisibly, user is created and signed in.
6. Try registering with the same email — error: "An account with this email already exists."
7. Try registering with the same NRIC — error: "An account with this NRIC already exists."

### 2. Login and Session Demo
1. Log out and navigate to `/Account/Login`.
2. Enter correct credentials — redirected to Profile page showing all user info.
3. Open another browser/incognito window and log in again — the first session is terminated.
4. Return to the first browser and click any link — redirected to Login (session invalidated).

### 3. Account Lockout Demo
1. Attempt 3 incorrect logins — account is locked with message "Account is locked out. Please try again later."
2. Wait 30 minutes (or reset via database) — account automatically unlocks.

### 4. Password Change Demo
1. Navigate to Change Password from the dropdown menu.
2. Enter current password and a new strong password.
3. Attempt to reuse the same password — error: "You cannot reuse a previous password."
4. After changing, try changing again immediately — error about minimum password age.

### 5. Password Reset Demo
1. Navigate to `/Account/ForgotPassword`.
2. Enter registered email address.
3. Check Mailtrap inbox — styled HTML email with reset link received.
4. Click the reset link — enter new password (subject to history check).
5. After reset, log in with the new password.

### 6. Two-Factor Authentication Demo
1. Log in and navigate to Profile.
2. Click "Enable Two-Factor Authentication".
3. Scan QR code with Google Authenticator or enter manual key.
4. Enter 6-digit code to verify and activate.
5. Log out and log in again — after password, prompted for 2FA code.
6. Enter valid code from authenticator app — access granted.

### 7. Error Handling Demo
1. Navigate to `/nonexistent` — custom 404 page displayed.
2. Access a protected page while logged out — redirected to Login.
3. Session timeout (20 min inactivity) — modal warning at 18 min, auto-redirect at 20 min.

### 8. Input Validation Demo
1. Try submitting registration with empty fields — all validation errors shown.
2. Try entering invalid NRIC format (e.g., "12345") — regex validation error.
3. Try uploading a `.txt` file — "Only PDF and DOCX files are allowed."
4. Try a password shorter than 12 chars — strength stays "Weak", button stays disabled.

---

## Conclusion

The Ace Job Agency Membership Service implements a comprehensive defense-in-depth security architecture addressing all 40+ requirements in the security checklist. Key highlights include:

- **Multi-layered authentication**: Password + optional TOTP 2FA + reCAPTCHA
- **Hardened encryption**: AES-256 with random IV per record, PBKDF2 key derivation
- **Single-session enforcement**: New logins terminate all prior sessions
- **Full audit trail**: Every security-relevant action logged with IP, timestamp, and status
- **Robust input validation**: Dual client/server validation with custom error messages
- **Password lifecycle management**: History, minimum/maximum age, complexity, and strength gating
- **Graceful error handling**: Custom pages for 404, 403, 500 with no sensitive data leakage

---

**Document Version**: 2.0  
**Last Updated**: February 2026  
**Framework**: ASP.NET Core 8.0 MVC  
**Application URL**: http://localhost:5002
