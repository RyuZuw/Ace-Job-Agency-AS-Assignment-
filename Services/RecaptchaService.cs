using System.Text.Json;
using System.Text.Json.Serialization;

namespace AceJobAgency.Services
{
    public class RecaptchaService : IRecaptchaService
    {
        private readonly string _secretKey;
        private readonly string _siteKey;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(IConfiguration configuration, HttpClient httpClient, ILogger<RecaptchaService> logger)
        {
            _secretKey = configuration["Recaptcha:SecretKey"] ?? throw new ArgumentNullException("Recaptcha secret key not configured");
            _siteKey = configuration["Recaptcha:SiteKey"] ?? throw new ArgumentNullException("Recaptcha site key not configured");
            _httpClient = httpClient;
            _logger = logger;
        }

        public string GetSiteKey()
        {
            return _siteKey;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("reCaptcha token is empty");
                return false;
            }

            try
            {
                var response = await _httpClient.PostAsync(
                    $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                    null);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("reCaptcha API request failed");
                    return false;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonResponse);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize reCaptcha response");
                    return false;
                }

                // For reCaptcha v3, check score (0.0 to 1.0, where 1.0 is very likely a good interaction)
                if (result.Score.HasValue && result.Score.Value < 0.5)
                {
                    _logger.LogWarning($"reCaptcha score too low: {result.Score.Value}");
                    return false;
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reCaptcha token");
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
