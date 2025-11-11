using AutoMapper;
using Microsoft.Extensions.Logging;
using myCareers.Application.DTOs.JobPosting;
using myCareers.Application.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;

namespace myCareers.Application.Services
{
    public class JobPostingService : IJobPostingService
    {
        private readonly IJobPostingRepository _jobRepository;
        private readonly IRecruiterRepository _recruiterRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<JobPostingService> _logger;

        public JobPostingService(
            IJobPostingRepository jobRepository,
            IRecruiterRepository recruiterRepository,
            IMapper mapper,
            ILogger<JobPostingService> logger)
        {
            _jobRepository = jobRepository;
            _recruiterRepository = recruiterRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // CREATE
        public async Task<JobPostingResult> CreateJobPostingAsync(JobPostingDto dto, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Title is required" } };

                if (dto.ClosingDate <= DateTime.UtcNow)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Closing date must be in the future" } };

                var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);
                if (recruiter == null)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Recruiter profile not found" } };

                var job = new JobPosting
                {
                    RecruiterId = recruiter.Id, 
                    Title = dto.Title,
                    Description = dto.Description,
                    Department = dto.Department,
                    Location = dto.Location,
                    SalaryRange = dto.SalaryRange,
                    EmploymentType = dto.EmploymentType,
                    ExperienceLevel = dto.ExperienceLevel,
                    ClosingDate = dto.ClosingDate,
                    Status = dto.PublishImmediately ? JobStatus.Published : JobStatus.Draft,
                    IsActive = dto.PublishImmediately,
                    CreatedDate = DateTime.UtcNow,
                    PublishedDate = dto.PublishImmediately ? DateTime.UtcNow : null
                };

                var createdJob = await _jobRepository.CreateAsync(job);

                // Reload with related entities
                createdJob = await _jobRepository.GetByIdWithIncludesAsync(createdJob.Id);

                if (createdJob == null)
                {
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Failed to retrieve created job posting" } };
                }

                _logger.LogInformation("Job posting created: {JobId} by recruiter {RecruiterId}", createdJob.Id, recruiter.Id);

                return new JobPostingResult
                {
                    Success = true,
                    JobPosting = _mapper.Map<JobPostingDto>(createdJob)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job posting for user {UserId}", userId);
                return new JobPostingResult { Success = false, Errors = new List<string> { $"An error occurred while creating the job posting: {ex.Message}" } };
            }
        }

        // UPDATE
        public async Task<JobPostingResult> UpdateJobPostingForUserAsync(UpdateJobPostingDto dto, int userId)
        {
            try
            {
                var job = await _jobRepository.GetByIdWithIncludesAsync(dto.Id);
                if (job == null)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Job posting not found" } };

                var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);        
                if (recruiter == null || job.Id != recruiter.Id)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "You are not authorized to update this job posting" } };

                job.Title = dto.Title;
                job.Description = dto.Description;
                job.Department = dto.Department;
                job.Location = dto.Location;
                job.SalaryRange = dto.SalaryRange;
                job.EmploymentType = dto.EmploymentType;
                job.ExperienceLevel = dto.ExperienceLevel;
                job.ClosingDate = dto.ClosingDate;
                job.UpdatedDate = DateTime.UtcNow;

                var updatedJob = await _jobRepository.UpdateAsync(job);

                // Reload with related entities
                updatedJob = await _jobRepository.GetByIdWithIncludesAsync(updatedJob.Id);

                _logger.LogInformation("Job posting updated: {JobId} by recruiter {RecruiterId}", updatedJob.Id, recruiter.Id);

                return new JobPostingResult { Success = true, JobPosting = _mapper.Map<JobPostingDto>(updatedJob) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job posting {JobId} for user {UserId}", dto.Id, userId);
                return new JobPostingResult { Success = false, Errors = new List<string> { $"An error occurred while updating the job posting: {ex.Message}" } };
            }
        }

        // DELETE
        public async Task<JobPostingResult> DeleteJobPostingForUserAsync(int jobId, int userId)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(jobId);
                if (job == null)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Job posting not found" } };

                var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);
                if (recruiter == null || job.RecruiterId != recruiter.Id)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "You are not authorized to delete this job posting" } };

                await _jobRepository.DeleteAsync(jobId);
                _logger.LogInformation("Job posting deleted: {JobId} by recruiter {RecruiterId}", jobId, recruiter.Id);

                return new JobPostingResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job posting {JobId} for user {UserId}", jobId, userId);
                return new JobPostingResult { Success = false, Errors = new List<string> { "An error occurred while deleting the job posting" } };
            }
        }

        // ACTIVATE / DEACTIVATE / PUBLISH
        public async Task<JobPostingResult> DeactivateJobPostingForUserAsync(int jobId, int userId)
            => await UpdateJobStatusForUserAsync(jobId, userId, isActive: false);

        public async Task<JobPostingResult> ActivateJobPostingForUserAsync(int jobId, int userId)
            => await UpdateJobStatusForUserAsync(jobId, userId, isActive: true);

        public async Task<JobPostingResult> PublishJobPostingForUserAsync(int jobId, int userId)
            => await UpdateJobStatusForUserAsync(jobId, userId, isActive: true, publish: true);

        private async Task<JobPostingResult> UpdateJobStatusForUserAsync(int jobId, int userId, bool? isActive = null, bool publish = false)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(jobId);
                if (job == null)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "Job posting not found" } };

                var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);
                if (recruiter == null || job.RecruiterId != recruiter.Id)
                    return new JobPostingResult { Success = false, Errors = new List<string> { "You are not authorized to modify this job posting" } };

                if (isActive.HasValue) job.IsActive = isActive.Value;
                if (publish)
                {
                    job.Status = JobStatus.Published;
                    job.PublishedDate = DateTime.UtcNow;
                }
                job.UpdatedDate = DateTime.UtcNow;

                var updatedJob = await _jobRepository.UpdateAsync(job);

                // Reload with related entities
                updatedJob = await _jobRepository.GetByIdWithIncludesAsync(updatedJob.Id);

                _logger.LogInformation("Job posting updated (status): {JobId} by recruiter {RecruiterId}", updatedJob.Id, recruiter.Id);

                return new JobPostingResult { Success = true, JobPosting = _mapper.Map<JobPostingDto>(updatedJob) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job posting {JobId} status for user {UserId}", jobId, userId);
                return new JobPostingResult { Success = false, Errors = new List<string> { "An error occurred" } };
            }
        }

        // GET JOB POSTINGS
        public async Task<JobPostingDto?> GetJobPostingByIdAsync(int id)
        {
            var job = await _jobRepository.GetByIdWithIncludesAsync(id);
            return job == null ? null : _mapper.Map<JobPostingDto>(job);
        }

        public async Task<JobPostingDto?> GetJobPostingByIdForUserAsync(int jobId, int userId)
        {
            var job = await _jobRepository.GetByIdWithIncludesAsync(jobId);
            var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);
            if (job == null || recruiter == null || job.Id != recruiter.Id)
                return null;

            return _mapper.Map<JobPostingDto>(job);
        }

        public async Task<List<JobPostingDto>> GetJobPostingsByUserAsync(int userId)
        {
            var recruiter = await _recruiterRepository.GetByUserIdAsync(userId);
            if (recruiter == null)
                return new List<JobPostingDto>();

            var jobs = await _jobRepository.GetByRecruiterIdWithIncludesAsync(recruiter.Id);
            return jobs == null ? new List<JobPostingDto>() : _mapper.Map<List<JobPostingDto>>(jobs);
        }

        public async Task<List<JobPostingDto>> GetActiveJobPostingsAsync()
        {
            var jobs = await _jobRepository.GetActiveJobsWithIncludesAsync();
            return jobs == null ? new List<JobPostingDto>() : _mapper.Map<List<JobPostingDto>>(jobs);
        }

        public async Task<List<JobPostingDto>> SearchJobPostingsAsync(string searchTerm)
        {
            var jobs = await _jobRepository.SearchJobsWithIncludesAsync(searchTerm);
            return jobs == null ? new List<JobPostingDto>() : _mapper.Map<List<JobPostingDto>>(jobs);
        }
    }
}