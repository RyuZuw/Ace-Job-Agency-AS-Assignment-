using System.Net;
using System.Net.Mail;
using System.Text.Encodings.Web;

namespace AceJobAgency.Services
{
    public class EmailService : IEmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly bool _smtpUseSsl;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration)
        {
            _smtpHost = configuration["Email:SmtpHost"] ?? "mail.smtp2go.com";
            _smtpPort = int.TryParse(configuration["Email:SmtpPort"], out var port) ? port : 587;
            _smtpUseSsl = bool.TryParse(configuration["Email:SmtpUseSsl"], out var useSsl) ? useSsl : true;
            _smtpUser = configuration["Email:SmtpUser"] ?? "apikey";
            _smtpPassword = configuration["Email:SmtpPassword"]
                ?? Environment.GetEnvironmentVariable("SMTP2GO_API_KEY")
                ?? throw new ArgumentNullException("SMTP2GO API key not configured");
            _fromEmail = configuration["Email:FromEmail"] ?? "noreply@acejobagency.com";
            _fromName = configuration["Email:FromName"] ?? "Ace Job Agency";
        }

        private async Task SendEmailInternalAsync(string toEmail, string subject, string htmlContent)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_fromEmail, _fromName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = htmlContent;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(_smtpHost, _smtpPort);
            client.EnableSsl = _smtpUseSsl;
            client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);

            await client.SendMailAsync(message);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var safeResetLink = HtmlEncoder.Default.Encode(resetLink);
            var subject = "Password Reset Request - Ace Job Agency";
            var htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; margin: 20px 0; }}
                        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 30px; 
                                  text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; color: #6c757d; font-size: 12px; margin-top: 30px; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 5px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Ace Job Agency</h1>
                        </div>
                        <div class='content'>
                            <h2>Password Reset Request</h2>
                            <p>Hello,</p>
                            <p>We received a request to reset your password for your Ace Job Agency account.</p>
                            <p>Click the button below to reset your password:</p>
                            <center>
                                <a href='{safeResetLink}' class='button'>Reset Password</a>
                            </center>
                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all;'><a href='{safeResetLink}'>{safeResetLink}</a></p>
                            <div class='warning'>
                                <strong>Important:</strong> This link will expire in 1 hour for security reasons.
                            </div>
                        </div>
                        <div class='footer'>
                            <p>If you did not request a password reset, please ignore this email or contact support.</p>
                            <p>&copy; 2024 Ace Job Agency. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            await SendEmailInternalAsync(toEmail, subject, htmlContent);
        }
    }
}
