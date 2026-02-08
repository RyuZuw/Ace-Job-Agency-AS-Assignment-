namespace AceJobAgency.Services
{
    public interface IRecaptchaService
    {
        Task<bool> ValidateTokenAsync(string token);
        string GetSiteKey();
    }
}
