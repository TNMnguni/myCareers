using AutoMapper;
using myCareers.Application.DTOs.JobApplication;
using myCareers.Application.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace myCareers.Application.Services
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _applicationRepository;
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<JobApplicationService> _logger;
        private readonly IApplicationDocumentRepository _documentRepository;

        public JobApplicationService(
            IJobApplicationRepository applicationRepository,
            IJobPostingRepository jobPostingRepository,
            IUserRepository userRepository,
            IApplicationDocumentRepository documentRepository,
            IMapper mapper,
            ILogger<JobApplicationService> logger)
        {
            _applicationRepository = applicationRepository;
            _jobPostingRepository = jobPostingRepository;
            _userRepository = userRepository;
            _documentRepository = documentRepository;
            _mapper = mapper;
            _logger = logger;
        }


        public async Task<JobApplicationResult> CreateApplicationAsync(CreateJobApplicationDto dto, int applicantId)
        {
            try
            {  
                // Validation
                var hasApplied = await _applicationRepository.HasAppliedToJobAsync(applicantId, dto.JobPostingId);
                if (hasApplied)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "You have already applied for this position" }
                    };
                }

                var jobPosting = await _jobPostingRepository.GetByIdAsync(dto.JobPostingId);
                if (jobPosting == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Job posting not found" }
                    };
                }

                if (!jobPosting.IsActive || jobPosting.ClosingDate < DateTime.UtcNow)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "This job posting is no longer accepting applications" }
                    };
                }

                // Create application with SAFE null-coalescing for all potentially null fields
                var application = new JobApplication
                {
                    JobPostingId = dto.JobPostingId,
                    ApplicantId = applicantId,
                    Status = ApplicationStatus.Draft,

                    // Section 1: Position (with null checks)
                    JobTitle = dto.PositionInfo?.JobTitle ?? string.Empty,
                    Department = dto.PositionInfo?.Department ?? string.Empty,
                    ReferenceNumber = dto.PositionInfo?.ReferenceNumber ?? string.Empty,
                    NoticePeriod = dto.PositionInfo?.NoticePeriod ?? string.Empty,

                    // Section 2: Personal Info (with null checks)
                    FirstName = dto.PersonalInfo?.FirstName ?? string.Empty,
                    LastName = dto.PersonalInfo?.LastName ?? string.Empty,
                    DateOfBirth = dto.PersonalInfo?.DateOfBirth,
                    IdPassportNumber = dto.PersonalInfo?.IdPassportNumber ?? string.Empty,
                    Race = dto.PersonalInfo?.Race ?? Race.African,
                    Gender = dto.PersonalInfo?.Gender ?? Gender.Male,
                    HasDisability = dto.PersonalInfo?.HasDisability ?? false,
                    DisabilityDetails = dto.PersonalInfo?.DisabilityDetails,
                    IsSouthAfricanCitizen = dto.PersonalInfo?.IsSouthAfricanCitizen ?? true,
                    Nationality = dto.PersonalInfo?.Nationality,
                    HasValidWorkPermit = dto.PersonalInfo?.HasValidWorkPermit,
                    HasCriminalRecord = dto.PersonalInfo?.HasCriminalRecord ?? false,
                    CriminalRecordDetails = dto.PersonalInfo?.CriminalRecordDetails,
                    HasPendingCriminalCase = dto.PersonalInfo?.HasPendingCriminalCase ?? false,
                    PendingCriminalCaseDetails = dto.PersonalInfo?.PendingCriminalCaseDetails,
                    HasBeenDismissed = dto.PersonalInfo?.HasBeenDismissed ?? false,
                    DismissalDetails = dto.PersonalInfo?.DismissalDetails,
                    HasPendingDisciplinary = dto.PersonalInfo?.HasPendingDisciplinary ?? false,
                    PendingDisciplinaryDetails = dto.PersonalInfo?.PendingDisciplinaryDetails,
                    HasResignedPendingDisciplinary = dto.PersonalInfo?.HasResignedPendingDisciplinary ?? false,
                    HasBeenDischargedIllHealth = dto.PersonalInfo?.HasBeenDischargedIllHealth ?? false,
                    ConductsBusinessWithState = dto.PersonalInfo?.ConductsBusinessWithState ?? false,
                    BusinessWithStateDetails = dto.PersonalInfo?.BusinessWithStateDetails,
                    WillRelinquishBusinessInterests = dto.PersonalInfo?.WillRelinquishBusinessInterests ?? false,
                    PrivateSectorExperienceYears = dto.PersonalInfo?.PrivateSectorExperienceYears ?? 0,
                    PublicSectorExperienceYears = dto.PersonalInfo?.PublicSectorExperienceYears ?? 0,
                    RequiresOfficialRegistration = dto.PersonalInfo?.RequiresOfficialRegistration ?? false,
                    RegistrationDate = dto.PersonalInfo?.RegistrationDate,
                    RegistrationNumber = dto.PersonalInfo?.RegistrationNumber,

                    // Section 3: Contact Details (with null checks)
                    PhysicalAddress = dto.ContactDetails?.PhysicalAddress ?? string.Empty,
                    City = dto.ContactDetails?.City ?? string.Empty,
                    PostalCode = dto.ContactDetails?.PostalCode ?? string.Empty,
                    PhoneNumber = dto.ContactDetails?.PhoneNumber ?? string.Empty,
                    AlternativePhoneNumber = dto.ContactDetails?.AlternativePhoneNumber,
                    Email = dto.ContactDetails?.Email ?? string.Empty,
                    PreferredLanguage = dto.ContactDetails?.PreferredLanguage ?? "English",
                    PreferredCommunicationMethod = dto.ContactDetails?.PreferredCommunicationMethod ?? "Email",

                    // Sections 4-8 as JSON (with null checks)
                    LanguageProficiencies = dto.LanguageProficiencies != null && dto.LanguageProficiencies.Any()
                        ? JsonSerializer.Serialize(dto.LanguageProficiencies)
                        : null,
                    Qualifications = dto.Qualifications != null && dto.Qualifications.Any()
                        ? JsonSerializer.Serialize(dto.Qualifications)
                        : null,
                    WorkExperience = dto.WorkExperience != null && dto.WorkExperience.Any()
                        ? JsonSerializer.Serialize(dto.WorkExperience)
                        : null,
                    References = dto.References != null && dto.References.Any()
                        ? JsonSerializer.Serialize(dto.References)
                        : null,

                    AppliedDate = DateTime.UtcNow,
                    CreatedDate = DateTime.UtcNow
                };

                var createdApplication = await _applicationRepository.CreateAsync(application);

                // Link uploaded documents to the application
                if (dto.Documents?.UploadedDocumentIds != null && dto.Documents.UploadedDocumentIds.Any())
                {
                    foreach (var documentId in dto.Documents.UploadedDocumentIds)
                    {
                        await _documentRepository.LinkDocumentToApplicationAsync(documentId, createdApplication.Id);
                    }
                }

                var applicationDto = _mapper.Map<JobApplicationDto>(createdApplication);

                // Populate document URLs
                await PopulateDocumentUrlsAsync(applicationDto, createdApplication.Id);

                _logger.LogInformation("Application created: {ApplicationId} for job {JobId} by applicant {ApplicantId}",
                    createdApplication.Id, dto.JobPostingId, applicantId);

                return new JobApplicationResult
                {
                    Success = true,
                    Application = applicationDto,
                    Message = "Application saved as draft successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application for job {JobId} by applicant {ApplicantId}",
                    dto.JobPostingId, applicantId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { $"An error occurred while creating your application: {ex.Message}" }
                };
            }
        }

        public async Task<JobApplicationResult> SubmitApplicationAsync(int applicationId, int applicantId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                if (application.ApplicantId != applicantId)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "You are not authorized to submit this application" }
                    };
                }

                if (application.Status != ApplicationStatus.Draft)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Only draft applications can be submitted" }
                    };
                }

                // Validate before submission
                var validationErrors = await ValidateApplicationForSubmissionAsync(applicationId, applicantId);
                if (validationErrors.Any())
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = validationErrors
                    };
                }

                application.Status = ApplicationStatus.Submitted;
                application.AppliedDate = DateTime.UtcNow;
                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Application submitted: {ApplicationId}", applicationId);

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Application submitted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { $"An error occurred while submitting your application: {ex.Message}" }
                };
            }
        }

        public async Task<JobApplicationResult> WithdrawApplicationAsync(int applicationId, int applicantId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                if (application.ApplicantId != applicantId)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "You are not authorized to withdraw this application" }
                    };
                }

                if (application.Status != ApplicationStatus.Submitted &&
                    application.Status != ApplicationStatus.UnderReview)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "This application cannot be withdrawn" }
                    };
                }

                application.Status = ApplicationStatus.Withdrawn;
                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Application withdrawn: {ApplicationId}", applicationId);

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Application withdrawn successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error withdrawing application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while withdrawing your application" }
                };
            }
        }

        public async Task<JobApplicationResult> UpdateApplicationStatusAsync(int applicationId, ApplicationStatus status)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                application.Status = status;

                if (status == ApplicationStatus.UnderReview)
                {
                    application.ReviewedDate = DateTime.UtcNow;
                }

                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Application status updated: {ApplicationId} to {Status}",
                    applicationId, status);

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Application status updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application status {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while updating the application status" }
                };
            }
        }

        public async Task<JobApplicationResult> UpdateApplicationAsync(int applicationId, CreateJobApplicationDto dto, int applicantId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                if (application.ApplicantId != applicantId)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "You are not authorized to update this application" }
                    };
                }

                if (application.Status != ApplicationStatus.Draft)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Only draft applications can be updated" }
                    };
                }

                // Update all fields similar to Create
                application.FirstName = dto.PersonalInfo.FirstName;
                application.LastName = dto.PersonalInfo.LastName;
                // ... update other fields

                await _applicationRepository.UpdateAsync(application);

                var applicationDto = _mapper.Map<JobApplicationDto>(application);

                return new JobApplicationResult
                {
                    Success = true,
                    Application = applicationDto,
                    Message = "Application updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while updating your application" }
                };
            }
        }

        public async Task<JobApplicationDto?> GetApplicationByIdAsync(int id)
        {
            var application = await _applicationRepository.GetByIdWithDetailsAsync(id);
            if (application == null)
                return null;

            var dto = _mapper.Map<JobApplicationDto>(application);
            await PopulateDocumentUrlsAsync(dto, id);

            return dto;
        }

        public async Task<List<JobApplicationDto>> GetApplicationsByApplicantAsync(int applicantId)
        {
            var applications = await _applicationRepository.GetByApplicantIdAsync(applicantId);
            var dtos = _mapper.Map<List<JobApplicationDto>>(applications);

            // Populate document URLs for each application
            foreach (var dto in dtos)
            {
                await PopulateDocumentUrlsAsync(dto, dto.Id);
            }

            return dtos;
        }

        public async Task<List<JobApplicationDto>> GetApplicationsByJobPostingAsync(int jobPostingId)
        {
            var applications = await _applicationRepository.GetByJobPostingIdAsync(jobPostingId);
            var dtos = _mapper.Map<List<JobApplicationDto>>(applications);

            // Populate document URLs for each application
            foreach (var dto in dtos)
            {
                await PopulateDocumentUrlsAsync(dto, dto.Id);
            }

            return dtos;
        }

        public async Task<ApplicationStatisticsDto> GetApplicationStatisticsAsync(int applicantId)
        {
            var totalApplications = await _applicationRepository.GetApplicationCountByApplicantAsync(applicantId);
            var underReview = await _applicationRepository.GetApplicationCountByStatusAsync(applicantId, ApplicationStatus.UnderReview);
            var interviews = await _applicationRepository.GetApplicationCountByStatusAsync(applicantId, ApplicationStatus.InterviewScheduled);
            var availableJobs = await _jobPostingRepository.GetActiveJobCountAsync();

            return new ApplicationStatisticsDto
            {
                TotalApplications = totalApplications,
                UnderReviewCount = underReview,
                InterviewsScheduledCount = interviews,
                AvailableJobsCount = availableJobs
            };
        }

        public async Task<bool> HasAppliedToJobAsync(int applicantId, int jobPostingId)
        {
            return await _applicationRepository.HasAppliedToJobAsync(applicantId, jobPostingId);
        }

        // ===== SPRINT 4: RECRUITER-SIDE APPLICATION MANAGEMENT =====

        public async Task<JobApplicationResult> RejectApplicationAsync(int applicationId, string? rejectionReason)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Business rule: Can only reject Submitted, UnderReview, or Shortlisted applications
                if (application.Status == ApplicationStatus.Accepted ||
                    application.Status == ApplicationStatus.Rejected ||
                    application.Status == ApplicationStatus.Withdrawn ||
                    application.Status == ApplicationStatus.Draft ||
                    application.Status == ApplicationStatus.InterviewScheduled)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { $"Cannot reject application with status: {application.Status}" }
                    };
                }

                application.Status = ApplicationStatus.Rejected;
                application.ReviewNotes = string.IsNullOrWhiteSpace(rejectionReason)
                    ? application.ReviewNotes
                    : $"{application.ReviewNotes}\n[REJECTED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {rejectionReason}".Trim();
                application.ReviewedDate = DateTime.UtcNow;

                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Application {ApplicationId} rejected. Reason: {Reason}",
                    applicationId, rejectionReason ?? "No reason provided");

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Application rejected successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { $"An error occurred while rejecting the application: {ex.Message}" }
                };
            }
        }

        public async Task<JobApplicationResult> BulkRejectApplicationsAsync(int jobPostingId, List<int> applicationIds, string? rejectionReason)
        {
            try
            {
                if (!applicationIds.Any())
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "No applications selected for rejection" }
                    };
                }

                var errors = new List<string>();
                var successCount = 0;

                foreach (var appId in applicationIds)
                {
                    var application = await _applicationRepository.GetByIdAsync(appId);

                    if (application == null)
                    {
                        errors.Add($"Application {appId} not found");
                        continue;
                    }

                    if (application.JobPostingId != jobPostingId)
                    {
                        errors.Add($"Application {appId} does not belong to job posting {jobPostingId}");
                        continue;
                    }

                    // Business rule validation
                    if (application.Status == ApplicationStatus.Accepted ||
                        application.Status == ApplicationStatus.Rejected ||
                        application.Status == ApplicationStatus.Withdrawn ||
                        application.Status == ApplicationStatus.Draft ||
                        application.Status == ApplicationStatus.InterviewScheduled)
                    {
                        errors.Add($"Application {appId} cannot be rejected (status: {application.Status})");
                        continue;
                    }

                    application.Status = ApplicationStatus.Rejected;
                    application.ReviewNotes = string.IsNullOrWhiteSpace(rejectionReason)
                        ? application.ReviewNotes
                        : $"{application.ReviewNotes}\n[BULK REJECTED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {rejectionReason}".Trim();
                    application.ReviewedDate = DateTime.UtcNow;

                    await _applicationRepository.UpdateAsync(application);
                    successCount++;
                }

                _logger.LogInformation("{SuccessCount} applications rejected for job {JobPostingId}",
                    successCount, jobPostingId);

                return new JobApplicationResult
                {
                    Success = successCount > 0,
                    Message = $"{successCount} application(s) rejected successfully",
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk rejecting applications for job {JobPostingId}", jobPostingId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred during bulk rejection" }
                };
            }
        }

        public async Task<JobApplicationResult> AcceptApplicationAsync(int applicationId, DateTime? interviewDate, string? interviewNotes)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Business rule: Can only accept UnderReview or Shortlisted applications
                if (application.Status != ApplicationStatus.UnderReview &&
                    application.Status != ApplicationStatus.Shortlisted)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { $"Cannot accept application with status: {application.Status}. Only UnderReview or Shortlisted applications can be accepted." }
                    };
                }

                // If interview date provided, schedule interview; otherwise just accept
                if (interviewDate.HasValue)
                {
                    if (interviewDate.Value < DateTime.UtcNow)
                    {
                        return new JobApplicationResult
                        {
                            Success = false,
                            Errors = new List<string> { "Interview date cannot be in the past" }
                        };
                    }

                    application.Status = ApplicationStatus.InterviewScheduled;
                    application.InterviewDate = interviewDate.Value;
                    application.ReviewNotes = string.IsNullOrWhiteSpace(interviewNotes)
                        ? application.ReviewNotes
                        : $"{application.ReviewNotes}\n[INTERVIEW SCHEDULED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {interviewNotes}".Trim();
                }
                else
                {
                    application.Status = ApplicationStatus.Accepted;
                }

                application.ReviewedDate = DateTime.UtcNow;
                await _applicationRepository.UpdateAsync(application);

                var message = interviewDate.HasValue
                    ? $"Application accepted and interview scheduled for {interviewDate.Value:yyyy-MM-dd HH:mm}"
                    : "Application accepted successfully";

                _logger.LogInformation("Application {ApplicationId} accepted. Interview: {InterviewDate}",
                    applicationId, interviewDate?.ToString("yyyy-MM-dd HH:mm") ?? "Not scheduled");

                return new JobApplicationResult
                {
                    Success = true,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while accepting the application" }
                };
            }
        }

        public async Task<JobApplicationResult> ScheduleInterviewAsync(int applicationId, DateTime interviewDate, string? interviewNotes)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Business rule: Can schedule interview for Accepted, Shortlisted, or already InterviewScheduled applications
                if (application.Status != ApplicationStatus.Accepted &&
                    application.Status != ApplicationStatus.Shortlisted &&
                    application.Status != ApplicationStatus.InterviewScheduled)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { $"Cannot schedule interview for application with status: {application.Status}" }
                    };
                }

                if (interviewDate < DateTime.UtcNow)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Interview date cannot be in the past" }
                    };
                }

                application.Status = ApplicationStatus.InterviewScheduled;
                application.InterviewDate = interviewDate;
                application.ReviewNotes = string.IsNullOrWhiteSpace(interviewNotes)
                    ? application.ReviewNotes
                    : $"{application.ReviewNotes}\n[INTERVIEW SCHEDULED] {DateTime.UtcNow:yyyy-MM-dd HH:mm}: {interviewNotes}".Trim();

                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Interview scheduled for application {ApplicationId} on {InterviewDate}",
                    applicationId, interviewDate.ToString("yyyy-MM-dd HH:mm"));

                return new JobApplicationResult
                {
                    Success = true,
                    Message = $"Interview scheduled successfully for {interviewDate:yyyy-MM-dd HH:mm}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling interview for application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while scheduling the interview" }
                };
            }
        }

        public async Task<JobApplicationResult> DeleteApplicationAsync(int applicationId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                // Business rule: Cannot delete Accepted or InterviewScheduled applications
                if (application.Status == ApplicationStatus.Accepted ||
                    application.Status == ApplicationStatus.InterviewScheduled)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Cannot delete accepted or interview-scheduled applications. Please reject them first." }
                    };
                }

                // Delete associated documents first
                var documents = await _documentRepository.GetByApplicationIdAsync(applicationId);
                foreach (var doc in documents)
                {
                    await _documentRepository.DeleteAsync(doc.FileId);
                }

                await _applicationRepository.DeleteAsync(applicationId);

                _logger.LogWarning("Application {ApplicationId} permanently deleted", applicationId);

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Application deleted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while deleting the application" }
                };
            }
        }

        public async Task<JobApplicationResult> UpdateReviewNotesAsync(int applicationId, string reviewNotes)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                {
                    return new JobApplicationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Application not found" }
                    };
                }

                application.ReviewNotes = reviewNotes;
                await _applicationRepository.UpdateAsync(application);

                _logger.LogInformation("Review notes updated for application {ApplicationId}", applicationId);

                return new JobApplicationResult
                {
                    Success = true,
                    Message = "Review notes updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating review notes for application {ApplicationId}", applicationId);
                return new JobApplicationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while updating review notes" }
                };
            }
        }

        public async Task<JobApplicationStatisticsDto> GetJobApplicationStatisticsAsync(int jobPostingId)
        {
            try
            {
                var applications = await _applicationRepository.GetByJobPostingIdAsync(jobPostingId);

                var statistics = new JobApplicationStatisticsDto
                {
                    TotalApplications = applications.Count,
                    SubmittedCount = applications.Count(a => a.Status == ApplicationStatus.Submitted),
                    UnderReviewCount = applications.Count(a => a.Status == ApplicationStatus.UnderReview),
                    ShortlistedCount = applications.Count(a => a.Status == ApplicationStatus.Shortlisted),
                    InterviewScheduledCount = applications.Count(a => a.Status == ApplicationStatus.InterviewScheduled),
                    AcceptedCount = applications.Count(a => a.Status == ApplicationStatus.Accepted),
                    RejectedCount = applications.Count(a => a.Status == ApplicationStatus.Rejected),
                    WithdrawnCount = applications.Count(a => a.Status == ApplicationStatus.Withdrawn),
                    NewApplications = applications.Count(a => a.Status == ApplicationStatus.Submitted &&
                                                            a.AppliedDate >= DateTime.UtcNow.AddDays(-7))
                };

                // Status breakdown
                statistics.StatusBreakdown = applications
                    .GroupBy(a => a.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Date statistics
                if (applications.Any())
                {
                    statistics.OldestApplicationDate = applications.Min(a => a.AppliedDate);
                    statistics.NewestApplicationDate = applications.Max(a => a.AppliedDate);

                    // Calculate average processing days for reviewed applications
                    var reviewedApps = applications.Where(a => a.ReviewedDate.HasValue).ToList();
                    if (reviewedApps.Any())
                    {
                        statistics.AverageProcessingDays = reviewedApps
                            .Average(a => (a.ReviewedDate!.Value - a.AppliedDate).TotalDays);
                    }
                }

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics for job {JobPostingId}", jobPostingId);
                return new JobApplicationStatisticsDto();
            }
        }

        public async Task<List<JobApplicationDto>> GetApplicationsByStatusAsync(int jobPostingId, ApplicationStatus status)
        {
            try
            {
                var applications = await _applicationRepository.GetByJobPostingIdAsync(jobPostingId);
                var filteredApps = applications.Where(a => a.Status == status).ToList();

                var dtos = _mapper.Map<List<JobApplicationDto>>(filteredApps);

                // Populate document URLs for each application
                foreach (var dto in dtos)
                {
                    await PopulateDocumentUrlsAsync(dto, dto.Id);
                }

                return dtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting applications by status for job {JobPostingId}", jobPostingId);
                return new List<JobApplicationDto>();
            }
        }

        public async Task<bool> CanRejectApplicationAsync(int applicationId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                    return false;

                // Can reject: Submitted, UnderReview, Shortlisted
                return application.Status == ApplicationStatus.Submitted ||
                       application.Status == ApplicationStatus.UnderReview ||
                       application.Status == ApplicationStatus.Shortlisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application {ApplicationId} can be rejected", applicationId);
                return false;
            }
        }

        public async Task<bool> CanAcceptApplicationAsync(int applicationId)
        {
            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);
                if (application == null)
                    return false;

                // Can accept: UnderReview, Shortlisted
                return application.Status == ApplicationStatus.UnderReview ||
                       application.Status == ApplicationStatus.Shortlisted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if application {ApplicationId} can be accepted", applicationId);
                return false;
            }
        }

        // ===== PRIVATE HELPER METHODS =====

        // VALIDATION METHOD 
        private async Task<List<string>> ValidateApplicationForSubmissionAsync(int applicationId, int applicantId)
        {
            var errors = new List<string>();

            try
            {
                var application = await _applicationRepository.GetByIdAsync(applicationId);

                if (application == null)
                {
                    errors.Add("Application not found");
                    return errors;
                }

                // Validate Personal Information
                if (string.IsNullOrWhiteSpace(application.FirstName))
                    errors.Add("First name is required");

                if (string.IsNullOrWhiteSpace(application.LastName))
                    errors.Add("Last name is required");

                if (application.DateOfBirth == null)
                    errors.Add("Date of birth is required");
                else
                {
                    var age = DateTime.UtcNow.Year - application.DateOfBirth.Value.Year;
                    if (age < 18)
                        errors.Add("Applicant must be at least 18 years old");
                    if (age > 100)
                        errors.Add("Please enter a valid date of birth");
                }

                if (string.IsNullOrWhiteSpace(application.IdPassportNumber))
                    errors.Add("ID/Passport number is required");

                // Validate Contact Details
                if (string.IsNullOrWhiteSpace(application.PhoneNumber))
                    errors.Add("Phone number is required");

                if (string.IsNullOrWhiteSpace(application.Email))
                    errors.Add("Email address is required");
                else if (!IsValidEmail(application.Email))
                    errors.Add("Please enter a valid email address");

                if (string.IsNullOrWhiteSpace(application.PhysicalAddress))
                    errors.Add("Physical address is required");

                if (string.IsNullOrWhiteSpace(application.City))
                    errors.Add("City is required");

                if (string.IsNullOrWhiteSpace(application.PostalCode))
                    errors.Add("Postal code is required");

                // Validate Language Proficiencies
                if (string.IsNullOrWhiteSpace(application.LanguageProficiencies))
                {
                    errors.Add("At least one language proficiency is required");
                }
                else
                {
                    try
                    {
                        var languages = JsonSerializer.Deserialize<List<object>>(application.LanguageProficiencies);
                        if (languages == null || !languages.Any())
                        {
                            errors.Add("At least one language proficiency is required");
                        }
                    }
                    catch
                    {
                        errors.Add("Invalid language proficiency data");
                    }
                }

                // Validate Qualifications
                if (string.IsNullOrWhiteSpace(application.Qualifications))
                {
                    errors.Add("At least one qualification is required");
                }
                else
                {
                    try
                    {
                        var qualifications = JsonSerializer.Deserialize<List<object>>(application.Qualifications);
                        if (qualifications == null || !qualifications.Any())
                        {
                            errors.Add("At least one qualification is required");
                        }
                    }
                    catch
                    {
                        errors.Add("Invalid qualification data");
                    }
                }

                // Validate References
                if (string.IsNullOrWhiteSpace(application.References))
                {
                    errors.Add("At least two references are required");
                }
                else
                {
                    try
                    {
                        var references = JsonSerializer.Deserialize<List<object>>(application.References);
                        if (references == null || references.Count < 2)
                        {
                            errors.Add("At least two references are required");
                        }
                    }
                    catch
                    {
                        errors.Add("Invalid reference data");
                    }
                }

                // Validate Position Information
                if (string.IsNullOrWhiteSpace(application.JobTitle))
                    errors.Add("Job title is required");

                if (string.IsNullOrWhiteSpace(application.Department))
                    errors.Add("Department is required");

                // Additional validations based on conditional fields
                if (!application.IsSouthAfricanCitizen)
                {
                    if (string.IsNullOrWhiteSpace(application.Nationality))
                        errors.Add("Nationality is required for non-South African citizens");

                    if (!application.HasValidWorkPermit.HasValue)
                        errors.Add("Work permit status is required for non-South African citizens");
                    else if (!application.HasValidWorkPermit.Value)
                        errors.Add("A valid work permit is required for non-South African citizens");
                }

                if (application.HasDisability && string.IsNullOrWhiteSpace(application.DisabilityDetails))
                    errors.Add("Disability details are required when disability is indicated");

                if (application.HasCriminalRecord && string.IsNullOrWhiteSpace(application.CriminalRecordDetails))
                    errors.Add("Criminal record details are required when indicated");

                if (application.HasPendingCriminalCase && string.IsNullOrWhiteSpace(application.PendingCriminalCaseDetails))
                    errors.Add("Pending criminal case details are required when indicated");

                if (application.HasBeenDismissed && string.IsNullOrWhiteSpace(application.DismissalDetails))
                    errors.Add("Dismissal details are required when indicated");

                if (application.HasPendingDisciplinary && string.IsNullOrWhiteSpace(application.PendingDisciplinaryDetails))
                    errors.Add("Pending disciplinary details are required when indicated");

                if (application.ConductsBusinessWithState)
                {
                    if (string.IsNullOrWhiteSpace(application.BusinessWithStateDetails))
                        errors.Add("Business with state details are required when indicated");

                    if (!application.WillRelinquishBusinessInterests)
                        errors.Add("You must be willing to relinquish business interests if appointed");
                }

                if (application.RequiresOfficialRegistration)
                {
                    if (!application.RegistrationDate.HasValue)
                        errors.Add("Registration date is required when official registration is indicated");

                    if (string.IsNullOrWhiteSpace(application.RegistrationNumber))
                        errors.Add("Registration number is required when official registration is indicated");
                }

                // Validate Required Documents
                var documents = await _documentRepository.GetByApplicationIdAsync(applicationId);

                if (!documents.Any(d => d.DocumentType.Equals("Resume", StringComparison.OrdinalIgnoreCase)))
                    errors.Add("Resume/CV document is required");

                if (!documents.Any(d => d.DocumentType.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                    errors.Add("ID/Passport document is required");

                if (!documents.Any(d => d.DocumentType.Equals("Qualification", StringComparison.OrdinalIgnoreCase)))
                    errors.Add("Qualification certificate is required");

                if (!documents.Any(d => d.DocumentType.Equals("Z83", StringComparison.OrdinalIgnoreCase)))
                    errors.Add("Signed Z83 form is required");

                return errors;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating application {ApplicationId}", applicationId);
                errors.Add("An error occurred while validating the application");
                return errors;
            }
        }

        // POPULATE DOCUMENT URLS
        private async Task PopulateDocumentUrlsAsync(JobApplicationDto dto, int applicationId)
        {
            try
            {
                var documents = await _documentRepository.GetByApplicationIdAsync(applicationId);

                foreach (var doc in documents)
                {
                    switch (doc.DocumentType.ToLower())
                    {
                        case "resume":
                            dto.ResumeUrl = doc.FileUrl;
                            break;
                        case "id":
                        case "iddocument":
                            dto.IdDocumentUrl = doc.FileUrl;
                            break;
                        case "qualification":
                        case "qualificationcertificate":
                            dto.QualificationCertificateUrl = doc.FileUrl;
                            break;
                        case "transcript":
                            dto.TranscriptUrl = doc.FileUrl;
                            break;
                        case "z83":
                        case "z83form":
                            dto.Z83FormUrl = doc.FileUrl;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating document URLs for application {ApplicationId}", applicationId);
                // Don't throw - just log the error and continue
            }
        }

        // EMAIL VALIDATION HELPER
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}