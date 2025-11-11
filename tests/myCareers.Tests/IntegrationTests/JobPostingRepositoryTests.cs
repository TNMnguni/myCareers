using Xunit;
using FluentAssertions;
using myCareers.Infrastructure.Data;
using myCareers.Infrastructure.Repositories;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace myCareers.Tests.IntegrationTests
{
    public class JobPostingRepositoryTests : IDisposable
    {
        private readonly myCareersDbContext _context;
        private readonly JobPostingRepository _repository;

        public JobPostingRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<myCareersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new myCareersDbContext(options);
            _repository = new JobPostingRepository(_context);

            // Seed a recruiter
            var recruiter = new User
            {
                Email = "recruiter@dirco.gov.za",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Recruiter",
                Role = UserRole.Recruiter,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.Users.Add(recruiter);
            _context.SaveChanges();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddJobToDatabase()
        {
            // Arrange
            var job = new JobPosting
            {
                RecruiterId = 1,
                Title = "Software Developer",
                Description = "Great opportunity",
                Department = "IT",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            // Act
            var result = await _repository.CreateAsync(job);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            var jobInDb = await _context.JobPostings.FindAsync(result.Id);
            jobInDb.Should().NotBeNull();
            jobInDb!.Title.Should().Be("Software Developer");
        }

        [Fact]
        public async Task GetByRecruiterIdAsync_ShouldReturnRecruiterJobs()
        {
            // Arrange
            var job1 = new JobPosting
            {
                RecruiterId = 1,
                Title = "Job 1",
                Description = "Description 1",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            var job2 = new JobPosting
            {
                RecruiterId = 1,
                Title = "Job 2",
                Description = "Description 2",
                IsActive = false,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            var job3 = new JobPosting
            {
                RecruiterId = 2,
                Title = "Job 3",
                Description = "Description 3",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            await _context.JobPostings.AddRangeAsync(job1, job2, job3);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByRecruiterIdAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(j => j.RecruiterId == 1);
        }

        [Fact]
        public async Task GetActiveJobsAsync_ShouldReturnOnlyActiveJobs()
        {
            // Arrange
            var activeJob = new JobPosting
            {
                RecruiterId = 1,
                Title = "Active",
                Description = "Active job description",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddDays(30), // ? Future date
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel,
                Status = JobStatus.Published,
                CreatedDate = DateTime.UtcNow
            };

            var inactiveJob = new JobPosting
            {
                RecruiterId = 1,
                Title = "Inactive",
                Description = "Inactive job description",
                IsActive = false,
                ClosingDate = DateTime.UtcNow.AddDays(30),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel,
                Status = JobStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };

            var closedJob = new JobPosting
            {
                RecruiterId = 1,
                Title = "Closed",
                Description = "Closed job description",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddDays(-1), // ? Past date (closed)
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel,
                Status = JobStatus.Published,
                CreatedDate = DateTime.UtcNow
            };

            await _context.JobPostings.AddRangeAsync(activeJob, inactiveJob, closedJob);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetActiveJobsAsync();

            // Assert
            result.Should().HaveCount(1, "only one job is both active and has a future closing date");
            result.Should().OnlyContain(j => j.IsActive, "all returned jobs should have IsActive = true");
            result.Should().OnlyContain(j => j.ClosingDate > DateTime.UtcNow, "all returned jobs should have future closing dates");
            result.First().Title.Should().Be("Active");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyJobInDatabase()
        {
            // Arrange
            var job = new JobPosting
            {
                RecruiterId = 1,
                Title = "Original Title",
                Description = "Original description",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            await _context.JobPostings.AddAsync(job);
            await _context.SaveChangesAsync();

            // Act
            job.Title = "Updated Title";
            job.IsActive = false;
            await _repository.UpdateAsync(job);

            // Assert
            var updatedJob = await _context.JobPostings.FindAsync(job.Id);
            updatedJob.Should().NotBeNull();
            updatedJob!.Title.Should().Be("Updated Title");
            updatedJob.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveJobFromDatabase()
        {
            // Arrange
            var job = new JobPosting
            {
                RecruiterId = 1,
                Title = "To Delete",
                Description = "Job to be deleted",
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel
            };

            await _context.JobPostings.AddAsync(job);
            await _context.SaveChangesAsync();
            var jobId = job.Id;

            // Act
            await _repository.DeleteAsync(jobId);

            // Assert
            var deletedJob = await _context.JobPostings.FindAsync(jobId);
            deletedJob.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}