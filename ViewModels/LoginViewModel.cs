using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? RecaptchaToken { get; set; }

        public string? TwoFactorCode { get; set; }

        public bool RequiresTwoFactor { get; set; } = false;

        public string? ReturnUrl { get; set; }
    }
}
