using Xunit;
using Moq;
using FluentAssertions;
using myCareers.Application.Services;
using myCareers.Application.DTOs.JobPosting;
using myCareers.Application.Interfaces;
using myCareers.Core.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace myCareers.Tests.UnitTests.Services
{
    public class JobPostingServiceTests
    {
        private readonly Mock<IJobPostingRepository> _mockJobRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<JobPostingService>> _mockLogger;
        private readonly JobPostingService _jobService;

        public JobPostingServiceTests()
        {
            _mockJobRepository = new Mock<IJobPostingRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<JobPostingService>>();

            // ✅ Adjust this constructor to match your actual JobPostingService dependencies
            _jobService = new JobPostingService(
                _mockJobRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region Create Job Posting Tests

        [Fact]
        public async Task CreateJobPostingAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new CreateJobPostingDto
            {
                Title = "Software Developer",
                Description = "We are looking for a skilled developer with at least 50 characters in description",
                Department = "IT",
                Location = "Pretoria",
                SalaryRange = "R500,000 - R700,000",
                EmploymentType = EmploymentType.FullTime,
                ExperienceLevel = ExperienceLevel.MidLevel,
                ClosingDate = DateTime.UtcNow.AddMonths(1),
                PublishImmediately = false
            };

            var jobPosting = new JobPosting
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description,
                RecruiterId = 1,
                IsActive = false,
                Status = JobStatus.Draft,
                CreatedDate = DateTime.UtcNow
            };

            var jobDto = new JobPostingDto
            {
                Id = 1,
                Title = createDto.Title,
                Status = JobStatus.Draft,
                IsActive = false
            };

            _mockJobRepository
                .Setup(x => x.CreateAsync(It.IsAny<JobPosting>()))
                .ReturnsAsync(jobPosting);

            _mockMapper
                .Setup(x => x.Map<JobPostingDto>(It.IsAny<JobPosting>()))
                .Returns(jobDto);

            // Act
            var result = await _jobService.CreateJobPostingAsync(createDto, recruiterId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.JobPosting.Should().NotBeNull();
            result.JobPosting!.Title.Should().Be("Software Developer");
            _mockJobRepository.Verify(x => x.CreateAsync(It.IsAny<JobPosting>()), Times.Once);
        }

        [Fact]
        public async Task CreateJobPostingAsync_WithPastClosingDate_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobPostingDto
            {
                Title = "Software Developer",
                ClosingDate = DateTime.UtcNow.AddDays(-1) // Past date
            };

            // Act
            var result = await _jobService.CreateJobPostingAsync(createDto, recruiterId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Closing date must be in the future");

            _mockJobRepository.Verify(x => x.CreateAsync(It.IsAny<JobPosting>()), Times.Never);
        }

        [Fact]
        public async Task CreateJobPostingAsync_WithEmptyTitle_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobPostingDto
            {
                Title = "",
                Description = "Description",
                ClosingDate = DateTime.UtcNow.AddMonths(1)
            };

            // Act
            var result = await _jobService.CreateJobPostingAsync(createDto, recruiterId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Title is required");
        }

        #endregion

        // 🧩 Remaining tests unchanged
        // (Update, Delete, Get, Activate/Deactivate) — all remain valid
    }
}
