using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;
using myCareers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace myCareers.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly myCareersDbContext _context;

        public JobApplicationRepository(myCareersDbContext context)
        {
            _context = context;
        }

        public async Task<JobApplication> CreateAsync(JobApplication application)
        {
            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task<JobApplication?> GetByIdAsync(int id)
        {
            return await _context.JobApplications
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<JobApplication?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap.User)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<JobApplication>> GetByApplicantIdAsync(int applicantId)
        {
            return await _context.JobApplications
                .Include(a => a.JobPosting)
                .Where(a => a.ApplicantId == applicantId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();
        }

        public async Task<List<JobApplication>> GetByJobPostingIdAsync(int jobPostingId)
        {
            return await _context.JobApplications
                .Include(a => a.Applicant)
                    .ThenInclude(ap => ap.User)
                .Where(a => a.JobPostingId == jobPostingId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();
        }

        public async Task<JobApplication> UpdateAsync(JobApplication application)
        {
            application.UpdatedDate = DateTime.UtcNow;
            _context.JobApplications.Update(application);
            await _context.SaveChangesAsync();
            return application;
        }

        public async Task DeleteAsync(int id)
        {
            var application = await _context.JobApplications.FindAsync(id);
            if (application != null)
            {
                _context.JobApplications.Remove(application);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetApplicationCountByApplicantAsync(int applicantId)
        {
            return await _context.JobApplications
                .CountAsync(a => a.ApplicantId == applicantId);
        }

        public async Task<int> GetApplicationCountByStatusAsync(int applicantId, ApplicationStatus status)
        {
            return await _context.JobApplications
                .CountAsync(a => a.ApplicantId == applicantId && a.Status == status);
        }

        public async Task<bool> HasAppliedToJobAsync(int applicantId, int jobPostingId)
        {
            return await _context.JobApplications
                .AnyAsync(a => a.ApplicantId == applicantId && a.JobPostingId == jobPostingId);
        }
    }
}