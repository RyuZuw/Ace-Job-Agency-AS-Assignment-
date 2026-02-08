using System.Text.Json;
using System.Text.Json.Serialization;

namespace AceJobAgency.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly string _secretKey;
        private readonly string _siteKey;
        private readonly HttpClient _httpClient;

        public RecaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _secretKey = configuration["Recaptcha:SecretKey"] ?? throw new ArgumentNullException("Recaptcha secret key not configured");
            _siteKey = configuration["Recaptcha:SiteKey"] ?? throw new ArgumentNullException("Recaptcha site key not configured");
            _httpClient = httpClient;
        }

        public string GetSiteKey()
        {
            return _siteKey;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                    null);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse);

                if (result == null)
                {
                    return false;
                }

                // For reCaptcha v3, check score (0.0 to 1.0, where 1.0 is very likely a good interaction)
                if (result.Score.HasValue && result.Score.Value < 0.5)
                {
                    return false;
                }

                return result.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public float? Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public List<string>? ErrorCodes { get; set; }
    }
}
