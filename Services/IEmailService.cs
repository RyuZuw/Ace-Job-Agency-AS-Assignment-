namespace AceJobAgency.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlContent);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
    }
}
