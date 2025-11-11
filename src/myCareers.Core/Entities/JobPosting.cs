using myCareers.Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace myCareers.Core.Entities
{
    public class JobPosting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecruiterId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(100)]
        public string? SalaryRange { get; set; }

        public EmploymentType EmploymentType { get; set; }

        public ExperienceLevel ExperienceLevel { get; set; }

        public JobStatus Status { get; set; } = JobStatus.Draft;

        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime ClosingDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedDate { get; set; }

        public DateTime? PublishedDate { get; set; }

        public int ViewCount { get; set; } = 0;

        public int ApplicationCount { get; set; } = 0;

        // Navigation properties
        [Required]
        [ForeignKey("RecruiterId")]
        public Recruiter Recruiter { get; set; } = null!;


        // Applications will reference this
        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}
