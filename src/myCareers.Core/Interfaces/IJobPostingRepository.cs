using myCareers.Core.Entities;

namespace myCareers.Core.Interfaces
{
    public interface IJobPostingRepository
    {
        Task<JobPosting> CreateAsync(JobPosting jobPosting);
        Task<JobPosting?> GetByIdAsync(int id);
        Task<List<JobPosting>> GetAllAsync();
        Task<List<JobPosting>> GetActiveJobsAsync();
        Task<List<JobPosting>> GetByRecruiterIdAsync(int recruiterId);
        Task<JobPosting?> GetByIdWithIncludesAsync(int id);
        Task<JobPosting> UpdateAsync(JobPosting jobPosting);
        Task DeleteAsync(int id);
        Task<int> GetTotalJobCountAsync();
        Task<int> GetActiveJobCountAsync();
        Task<IEnumerable<JobPosting>> GetActiveJobsWithIncludesAsync();
        Task<IEnumerable<JobPosting>> GetByRecruiterIdWithIncludesAsync(int recruiterId);
        Task<IEnumerable<JobPosting>> SearchJobsWithIncludesAsync(string searchTerm);
        Task<List<JobPosting>> SearchJobsAsync(string searchTerm);
    }
}
