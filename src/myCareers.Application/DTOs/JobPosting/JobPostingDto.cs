using myCareers.Core.Enums;

namespace myCareers.Application.DTOs.JobPosting
{
    public class JobPostingDto
    {
        public int Id { get; set; }
        public int RecruiterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? SalaryRange { get; set; }
        public EmploymentType EmploymentType { get; set; }
        public string EmploymentTypeDisplay => EmploymentType.ToString().Replace("FullTime", "Full-Time").Replace("PartTime", "Part-Time");
        public ExperienceLevel ExperienceLevel { get; set; }
        public string ExperienceLevelDisplay => ExperienceLevel.ToString().Replace("Level", " Level");
        public JobStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public bool IsActive { get; set; }
        public DateTime ClosingDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public int ViewCount { get; set; }
        public int ApplicationCount { get; set; }

        // Recruiter info
        public string RecruiterName { get; set; } = string.Empty;
        public string RecruiterEmail { get; set; } = string.Empty;

        // Computed properties
        public bool IsClosed => DateTime.UtcNow > ClosingDate;
        public int DaysUntilClosing => (ClosingDate.Date - DateTime.UtcNow.Date).Days;

        public bool PublishImmediately { get; set; } = false;
    }
}
