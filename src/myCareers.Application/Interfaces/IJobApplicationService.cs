using myCareers.Application.DTOs.JobApplication;
using myCareers.Core.Enums;

namespace myCareers.Application.Interfaces
{
    public interface IJobApplicationService
    {
        Task<JobApplicationResult> CreateApplicationAsync(CreateJobApplicationDto dto, int applicantId);
        Task<JobApplicationResult> UpdateApplicationAsync(int applicationId, CreateJobApplicationDto dto, int applicantId);
        Task<JobApplicationResult> SubmitApplicationAsync(int applicationId, int applicantId);
        Task<JobApplicationResult> WithdrawApplicationAsync(int applicationId, int applicantId);
        Task<JobApplicationResult> UpdateApplicationStatusAsync(int applicationId, ApplicationStatus status);

        Task<JobApplicationDto?> GetApplicationByIdAsync(int id);
        Task<List<JobApplicationDto>> GetApplicationsByApplicantAsync(int applicantId);
        Task<List<JobApplicationDto>> GetApplicationsByJobPostingAsync(int jobPostingId);

        Task<ApplicationStatisticsDto> GetApplicationStatisticsAsync(int applicantId);
        Task<bool> HasAppliedToJobAsync(int applicantId, int jobPostingId);

        // ===== SPRINT 4: RECRUITER-SIDE METHODS =====

        /// <summary>
        /// Reject a single application with optional reason
        /// </summary>
        Task<JobApplicationResult> RejectApplicationAsync(int applicationId, string? rejectionReason);

        /// <summary>
        /// Bulk reject multiple applications for a job posting
        /// </summary>
        Task<JobApplicationResult> BulkRejectApplicationsAsync(int jobPostingId, List<int> applicationIds, string? rejectionReason);

        /// <summary>
        /// Accept application and optionally schedule interview
        /// </summary>
        Task<JobApplicationResult> AcceptApplicationAsync(int applicationId, DateTime? interviewDate, string? interviewNotes);

        /// <summary>
        /// Schedule or reschedule interview for an accepted/shortlisted application
        /// </summary>
        Task<JobApplicationResult> ScheduleInterviewAsync(int applicationId, DateTime interviewDate, string? interviewNotes);

        /// <summary>
        /// Permanently delete an application
        /// </summary>
        Task<JobApplicationResult> DeleteApplicationAsync(int applicationId);

        /// <summary>
        /// Update review notes for an application
        /// </summary>
        Task<JobApplicationResult> UpdateReviewNotesAsync(int applicationId, string reviewNotes);

        /// <summary>
        /// Get application statistics for a specific job posting (recruiter view)
        /// </summary>
        Task<JobApplicationStatisticsDto> GetJobApplicationStatisticsAsync(int jobPostingId);

        /// <summary>
        /// Get applications filtered by status for a job posting
        /// </summary>
        Task<List<JobApplicationDto>> GetApplicationsByStatusAsync(int jobPostingId, ApplicationStatus status);

        /// <summary>
        /// Check if application can be rejected (business rule validation)
        /// </summary>
        Task<bool> CanRejectApplicationAsync(int applicationId);

        /// <summary>
        /// Check if application can be accepted (business rule validation)
        /// </summary>
        Task<bool> CanAcceptApplicationAsync(int applicationId);
    }

    public class ApplicationStatisticsDto
    {
        public int TotalApplications { get; set; }
        public int UnderReviewCount { get; set; }
        public int InterviewsScheduledCount { get; set; }
        public int AvailableJobsCount { get; set; }
    }

    public class JobApplicationStatisticsDto
    {
        public int TotalApplications { get; set; }
        public int NewApplications { get; set; }
        public int SubmittedCount { get; set; }
        public int UnderReviewCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int InterviewScheduledCount { get; set; }
        public int AcceptedCount { get; set; }
        public int RejectedCount { get; set; }
        public int WithdrawnCount { get; set; }
        public Dictionary<ApplicationStatus, int> StatusBreakdown { get; set; } = new();
        public DateTime? OldestApplicationDate { get; set; }
        public DateTime? NewestApplicationDate { get; set; }
        public double AverageProcessingDays { get; set; }
    }
}