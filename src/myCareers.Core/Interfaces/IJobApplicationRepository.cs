using myCareers.Core.Entities;
using myCareers.Core.Enums;

namespace myCareers.Core.Interfaces
{
    public interface IJobApplicationRepository
    {
        Task<JobApplication> CreateAsync(JobApplication application);
        Task<JobApplication?> GetByIdAsync(int id);
        Task<JobApplication?> GetByIdWithDetailsAsync(int id);
        Task<List<JobApplication>> GetByApplicantIdAsync(int applicantId);
        Task<List<JobApplication>> GetByJobPostingIdAsync(int jobPostingId);
        Task<JobApplication> UpdateAsync(JobApplication application);
        Task DeleteAsync(int id);
        Task<int> GetApplicationCountByApplicantAsync(int applicantId);
        Task<int> GetApplicationCountByStatusAsync(int applicantId, ApplicationStatus status);
        Task<bool> HasAppliedToJobAsync(int applicantId, int jobPostingId);
    }
}