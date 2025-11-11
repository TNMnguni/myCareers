using Xunit;
using FluentAssertions;
using myCareers.Infrastructure.Data;
using myCareers.Infrastructure.Repositories;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace myCareers.Tests.IntegrationTests
{
	public class JobApplicationRepositoryTests : IDisposable
	{
		private readonly myCareersDbContext _context;
		private readonly JobApplicationRepository _repository;

		public JobApplicationRepositoryTests()
		{
			var options = new DbContextOptionsBuilder<myCareersDbContext>()
				.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
				.Options;

			_context = new myCareersDbContext(options);
			_repository = new JobApplicationRepository(_context);

			// Seed test data
			SeedTestData();
		}

		private void SeedTestData()
		{
			var user = new User
			{
				Email = "applicant@test.com",
				PasswordHash = "hash",
				FirstName = "Test",
				LastName = "Applicant",
				Role = UserRole.Applicant,
				IsActive = true
			};
			_context.Users.Add(user);
			_context.SaveChanges();

			var applicant = new Applicant
			{
				UserId = user.Id
			};
			_context.Applicants.Add(applicant);

			var recruiter = new User
			{
				Email = "recruiter@test.com",
				PasswordHash = "hash",
				FirstName = "Test",
				LastName = "Recruiter",
				Role = UserRole.Recruiter,
				IsActive = true
			};
			_context.Users.Add(recruiter);
			_context.SaveChanges();

			var jobPosting = new JobPosting
			{
				RecruiterId = recruiter.Id,
				Title = "Software Developer",
				Description = "Test job",
				IsActive = true,
				ClosingDate = DateTime.UtcNow.AddDays(30),
				EmploymentType = EmploymentType.FullTime,
				ExperienceLevel = ExperienceLevel.MidLevel
			};
			_context.JobPostings.Add(jobPosting);
			_context.SaveChanges();
		}

		[Fact]
		public async Task CreateAsync_ShouldAddApplicationToDatabase()
		{
			// Arrange
			var application = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				JobTitle = "Software Developer",
				Department = "IT",
				ReferenceNumber = "REF001",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com",
				Status = ApplicationStatus.Draft
			};

			// Act
			var result = await _repository.CreateAsync(application);

			// Assert
			result.Should().NotBeNull();
			result.Id.Should().BeGreaterThan(0);
			var dbApplication = await _context.JobApplications.FindAsync(result.Id);
			dbApplication.Should().NotBeNull();
			dbApplication!.JobTitle.Should().Be("Software Developer");
		}

		[Fact]
		public async Task GetByApplicantIdAsync_ShouldReturnApplicantApplications()
		{
			// Arrange
			var app1 = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				JobTitle = "Job 1",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};
			var app2 = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				JobTitle = "Job 2",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};

			await _context.JobApplications.AddRangeAsync(app1, app2);
			await _context.SaveChangesAsync();

			// Act
			var result = await _repository.GetByApplicantIdAsync(1);

			// Assert
			result.Should().HaveCount(2);
			result.Should().OnlyContain(a => a.ApplicantId == 1);
		}

		[Fact]
		public async Task HasAppliedToJobAsync_WhenApplied_ShouldReturnTrue()
		{
			// Arrange
			var application = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				JobTitle = "Test",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};
			await _context.JobApplications.AddAsync(application);
			await _context.SaveChangesAsync();

			// Act
			var result = await _repository.HasAppliedToJobAsync(1, 1);

			// Assert
			result.Should().BeTrue();
		}

		[Fact]
		public async Task HasAppliedToJobAsync_WhenNotApplied_ShouldReturnFalse()
		{
			// Act
			var result = await _repository.HasAppliedToJobAsync(1, 999);

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public async Task GetApplicationCountByStatusAsync_ShouldReturnCorrectCount()
		{
			// Arrange
			var app1 = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				Status = ApplicationStatus.Submitted,
				JobTitle = "Test",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};
			var app2 = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				Status = ApplicationStatus.UnderReview,
				JobTitle = "Test",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};
			var app3 = new JobApplication
			{
				JobPostingId = 1,
				ApplicantId = 1,
				Status = ApplicationStatus.Submitted,
				JobTitle = "Test",
				FirstName = "John",
				LastName = "Doe",
				Email = "john@test.com"
			};

			await _context.JobApplications.AddRangeAsync(app1, app2, app3);
			await _context.SaveChangesAsync();

			// Act
			var result = await _repository.GetApplicationCountByStatusAsync(1, ApplicationStatus.Submitted);

			// Assert
			result.Should().Be(2);
		}

		public void Dispose()
		{
			_context.Database.EnsureDeleted();
			_context.Dispose();
		}
	}
}