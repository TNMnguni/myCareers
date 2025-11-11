using Microsoft.EntityFrameworkCore;
using myCareers.Core.Interfaces;
using myCareers.Core.Entities;
using myCareers.Infrastructure.Data;

namespace myCareers.Infrastructure.Repositories
{
    public class JobPostingRepository : IJobPostingRepository
    {
        private readonly myCareersDbContext _context;

        public JobPostingRepository(myCareersDbContext context)
        {
            _context = context;
        }

        public async Task<JobPosting> CreateAsync(JobPosting jobPosting)
        {
            _context.JobPostings.Add(jobPosting);
            await _context.SaveChangesAsync();
            return jobPosting;
        }


        public async Task<JobPosting?> GetByIdAsync(int id)
        {
            return await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(j => j.Id == id);
        }
        public async Task<JobPosting?> GetByIdWithIncludesAsync(int id)
        {
        return await _context.JobPostings
            .Include(jp => jp.Recruiter)
                .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(jp => jp.Id == id);
        }

        public async Task<List<JobPosting>> GetAllAsync()
        {
            return await _context.JobPostings
                .Include(j => j.Recruiter)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();
        }
     

        public async Task<List<JobPosting>> GetActiveJobsAsync()
        {
            return await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive && j.ClosingDate > DateTime.UtcNow)
                .OrderByDescending(j => j.PublishedDate)
                .ToListAsync();
        }
        // NEW: Include Recruiter and User
        public async Task<IEnumerable<JobPosting>> GetByRecruiterIdWithIncludesAsync(int recruiterId)
        {
            return await _context.JobPostings
                .Include(jp => jp.Recruiter)
                    .ThenInclude(r => r.User)
                .Where(jp => jp.RecruiterId == recruiterId)
                .ToListAsync();
        }
        public async Task<List<JobPosting>> GetByRecruiterIdAsync(int recruiterId)
        {
            return await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.RecruiterId == recruiterId)
                .OrderByDescending(j => j.CreatedDate)
                .ToListAsync();
        }

        public async Task<JobPosting> UpdateAsync(JobPosting jobPosting)
        {
            _context.JobPostings.Update(jobPosting);
            await _context.SaveChangesAsync();
            return jobPosting;
        }

        public async Task DeleteAsync(int id)
        {
            var jobPosting = await _context.JobPostings.FindAsync(id);
            if (jobPosting != null)
            {
                _context.JobPostings.Remove(jobPosting);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetTotalJobCountAsync()
        {
            return await _context.JobPostings.CountAsync();
        }

        public async Task<int> GetActiveJobCountAsync()
        {
            return await _context.JobPostings
                .CountAsync(j => j.IsActive && j.ClosingDate > DateTime.UtcNow);
        }
        // NEW: Include Recruiter and User
        public async Task<IEnumerable<JobPosting>> GetActiveJobsWithIncludesAsync()
        {
            return await _context.JobPostings
                .Include(jp => jp.Recruiter)
                    .ThenInclude(r => r.User)
                .Where(jp => jp.IsActive)
                .OrderByDescending(jp => jp.PublishedDate)
                .ToListAsync();
        }

        public async Task<List<JobPosting>> SearchJobsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetActiveJobsAsync();
            }

            var lowerSearchTerm = searchTerm.ToLower();

            return await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive &&
                           j.ClosingDate > DateTime.UtcNow &&
                           (j.Title.ToLower().Contains(lowerSearchTerm) ||
                            j.Description.ToLower().Contains(lowerSearchTerm) ||
                            (j.Department != null && j.Department.ToLower().Contains(lowerSearchTerm)) ||
                            (j.Location != null && j.Location.ToLower().Contains(lowerSearchTerm))))
                .OrderByDescending(j => j.PublishedDate)
                .ToListAsync();
        }

        // NEW: Include Recruiter and User
        public async Task<IEnumerable<JobPosting>> SearchJobsWithIncludesAsync(string searchTerm)
        {
            return await _context.JobPostings
                .Include(jp => jp.Recruiter)
                    .ThenInclude(r => r.User)
                .Where(jp => jp.IsActive &&
                    (jp.Title.Contains(searchTerm) ||
                     jp.Description.Contains(searchTerm) ||
                     (jp.Department != null && jp.Department.Contains(searchTerm))))
                .OrderByDescending(jp => jp.PublishedDate)
                .ToListAsync();
        }
    }
}
