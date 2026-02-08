namespace AceJobAgency.Services
{
    public interface ITwoFactorService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secretKey);
        byte[] GenerateQrCodeImage(string qrCodeUri);
        bool ValidateToken(string secretKey, string token);
    }
}
