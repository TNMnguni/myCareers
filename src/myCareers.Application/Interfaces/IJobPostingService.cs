using myCareers.Application.DTOs.JobPosting;

namespace myCareers.Application.Interfaces
{
    public interface IJobPostingService
    {
        // Create
        Task<JobPostingResult> CreateJobPostingAsync(JobPostingDto dto, int userId);

        // Update
        Task<JobPostingResult> UpdateJobPostingForUserAsync(UpdateJobPostingDto dto, int userId);

        // Delete
        Task<JobPostingResult> DeleteJobPostingForUserAsync(int jobId, int userId);

        // Activate / Deactivate / Publish
        Task<JobPostingResult> DeactivateJobPostingForUserAsync(int jobId, int userId);
        Task<JobPostingResult> ActivateJobPostingForUserAsync(int jobId, int userId);
        Task<JobPostingResult> PublishJobPostingForUserAsync(int jobId, int userId);

        // Get job postings
        Task<JobPostingDto?> GetJobPostingByIdAsync(int id);
        Task<JobPostingDto?> GetJobPostingByIdForUserAsync(int jobId, int userId);
        Task<List<JobPostingDto>> GetJobPostingsByUserAsync(int userId);
        Task<List<JobPostingDto>> GetActiveJobPostingsAsync();
        Task<List<JobPostingDto>> SearchJobPostingsAsync(string searchTerm);
    }
}
