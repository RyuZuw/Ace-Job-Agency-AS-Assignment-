using System.ComponentModel.DataAnnotations;

namespace AceJobAgency.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Display(Name = "Full Name")]
        public string FullName => $"{FirstName} {LastName}";

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "NRIC")]
        public string NRIC { get; set; } = string.Empty;

        [Display(Name = "Gender")]
        public string Gender { get; set; } = string.Empty;

        [Display(Name = "Date of Birth")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Display(Name = "Age")]
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age))
                    age--;
                return age;
            }
        }

        [Display(Name = "Resume")]
        public string? ResumePath { get; set; }

        [Display(Name = "Resume File Name")]
        public string? ResumeFileName { get; set; }

        [Display(Name = "Who Am I")]
        public string? WhoAmI { get; set; }

        [Display(Name = "Two-Factor Authentication")]
        public bool IsTwoFactorEnabled { get; set; }

        [Display(Name = "Last Password Change")]
        public DateTime LastPasswordChange { get; set; }

        [Display(Name = "Account Created")]
        public DateTime CreatedAt { get; set; }

        public List<ActivityLogViewModel> RecentActivity { get; set; } = new List<ActivityLogViewModel>();
    }

    public class ActivityLogViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
    }
}
