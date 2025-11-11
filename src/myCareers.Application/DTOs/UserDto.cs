using myCareers.Core.Enums;

namespace myCareers.Application.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public string RoleDisplayName => Role.ToString();

        // Recruiter profile info
        public RecruiterProfileDto? RecruiterProfile { get; set; }

        // Applicant profile info 
        public ApplicantProfileDto? ApplicantProfile { get; set; }
    }

    public class RecruiterProfileDto
    {
        public int Id { get; set; }
        public string? Department { get; set; }
        public string? JobTitle { get; set; }
        public string? EmployeeId { get; set; }
        public DateTime? StartDate { get; set; }
    }

    public class ApplicantProfileDto
    {
        public int Id { get; set; }
        // Add applicant-specific fields when Sprint 3 comes
    }
}
