using myCareers.Core.Enums;

namespace myCareers.Application.DTOs.JobApplication
{
    public class JobApplicationDto
    {
        public int Id { get; set; }
        public int JobPostingId { get; set; }
        public int ApplicantId { get; set; }
        public ApplicationStatus Status { get; set; }
        public string StatusDisplay => Status.ToString();
        public DateTime AppliedDate { get; set; }
        public DateTime? ReviewedDate { get; set; }
        public DateTime? InterviewDate { get; set; }
        public string? ReviewNotes { get; set; }

        // Position Info
        public string JobTitle { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ReferenceNumber { get; set; } = string.Empty;

        // Applicant Info
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        // Document URLs - ADD THESE
        public string? ResumeUrl { get; set; }
        public string? IdDocumentUrl { get; set; }
        public string? QualificationCertificateUrl { get; set; }
        public string? TranscriptUrl { get; set; }
        public string? Z83FormUrl { get; set; }


        // Computed Properties
        public int DaysSinceApplied => (DateTime.UtcNow - AppliedDate).Days;
        public bool CanWithdraw => Status == ApplicationStatus.Submitted || Status == ApplicationStatus.UnderReview;
        public bool IsActive => Status != ApplicationStatus.Rejected && Status != ApplicationStatus.Withdrawn;
    }
}