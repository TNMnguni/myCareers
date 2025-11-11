using myCareers.Core.Enums;
using System.ComponentModel.DataAnnotations;


namespace myCareers.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsEmailConfirmed { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        public Recruiter? Recruiter { get; set; }
        public Applicant? Applicant { get; set; }
    }
}
