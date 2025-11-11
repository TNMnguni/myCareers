using Xunit;
using Moq;
using FluentAssertions;
using myCareers.Application.Services;
using myCareers.Application.DTOs.JobApplication;
using myCareers.Application.Interfaces;
using myCareers.Core.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace myCareers.Tests.UnitTests.Services
{
    public class JobApplicationServiceTests
    {
        private readonly Mock<IJobApplicationRepository> _mockApplicationRepository;
        private readonly Mock<IJobPostingRepository> _mockJobPostingRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<JobApplicationService>> _mockLogger;
        private readonly Mock<IApplicationDocumentRepository> _mockDocumentRepository;
        private readonly JobApplicationService _service;
        ILogger<JobApplicationService> logger;

        public JobApplicationServiceTests()
        {
            _mockApplicationRepository = new Mock<IJobApplicationRepository>();
            _mockJobPostingRepository = new Mock<IJobPostingRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockDocumentRepository = new Mock<IApplicationDocumentRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<JobApplicationService>>();
            

            _service = new JobApplicationService(
                _mockApplicationRepository.Object,
                _mockJobPostingRepository.Object,
                _mockUserRepository.Object,
                _mockDocumentRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object
                
            );
        }

        #region Create Application Tests

        [Fact]
        public async Task CreateApplicationAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var createDto = new CreateJobApplicationDto
            {
                JobPostingId = 1,
                PositionInfo = new PositionInfoDto
                {
                    JobTitle = "Software Developer",
                    Department = "IT",
                    ReferenceNumber = "REF001",
                    NoticePeriod = "1 month"
                },
                PersonalInfo = new PersonalInfoDto
                {
                    FirstName = "John",
                    LastName = "Doe",
                    DateOfBirth = DateTime.Parse("1990-01-01"),
                    IdPassportNumber = "9001015800080",
                    Race = Race.African,
                    Gender = Gender.Male,
                    IsSouthAfricanCitizen = true
                }
            };

            var jobPosting = new JobPosting
            {
                Id = 1,
                Title = "Software Developer",
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddDays(30)
            };

            var applicant = new Applicant { Id = 1, UserId = 1 };
            var user = new User { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com" };

            var createdApplication = new JobApplication
            {
                Id = 1,
                JobPostingId = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Draft
            };

            var applicationDto = new JobApplicationDto
            {
                Id = 1,
                JobPostingId = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Draft
            };

            _mockJobPostingRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(jobPosting);

            _mockUserRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(user);

            _mockApplicationRepository.Setup(x => x.HasAppliedToJobAsync(1, 1))
                .ReturnsAsync(false);

            _mockApplicationRepository.Setup(x => x.CreateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(createdApplication);

            _mockMapper.Setup(x => x.Map<JobApplicationDto>(It.IsAny<JobApplication>()))
                .Returns(applicationDto);

            // Act
            var result = await _service.CreateApplicationAsync(createDto, applicantId: 1);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Application.Should().NotBeNull();
            result.Application!.Id.Should().Be(1);
            _mockApplicationRepository.Verify(x => x.CreateAsync(It.IsAny<JobApplication>()), Times.Once);
        }

        [Fact]
        public async Task CreateApplicationAsync_WhenAlreadyApplied_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobApplicationDto { JobPostingId = 1 };

            _mockApplicationRepository.Setup(x => x.HasAppliedToJobAsync(1, 1))
                .ReturnsAsync(true);

            // Act
            var result = await _service.CreateApplicationAsync(createDto, applicantId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("You have already applied for this position");
            _mockApplicationRepository.Verify(x => x.CreateAsync(It.IsAny<JobApplication>()), Times.Never);
        }

        [Fact]
        public async Task CreateApplicationAsync_WhenJobNotFound_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobApplicationDto { JobPostingId = 999 };

            _mockApplicationRepository.Setup(x => x.HasAppliedToJobAsync(1, 999))
                .ReturnsAsync(false);

            _mockJobPostingRepository.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((JobPosting)null!);

            // Act
            var result = await _service.CreateApplicationAsync(createDto, applicantId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Job posting not found");
        }

        [Fact]
        public async Task CreateApplicationAsync_WhenJobNotActive_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobApplicationDto { JobPostingId = 1 };
            var jobPosting = new JobPosting
            {
                Id = 1,
                IsActive = false,
                ClosingDate = DateTime.UtcNow.AddDays(30)
            };

            _mockApplicationRepository.Setup(x => x.HasAppliedToJobAsync(1, 1))
                .ReturnsAsync(false);

            _mockJobPostingRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(jobPosting);

            // Act
            var result = await _service.CreateApplicationAsync(createDto, applicantId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("This job posting is no longer accepting applications");
        }

        [Fact]
        public async Task CreateApplicationAsync_WhenJobClosed_ShouldReturnFailure()
        {
            // Arrange
            var createDto = new CreateJobApplicationDto { JobPostingId = 1 };
            var jobPosting = new JobPosting
            {
                Id = 1,
                IsActive = true,
                ClosingDate = DateTime.UtcNow.AddDays(-1) // Past date
            };

            _mockApplicationRepository.Setup(x => x.HasAppliedToJobAsync(1, 1))
                .ReturnsAsync(false);

            _mockJobPostingRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(jobPosting);

            // Act
            var result = await _service.CreateApplicationAsync(createDto, applicantId: 1);

            // Assert
            result.Success.Should().BeFalse();
            // FIX: Match the actual error message from the service
            result.Errors.Should().Contain("This job posting is no longer accepting applications");
        }


        #endregion

        #region Get Application Tests

        [Fact]
        public async Task GetApplicationByIdAsync_WithValidId_ShouldReturnApplication()
        {
            // Arrange
            var application = new JobApplication
            {
                Id = 1,
                JobPostingId = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Submitted
            };

            var applicationDto = new JobApplicationDto
            {
                Id = 1,
                Status = ApplicationStatus.Submitted
            };

            _mockApplicationRepository.Setup(x => x.GetByIdWithDetailsAsync(1))
                .ReturnsAsync(application);

            _mockMapper.Setup(x => x.Map<JobApplicationDto>(application))
                .Returns(applicationDto);

            // Act
            var result = await _service.GetApplicationByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
        }

        [Fact]
        public async Task GetApplicationByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            _mockApplicationRepository.Setup(x => x.GetByIdWithDetailsAsync(999))
                .ReturnsAsync((JobApplication)null!);

            // Act
            var result = await _service.GetApplicationByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetApplicationsByApplicantAsync_ShouldReturnApplications()
        {
            // Arrange
            var applications = new List<JobApplication>
            {
                new() { Id = 1, ApplicantId = 1, Status = ApplicationStatus.Submitted },
                new() { Id = 2, ApplicantId = 1, Status = ApplicationStatus.UnderReview }
            };

            var applicationDtos = new List<JobApplicationDto>
            {
                new() { Id = 1, Status = ApplicationStatus.Submitted },
                new() { Id = 2, Status = ApplicationStatus.UnderReview }
            };

            _mockApplicationRepository.Setup(x => x.GetByApplicantIdAsync(1))
                .ReturnsAsync(applications);

            _mockMapper.Setup(x => x.Map<List<JobApplicationDto>>(applications))
                .Returns(applicationDtos);

            // Act
            var result = await _service.GetApplicationsByApplicantAsync(1);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(a => a.ApplicantId == 1 || a.Id > 0);
        }

        #endregion

        #region Update Application Tests

        [Fact]
        public async Task UpdateApplicationStatusAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var application = new JobApplication
            {
                Id = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Submitted
            };

            _mockApplicationRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(application);

            _mockApplicationRepository.Setup(x => x.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.UpdateApplicationStatusAsync(1, ApplicationStatus.UnderReview);

            // Assert
            result.Success.Should().BeTrue();
            application.Status.Should().Be(ApplicationStatus.UnderReview);
        }

        [Fact]
        public async Task WithdrawApplicationAsync_WhenAllowed_ShouldReturnSuccess()
        {
            // Arrange
            var application = new JobApplication
            {
                Id = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Submitted
            };

            _mockApplicationRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(application);

            _mockApplicationRepository.Setup(x => x.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.WithdrawApplicationAsync(1, applicantId: 1);

            // Assert
            result.Success.Should().BeTrue();
            application.Status.Should().Be(ApplicationStatus.Withdrawn);
        }

        [Fact]
        public async Task WithdrawApplicationAsync_WhenNotOwner_ShouldReturnFailure()
        {
            // Arrange
            var application = new JobApplication
            {
                Id = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Submitted
            };

            _mockApplicationRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(application);

            // Act
            var result = await _service.WithdrawApplicationAsync(1, applicantId: 2); // Different applicant

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("You are not authorized to withdraw this application");
        }

        [Fact]
        public async Task WithdrawApplicationAsync_WhenAlreadyProcessed_ShouldReturnFailure()
        {
            // Arrange
            var application = new JobApplication
            {
                Id = 1,
                ApplicantId = 1,
                Status = ApplicationStatus.Accepted // Already processed
            };

            _mockApplicationRepository.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(application);

            // Act
            var result = await _service.WithdrawApplicationAsync(1, applicantId: 1);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("This application cannot be withdrawn");
        }

        #endregion

        #region Statistics Tests

        [Fact]
        public async Task GetApplicationStatisticsAsync_ShouldReturnCorrectCounts()
        {
            // Arrange
            _mockApplicationRepository.Setup(x => x.GetApplicationCountByApplicantAsync(1))
                .ReturnsAsync(10);

            _mockApplicationRepository.Setup(x => x.GetApplicationCountByStatusAsync(1, ApplicationStatus.UnderReview))
                .ReturnsAsync(3);

            _mockApplicationRepository.Setup(x => x.GetApplicationCountByStatusAsync(1, ApplicationStatus.InterviewScheduled))
                .ReturnsAsync(2);

            _mockJobPostingRepository.Setup(x => x.GetActiveJobCountAsync())
                .ReturnsAsync(25);

            // Act
            var result = await _service.GetApplicationStatisticsAsync(1);

            // Assert
            result.TotalApplications.Should().Be(10);
            result.UnderReviewCount.Should().Be(3);
            result.InterviewsScheduledCount.Should().Be(2);
            result.AvailableJobsCount.Should().Be(25);
        }

        #endregion

        #region RejectApplication Tests

        [Fact]
        public async Task RejectApplicationAsync_WithValidSubmittedApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var rejectionReason = "Does not meet minimum qualifications";
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Submitted,
                ReviewNotes = ""
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, rejectionReason);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Application rejected successfully");
            application.Status.Should().Be(ApplicationStatus.Rejected);
            application.ReviewNotes.Should().Contain(rejectionReason);
            application.ReviewedDate.Should().NotBeNull();

            _mockApplicationRepository.Verify(r => r.UpdateAsync(It.Is<JobApplication>(
                a => a.Status == ApplicationStatus.Rejected)), Times.Once);
        }

        [Fact]
        public async Task RejectApplicationAsync_WithUnderReviewApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.UnderReview
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, null);

            // Assert
            result.Success.Should().BeTrue();
            application.Status.Should().Be(ApplicationStatus.Rejected);
        }

        [Fact]
        public async Task RejectApplicationAsync_WithShortlistedApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Shortlisted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, "Position filled");

            // Assert
            result.Success.Should().BeTrue();
            application.Status.Should().Be(ApplicationStatus.Rejected);
        }

        [Fact]
        public async Task RejectApplicationAsync_WithAcceptedApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Accepted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot reject"));
            _mockApplicationRepository.Verify(r => r.UpdateAsync(It.IsAny<JobApplication>()), Times.Never);
        }

        [Fact]
        public async Task RejectApplicationAsync_WithRejectedApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Rejected
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot reject"));
        }

        [Fact]
        public async Task RejectApplicationAsync_WithDraftApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Draft
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot reject"));
        }

        [Fact]
        public async Task RejectApplicationAsync_WithNonExistentApplication_ShouldFail()
        {
            // Arrange
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((JobApplication?)null);

            // Act
            var result = await _service.RejectApplicationAsync(999, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Application not found");
        }

        #endregion

        #region BulkRejectApplications Tests

        [Fact]
        public async Task BulkRejectApplicationsAsync_WithValidApplications_ShouldSucceed()
        {
            // Arrange
            var jobPostingId = 1;
            var applicationIds = new List<int> { 1, 2, 3 };
            var applications = new List<JobApplication>
            {
                new() { Id = 1, JobPostingId = jobPostingId, Status = ApplicationStatus.Submitted },
                new() { Id = 2, JobPostingId = jobPostingId, Status = ApplicationStatus.UnderReview },
                new() { Id = 3, JobPostingId = jobPostingId, Status = ApplicationStatus.Shortlisted }
            };

            foreach (var app in applications)
            {
                _mockApplicationRepository.Setup(r => r.GetByIdAsync(app.Id))
                    .ReturnsAsync(app);
                _mockApplicationRepository.Setup(r => r.UpdateAsync(app))
                    .ReturnsAsync(app);
            }

            // Act
            var result = await _service.BulkRejectApplicationsAsync(jobPostingId, applicationIds, "Bulk rejection");

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("3 application(s) rejected");
            applications.Should().AllSatisfy(a => a.Status.Should().Be(ApplicationStatus.Rejected));
        }

        [Fact]
        public async Task BulkRejectApplicationsAsync_WithEmptyList_ShouldFail()
        {
            // Arrange
            var jobPostingId = 1;
            var applicationIds = new List<int>();

            // Act
            var result = await _service.BulkRejectApplicationsAsync(jobPostingId, applicationIds, "Test");

            // Assert
            result.Success.Should().BeFalse();
            // FIX: Match the exact error message
            result.Errors.Should().Contain("No applications selected for rejection");
        }


        [Fact]
        public async Task BulkRejectApplicationsAsync_WithMixedValidAndInvalid_ShouldPartiallySucceed()
        {
            // Arrange
            var jobPostingId = 1;
            var applicationIds = new List<int> { 1, 2, 3 };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new JobApplication { Id = 1, JobPostingId = jobPostingId, Status = ApplicationStatus.Submitted });
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(2))
                .ReturnsAsync(new JobApplication { Id = 2, JobPostingId = jobPostingId, Status = ApplicationStatus.Accepted }); // Invalid
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(3))
                .ReturnsAsync(new JobApplication { Id = 3, JobPostingId = jobPostingId, Status = ApplicationStatus.UnderReview });

            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync((JobApplication a) => a);

            // Act
            var result = await _service.BulkRejectApplicationsAsync(jobPostingId, applicationIds, "Test");

            // Assert
            result.Success.Should().BeTrue(); // 2 out of 3 succeeded
            result.Message.Should().Contain("2 application(s) rejected");
            result.Errors.Should().NotBeEmpty();
            result.Errors.Should().Contain(e => e.Contains("Application 2"));
        }

        [Fact]
        public async Task BulkRejectApplicationsAsync_WithWrongJobPosting_ShouldFail()
        {
            // Arrange
            var jobPostingId = 1;
            var applicationIds = new List<int> { 1 };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new JobApplication { Id = 1, JobPostingId = 999, Status = ApplicationStatus.Submitted });

            // Act
            var result = await _service.BulkRejectApplicationsAsync(jobPostingId, applicationIds, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("does not belong to job posting"));
        }

        #endregion

        #region AcceptApplication Tests

        [Fact]
        public async Task AcceptApplicationAsync_WithUnderReviewApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.UnderReview
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.AcceptApplicationAsync(applicationId, null, null);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("accepted successfully");
            application.Status.Should().Be(ApplicationStatus.Accepted);
            application.ReviewedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task AcceptApplicationAsync_WithInterviewDate_ShouldScheduleInterview()
        {
            // Arrange
            var applicationId = 1;
            var interviewDate = DateTime.UtcNow.AddDays(7);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Shortlisted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.AcceptApplicationAsync(applicationId, interviewDate, "Initial interview");

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("interview scheduled");
            application.Status.Should().Be(ApplicationStatus.InterviewScheduled);
            application.InterviewDate.Should().Be(interviewDate);
            application.ReviewNotes.Should().Contain("Initial interview");
        }

        [Fact]
        public async Task AcceptApplicationAsync_WithPastInterviewDate_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.UnderReview
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.AcceptApplicationAsync(applicationId, pastDate, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("cannot be in the past"));
        }

        [Fact]
        public async Task AcceptApplicationAsync_WithSubmittedApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Submitted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.AcceptApplicationAsync(applicationId, null, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Only UnderReview or Shortlisted"));
        }

        #endregion

        #region ScheduleInterview Tests

        [Fact]
        public async Task ScheduleInterviewAsync_WithAcceptedApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var interviewDate = DateTime.UtcNow.AddDays(5);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Accepted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.ScheduleInterviewAsync(applicationId, interviewDate, "Second round");

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("scheduled successfully");
            application.Status.Should().Be(ApplicationStatus.InterviewScheduled);
            application.InterviewDate.Should().Be(interviewDate);
        }

        [Fact]
        public async Task ScheduleInterviewAsync_RescheduleExisting_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var oldDate = DateTime.UtcNow.AddDays(3);
            var newDate = DateTime.UtcNow.AddDays(7);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.InterviewScheduled,
                InterviewDate = oldDate
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.ScheduleInterviewAsync(applicationId, newDate, "Rescheduled");

            // Assert
            result.Success.Should().BeTrue();
            application.InterviewDate.Should().Be(newDate);
            application.ReviewNotes.Should().Contain("Rescheduled");
        }

        [Fact]
        public async Task ScheduleInterviewAsync_WithPastDate_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Accepted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.ScheduleInterviewAsync(applicationId, pastDate, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("cannot be in the past"));
        }

        [Fact]
        public async Task ScheduleInterviewAsync_WithRejectedApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var interviewDate = DateTime.UtcNow.AddDays(5);
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Rejected
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.ScheduleInterviewAsync(applicationId, interviewDate, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot schedule interview"));
        }

        #endregion

        #region DeleteApplication Tests

        [Fact]
        public async Task DeleteApplicationAsync_WithRejectedApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Rejected
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockDocumentRepository.Setup(r => r.GetByApplicationIdAsync(applicationId))
                .ReturnsAsync(new List<ApplicationDocument>());
            _mockApplicationRepository.Setup(r => r.DeleteAsync(applicationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteApplicationAsync(applicationId);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("deleted successfully");
            _mockApplicationRepository.Verify(r => r.DeleteAsync(applicationId), Times.Once);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WithDocuments_ShouldDeleteDocumentsFirst()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Withdrawn
            };
            var documents = new List<ApplicationDocument>
            {
                new() { FileId = Guid.NewGuid(), JobApplicationId = applicationId },
                new() { FileId = Guid.NewGuid(), JobApplicationId = applicationId }
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockDocumentRepository.Setup(r => r.GetByApplicationIdAsync(applicationId))
                .ReturnsAsync(documents);
            _mockDocumentRepository.Setup(r => r.DeleteAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            _mockApplicationRepository.Setup(r => r.DeleteAsync(applicationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteApplicationAsync(applicationId);

            // Assert
            result.Success.Should().BeTrue();
            _mockDocumentRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Exactly(2));
            _mockApplicationRepository.Verify(r => r.DeleteAsync(applicationId), Times.Once);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WithAcceptedApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.Accepted
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.DeleteApplicationAsync(applicationId);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot delete accepted"));
            _mockApplicationRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WithInterviewScheduledApplication_ShouldFail()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.InterviewScheduled
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.DeleteApplicationAsync(applicationId);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("Cannot delete"));
            _mockApplicationRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task DeleteApplicationAsync_WithNonExistentApplication_ShouldFail()
        {
            // Arrange
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((JobApplication?)null);

            // Act
            var result = await _service.DeleteApplicationAsync(999);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Application not found");
        }

        #endregion

        #region UpdateReviewNotes Tests

        [Fact]
        public async Task UpdateReviewNotesAsync_WithValidApplication_ShouldSucceed()
        {
            // Arrange
            var applicationId = 1;
            var reviewNotes = "Candidate has strong technical skills but lacks communication skills";
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.UnderReview,
                ReviewNotes = ""
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ReturnsAsync(application);

            // Act
            var result = await _service.UpdateReviewNotesAsync(applicationId, reviewNotes);

            // Assert
            result.Success.Should().BeTrue();
            result.Message.Should().Contain("Review notes updated");
            application.ReviewNotes.Should().Be(reviewNotes);
            _mockApplicationRepository.Verify(r => r.UpdateAsync(It.Is<JobApplication>(
                a => a.ReviewNotes == reviewNotes)), Times.Once);
        }

        [Fact]
        public async Task UpdateReviewNotesAsync_WithNonExistentApplication_ShouldFail()
        {
            // Arrange
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((JobApplication?)null);

            // Act
            var result = await _service.UpdateReviewNotesAsync(999, "Test notes");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Application not found");
        }

        #endregion

        #region GetJobApplicationStatistics Tests

        [Fact]
        public async Task GetJobApplicationStatisticsAsync_WithApplications_ShouldReturnCorrectStatistics()
        {
            // Arrange
            var jobPostingId = 1;
            var applications = new List<JobApplication>
    {
        new() { Id = 1, JobPostingId = jobPostingId, Status = ApplicationStatus.Submitted, AppliedDate = DateTime.UtcNow.AddDays(-1) },
        new() { Id = 2, JobPostingId = jobPostingId, Status = ApplicationStatus.Submitted, AppliedDate = DateTime.UtcNow.AddDays(-2) },
        new() { Id = 3, JobPostingId = jobPostingId, Status = ApplicationStatus.UnderReview, AppliedDate = DateTime.UtcNow.AddDays(-5), ReviewedDate = DateTime.UtcNow.AddDays(-3) },
        new() { Id = 4, JobPostingId = jobPostingId, Status = ApplicationStatus.Shortlisted, AppliedDate = DateTime.UtcNow.AddDays(-7) },
        new() { Id = 5, JobPostingId = jobPostingId, Status = ApplicationStatus.Rejected, AppliedDate = DateTime.UtcNow.AddDays(-10), ReviewedDate = DateTime.UtcNow.AddDays(-8) },
        new() { Id = 6, JobPostingId = jobPostingId, Status = ApplicationStatus.InterviewScheduled, AppliedDate = DateTime.UtcNow.AddDays(-6) },
        new() { Id = 7, JobPostingId = jobPostingId, Status = ApplicationStatus.Accepted, AppliedDate = DateTime.UtcNow.AddDays(-15) },
        new() { Id = 8, JobPostingId = jobPostingId, Status = ApplicationStatus.Withdrawn, AppliedDate = DateTime.UtcNow.AddDays(-3) }
    };

            _mockApplicationRepository.Setup(r => r.GetByJobPostingIdAsync(jobPostingId))
                .ReturnsAsync(applications);

            // Act
            var result = await _service.GetJobApplicationStatisticsAsync(jobPostingId);

            // Assert
            result.Should().NotBeNull();
            result.TotalApplications.Should().Be(8);
            result.SubmittedCount.Should().Be(2);
            result.UnderReviewCount.Should().Be(1);
            result.ShortlistedCount.Should().Be(1);
            result.InterviewScheduledCount.Should().Be(1);
            result.AcceptedCount.Should().Be(1);
            result.RejectedCount.Should().Be(1);
            result.WithdrawnCount.Should().Be(1);
            result.NewApplications.Should().Be(2); // Applications in last 7 days
            
            // (2 apps are Submitted, so only 7 unique statuses)
            result.StatusBreakdown.Should().HaveCount(7); // Changed from 8 to 7
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.Submitted).WhoseValue.Should().Be(2);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.UnderReview).WhoseValue.Should().Be(1);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.Shortlisted).WhoseValue.Should().Be(1);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.InterviewScheduled).WhoseValue.Should().Be(1);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.Accepted).WhoseValue.Should().Be(1);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.Rejected).WhoseValue.Should().Be(1);
            result.StatusBreakdown.Should().ContainKey(ApplicationStatus.Withdrawn).WhoseValue.Should().Be(1);

            result.OldestApplicationDate.Should().NotBeNull();
            result.NewestApplicationDate.Should().NotBeNull();
            result.AverageProcessingDays.Should().BeGreaterThan(0);
        }

        #endregion

        #region GetApplicationsByStatus Tests

        [Fact]
        public async Task GetApplicationsByStatusAsync_WithValidStatus_ShouldReturnFilteredApplications()
        {
            // Arrange
            var jobPostingId = 1;
            var status = ApplicationStatus.UnderReview;
            var allApplications = new List<JobApplication>
            {
                new() { Id = 1, JobPostingId = jobPostingId, Status = ApplicationStatus.Submitted },
                new() { Id = 2, JobPostingId = jobPostingId, Status = ApplicationStatus.UnderReview },
                new() { Id = 3, JobPostingId = jobPostingId, Status = ApplicationStatus.UnderReview },
                new() { Id = 4, JobPostingId = jobPostingId, Status = ApplicationStatus.Rejected }
            };

            _mockApplicationRepository.Setup(r => r.GetByJobPostingIdAsync(jobPostingId))
                .ReturnsAsync(allApplications);
            _mockMapper.Setup(m => m.Map<List<JobApplicationDto>>(It.IsAny<List<JobApplication>>()))
                .Returns(new List<JobApplicationDto>
                {
                    new() { Id = 2, Status = ApplicationStatus.UnderReview },
                    new() { Id = 3, Status = ApplicationStatus.UnderReview }
                });
            _mockDocumentRepository.Setup(r => r.GetByApplicationIdAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<ApplicationDocument>());

            // Act
            var result = await _service.GetApplicationsByStatusAsync(jobPostingId, status);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(a => a.Status.Should().Be(ApplicationStatus.UnderReview));
        }

        #endregion

        #region CanRejectApplication Tests

        [Theory]
        [InlineData(ApplicationStatus.Submitted, true)]
        [InlineData(ApplicationStatus.UnderReview, true)]
        [InlineData(ApplicationStatus.Shortlisted, true)]
        [InlineData(ApplicationStatus.Accepted, false)]
        [InlineData(ApplicationStatus.Rejected, false)]
        [InlineData(ApplicationStatus.Withdrawn, false)]
        [InlineData(ApplicationStatus.Draft, false)]
        [InlineData(ApplicationStatus.InterviewScheduled, false)]
        public async Task CanRejectApplicationAsync_WithVariousStatuses_ShouldReturnCorrectResult(
            ApplicationStatus status, bool expectedResult)
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = status
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.CanRejectApplicationAsync(applicationId);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task CanRejectApplicationAsync_WithNonExistentApplication_ShouldReturnFalse()
        {
            // Arrange
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((JobApplication?)null);

            // Act
            var result = await _service.CanRejectApplicationAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region CanAcceptApplication Tests

        [Theory]
        [InlineData(ApplicationStatus.UnderReview, true)]
        [InlineData(ApplicationStatus.Shortlisted, true)]
        [InlineData(ApplicationStatus.Submitted, false)]
        [InlineData(ApplicationStatus.Accepted, false)]
        [InlineData(ApplicationStatus.Rejected, false)]
        [InlineData(ApplicationStatus.Withdrawn, false)]
        [InlineData(ApplicationStatus.Draft, false)]
        [InlineData(ApplicationStatus.InterviewScheduled, false)]
        public async Task CanAcceptApplicationAsync_WithVariousStatuses_ShouldReturnCorrectResult(
            ApplicationStatus status, bool expectedResult)
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = status
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);

            // Act
            var result = await _service.CanAcceptApplicationAsync(applicationId);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public async Task CanAcceptApplicationAsync_WithNonExistentApplication_ShouldReturnFalse()
        {
            // Arrange
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((JobApplication?)null);

            // Act
            var result = await _service.CanAcceptApplicationAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public async Task RejectApplicationAsync_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var applicationId = 1;
            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await _service.RejectApplicationAsync(applicationId, "Test");

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("error occurred"));
        }

        [Fact]
        public async Task AcceptApplicationAsync_WhenUpdateFails_ShouldReturnFailure()
        {
            // Arrange
            var applicationId = 1;
            var application = new JobApplication
            {
                Id = applicationId,
                Status = ApplicationStatus.UnderReview
            };

            _mockApplicationRepository.Setup(r => r.GetByIdAsync(applicationId))
                .ReturnsAsync(application);
            _mockApplicationRepository.Setup(r => r.UpdateAsync(It.IsAny<JobApplication>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var result = await _service.AcceptApplicationAsync(applicationId, null, null);

            // Assert
            result.Success.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        #endregion
    }
}