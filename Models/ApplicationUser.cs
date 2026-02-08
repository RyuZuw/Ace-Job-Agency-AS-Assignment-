using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)] // Encrypted NRIC
        public string NRIC { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string Gender { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime DateOfBirth { get; set; }

        [StringLength(500)]
        [Display(Name = "Resume Path")]
        public string? ResumePath { get; set; }

        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Who Am I")]
        public string? WhoAmI { get; set; }

        [StringLength(100)]
        public string? TwoFactorSecret { get; set; }

        [Required]
        [Display(Name = "Last Password Change")]
        public DateTime LastPasswordChange { get; set; } = DateTime.UtcNow;

        [Display(Name = "Two-Factor Enabled")]
        public bool IsTwoFactorEnabled { get; set; } = false;

        // Navigation properties
        public virtual ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";
    }
}
