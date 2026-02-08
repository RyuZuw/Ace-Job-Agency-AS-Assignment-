# Ace Job Agency - Membership Service

A secure .NET Core Web Application for Ace Job Agency providing robust membership registration and authentication with advanced security features.

## Features

### User Registration & Profile
- First Name, Last Name collection
- Gender selection
- NRIC (encrypted storage)
- Email-based authentication
- Date of Birth validation
- Resume upload (PDF/DOCX)
- "Who Am I" description field

### Security Features
- **Multi-Factor Authentication (2FA)** with TOTP
- **Account Lockout** after 3 failed login attempts
- **Password Policy**: 12+ chars, complexity requirements
- **Password History**: Prevents reuse of last 2 passwords
- **Password Age**: Minimum 1 day, Maximum 90 days
- **NRIC Encryption**: AES-256 encryption
- **Google reCaptcha v3**: Bot protection
- **Rate Limiting**: Prevents brute force attacks
- **Secure Sessions**: HttpOnly, Secure, SameSite cookies
- **Concurrent Session Detection**: Track and manage multiple logins
- **Audit Logging**: Complete activity tracking

### Attack Prevention
- SQL Injection prevention (Entity Framework)
- Cross-Site Scripting (XSS) protection
- Cross-Site Request Forgery (CSRF) protection
- Security headers (CSP, X-Frame-Options, etc.)

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) or SQL Server
- Visual Studio 2022 or VS Code (optional)

## Setup Instructions

### 1. Clone/Download the Project

```bash
cd AceJobAgency
```

### 2. Configure Application Settings

Edit `appsettings.json` with your configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AceJobAgencyDB;Trusted_Connection=True;"
  },
  "Encryption": {
    "Key": "YourSecretEncryptionKey32CharsLong!!"
  },
  "Recaptcha": {
    "SiteKey": "your-recaptcha-site-key",
    "SecretKey": "your-recaptcha-secret-key"
  },
  "Email": {
    "SendGridApiKey": "your-sendgrid-api-key",
    "FromEmail": "noreply@acejobagency.com",
    "FromName": "Ace Job Agency"
  }
}
```

### 3. Get Google reCaptcha Keys

1. Go to [Google reCaptcha Admin Console](https://www.google.com/recaptcha/admin)
2. Register a new site (select reCaptcha v3)
3. Add your domain (localhost for development)
4. Copy Site Key and Secret Key to appsettings.json

### 4. Get SendGrid API Key (Optional - for password reset emails)

1. Sign up at [SendGrid](https://sendgrid.com/)
2. Create an API key
3. Add to appsettings.json

### 5. Build and Run

```bash
# Restore packages
dotnet restore

# Build the project
dotnet build

# Run database migrations
dotnet ef database update

# Run the application
dotnet run
```

### 6. Access the Application

Open your browser and navigate to:
- Application: `http://localhost:5000` or `https://localhost:5001`

## Default Configuration

### Password Policy
- Minimum length: 12 characters
- Requires uppercase letter
- Requires lowercase letter
- Requires number
- Requires special character
- History: Last 2 passwords cannot be reused
- Minimum age: 1 day
- Maximum age: 90 days

### Account Lockout
- Max failed attempts: 3
- Lockout duration: 30 minutes

### Session
- Timeout: 20 minutes of inactivity
- HttpOnly cookies
- Secure cookies (HTTPS)
- SameSite=Strict

## Project Structure

```
AceJobAgency/
├── Controllers/        # MVC Controllers
├── Models/             # Entity models
├── ViewModels/         # Form view models
├── Services/           # Business logic services
├── Middleware/         # Custom middleware
├── Data/               # Database context
├── Views/              # Razor views
├── wwwroot/            # Static files
├── appsettings.json    # Configuration
└── Program.cs          # Application entry point
```

## Key Services

### EncryptionService
- AES-256 encryption for NRIC
- PBKDF2 key derivation

### AuditLogService
- Logs all user activities
- Tracks IP addresses and user agents
- Success/failure tracking

### TwoFactorService
- TOTP implementation (RFC 6238)
- QR code generation
- Manual entry key support

### PasswordHistoryService
- Tracks password history
- Enforces password age policy
- Prevents password reuse

### SessionService
- Manages user sessions
- Detects concurrent logins
- Session termination

## Security Headers

The application sets the following security headers:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `X-XSS-Protection: 1; mode=block`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Content-Security-Policy` (comprehensive CSP)

## Testing

### Test Accounts

Create a test account through the registration page, or manually seed the database.

### Testing 2FA

1. Register a new account
2. Go to Profile → Enable 2FA
3. Scan QR code with Google Authenticator
4. Enter verification code to confirm

### Testing Account Lockout

1. Attempt login with wrong password 3 times
2. Account will be locked for 30 minutes
3. Or use "Forgot Password" to reset immediately

## Troubleshooting

### Database Connection Issues

```bash
# Ensure LocalDB is installed
sqllocaldb info

# Create instance if needed
sqllocaldb create MSSQLLocalDB

# Start instance
sqllocaldb start MSSQLLocalDB
```

### Certificate Issues (Development)

```bash
# Trust development certificate
dotnet dev-certs https --trust
```

### Migration Issues

```bash
# Remove existing migrations (if needed)
dotnet ef migrations remove

# Create new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Development

### Adding New Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running in Development Mode

```bash
dotnet run --environment Development
```

## Production Deployment

### Checklist

- [ ] Change Encryption Key
- [ ] Update reCaptcha keys for production domain
- [ ] Configure production email service
- [ ] Use production database connection string
- [ ] Enable HTTPS redirection
- [ ] Configure proper logging
- [ ] Set up error monitoring

### Environment Variables

For production, use environment variables instead of appsettings.json:

```bash
export ConnectionStrings__DefaultConnection="your-production-connection-string"
export Encryption__Key="your-production-encryption-key"
export Recaptcha__SiteKey="your-production-site-key"
export Recaptcha__SecretKey="your-production-secret-key"
export Email__SendGridApiKey="your-production-api-key"
```

## License

This project is for educational purposes as part of the IT2163-01 to ITN2163-03 assignment.

## Support

For issues or questions, please contact the development team.

---

**Version**: 1.0  
**Last Updated**: 2024
