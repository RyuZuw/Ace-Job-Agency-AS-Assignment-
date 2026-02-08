using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.ViewModels
{
    public class TwoFactorViewModel
    {
        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string Code { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }

        public bool RememberMe { get; set; }
    }

    public class EnableTwoFactorViewModel
    {
        public string QrCodeImageBase64 { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must be 6 digits")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = string.Empty;
    }
}
