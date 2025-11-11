using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Application.DTOs.JobApplication;
using myCareers.Application.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Interfaces;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    [Authorize(Roles = "Applicant")]
    public class ApplicationController : Controller
    {
        private readonly IJobApplicationService _applicationService;
        private readonly IJobPostingService _jobPostingService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<ApplicationController> _logger;
        private readonly IApplicationDocumentRepository _documentRepository;

        public ApplicationController(
            IJobApplicationService applicationService,
            IJobPostingService jobPostingService,
            IAuthenticationService authenticationService,
            IFileUploadService fileUploadService,
            IApplicationDocumentRepository documentRepository,
            ILogger<ApplicationController> logger)
        {
            _applicationService = applicationService;
            _jobPostingService = jobPostingService;
            _authenticationService = authenticationService;
            _fileUploadService = fileUploadService;
            _documentRepository = documentRepository;
            _logger = logger;
        }

        // POST: Application/UploadFile
        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string documentType, int? applicationId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No file selected" });
                }

                var applicantId = await GetCurrentApplicantIdAsync();
                if (applicantId == 0)
                {
                    return Json(new { success = false, message = "Applicant profile not found" });
                }

                // Validate file type and size
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}"
                    });
                }

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    return Json(new { success = false, message = "File size exceeds 5MB limit" });
                }

                // Upload file to storage
                var uploadResult = await _fileUploadService.UploadFileAsync(file, "applications");

                if (!uploadResult.Success)
                {
                    return Json(new { success = false, errors = uploadResult.Errors });
                }

                // Save document metadata to database
                var document = new ApplicationDocument
                {
                    FileId = Guid.NewGuid(),
                    FilePath = uploadResult.FilePath!,
                    FileName = uploadResult.FileName!,
                    FileUrl = uploadResult.FileUrl!,
                    FileSize = uploadResult.FileSize,
                    ApplicantId = applicantId,
                    JobApplicationId = applicationId, // Can be null for drafts
                    FileType = extension.TrimStart('.'),
                    DocumentType = documentType,
                    CreatedOn = DateTime.UtcNow
                };

                await _documentRepository.CreateAsync(document);

                _logger.LogInformation("File uploaded: {FileName} by applicant {ApplicantId}",
                    document.FileName, applicantId);

                return Json(new
                {
                    success = true,
                    fileId = document.FileId,
                    fileUrl = document.FileUrl,
                    fileName = document.FileName,
                    fileSize = document.FileSize,
                    documentType = document.DocumentType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return Json(new { success = false, message = "An error occurred while uploading the file" });
            }
        }

        // GET: Application/MyApplications
        [HttpGet]
        public async Task<IActionResult> MyApplications()
        {
            var applicantId = await GetCurrentApplicantIdAsync();
            if (applicantId == 0)
            {
                TempData["ErrorMessage"] = "Applicant profile not found";
                return RedirectToAction("Index", "Dashboard");
            }

            var applications = await _applicationService.GetApplicationsByApplicantAsync(applicantId);
            return View(applications);
        }

        // GET: Application/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var application = await _applicationService.GetApplicationByIdAsync(id);

            if (application == null)
            {
                TempData["ErrorMessage"] = "Application not found";
                return RedirectToAction(nameof(MyApplications));
            }

            var applicantId = await GetCurrentApplicantIdAsync();
            if (application.ApplicantId != applicantId)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this application";
                return RedirectToAction(nameof(MyApplications));
            }

            return View(application);
        }

        // GET: Application/Apply/5
        [HttpGet]
        public async Task<IActionResult> Apply(int jobId)
        {
            var applicantId = await GetCurrentApplicantIdAsync();
            if (applicantId == 0)
            {
                TempData["ErrorMessage"] = "Applicant profile not found";
                return RedirectToAction("Index", "Dashboard");
            }

            // Check if already applied
            var hasApplied = await _applicationService.HasAppliedToJobAsync(applicantId, jobId);
            if (hasApplied)
            {
                TempData["ErrorMessage"] = "You have already applied for this position";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            // Get job details
            var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
            if (job == null)
            {
                TempData["ErrorMessage"] = "Job posting not found";
                return RedirectToAction("Browse", "Jobs");
            }

            if (!job.IsActive || job.IsClosed)
            {
                TempData["ErrorMessage"] = "This job posting is no longer accepting applications";
                return RedirectToAction("Details", "Jobs", new { id = jobId });
            }

            // Get user profile to pre-populate
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);

            // Initialize the application DTO with pre-populated data
            var model = new CreateJobApplicationDto
            {
                JobPostingId = jobId,
                PositionInfo = new PositionInfoDto
                {
                    JobTitle = job.Title,
                    Department = job.Department ?? string.Empty,
                    ReferenceNumber = $"REF-{job.Id}-{DateTime.UtcNow:yyyyMMdd}",
                    NoticePeriod = "1 month"
                },
                PersonalInfo = new PersonalInfoDto
                {
                    FirstName = user?.FirstName ?? string.Empty,
                    LastName = user?.LastName ?? string.Empty,
                    IsSouthAfricanCitizen = true
                },
                ContactDetails = new ContactDetailsDto
                {
                    Email = user?.Email ?? string.Empty,
                    PhoneNumber = user?.PhoneNumber ?? string.Empty,
                    PreferredLanguage = "English",
                    PreferredCommunicationMethod = "Email"
                }
            };

            ViewBag.JobTitle = job.Title;
            ViewBag.Department = job.Department;
            ViewBag.JobId = jobId;
            ViewBag.CurrentStep = 1;

            return View(model);
        }

        // POST: Application/SaveDraft
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDraft([FromBody] SaveDraftRequest request)
        {
            try
            {
                var applicantId = await GetCurrentApplicantIdAsync();
                if (applicantId == 0)
                {
                    return Json(new { success = false, message = "Applicant profile not found" });
                }

                JobApplicationResult result;

                // Update existing draft or create new one
                if (request.ApplicationId.HasValue && request.ApplicationId.Value > 0)
                {
                    _logger.LogInformation("Updating existing draft: {ApplicationId}", request.ApplicationId.Value);
                    result = await _applicationService.UpdateApplicationAsync(
                        request.ApplicationId.Value,
                        request.Application,
                        applicantId);
                }
                else
                {
                    _logger.LogInformation("Creating new draft application for job {JobId}", request.Application.JobPostingId);
                    result = await _applicationService.CreateApplicationAsync(request.Application, applicantId);
                }

                if (result.Success)
                {
                    // Link any uploaded documents to this application
                    if (request.Application.Documents?.UploadedDocumentIds != null)
                    {
                        foreach (var fileId in request.Application.Documents.UploadedDocumentIds)
                        {
                            await _documentRepository.UpdateApplicationIdAsync(fileId, result.Application!.Id);
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        applicationId = result.Application?.Id,
                        message = result.Message ?? "Draft saved successfully"
                    });
                }

                return Json(new { success = false, errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving draft");
                return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        // GET: Application/GetDocuments
        [HttpGet]
        public async Task<IActionResult> GetDocuments(int? applicationId, int? applicantId)
        {
            try
            {
                var currentApplicantId = await GetCurrentApplicantIdAsync();

                IEnumerable<ApplicationDocument> documents;

                if (applicationId.HasValue)
                {
                    documents = await _documentRepository.GetByApplicationIdAsync(applicationId.Value);
                }
                else
                {
                    // Get documents for applicant that aren't linked to any application yet
                    documents = await _documentRepository.GetOrphanedDocumentsByApplicantAsync(currentApplicantId);
                }

                return Json(new
                {
                    success = true,
                    documents = documents.Select(d => new
                    {
                        fileId = d.FileId,
                        fileName = d.FileName,
                        fileUrl = d.FileUrl,
                        fileSize = d.FileSize,
                        documentType = d.DocumentType,
                        createdOn = d.CreatedOn
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting documents");
                return Json(new { success = false, message = "Error retrieving documents" });
            }
        }

        // DELETE: Application/DeleteDocument/guid
        [HttpPost]
        public async Task<IActionResult> DeleteDocument(Guid fileId)
        {
            try
            {
                var document = await _documentRepository.GetByIdAsync(fileId);
                if (document == null)
                {
                    return Json(new { success = false, message = "Document not found" });
                }

                var applicantId = await GetCurrentApplicantIdAsync();
                if (document.ApplicantId != applicantId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Delete file from storage
                await _fileUploadService.DeleteFileAsync(document.FilePath);

                // Delete from database
                await _documentRepository.DeleteAsync(fileId);

                _logger.LogInformation("Document deleted: {FileId}", fileId);

                return Json(new { success = true, message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return Json(new { success = false, message = "Error deleting document" });
            }
        }

        // POST: Application/Submit/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest request)
        {
            try
            {
                // Get current applicant ID
                var applicantId = await GetCurrentApplicantIdAsync();
                if (applicantId == 0)
                {
                    return Json(new { success = false, message = "Applicant profile not found" });
                }

                JobApplicationResult result;

                // If no applicationId, create the application first
                if (!request.ApplicationId.HasValue || request.ApplicationId.Value == 0)
                {
                    _logger.LogInformation("Creating new application before submission for applicant {ApplicantId}", applicantId);

                    result = await _applicationService.CreateApplicationAsync(request.Application, applicantId);

                    if (!result.Success)
                    {
                        _logger.LogWarning("Failed to create application: {Errors}", string.Join(", ", result.Errors));
                        return Json(new
                        {
                            success = false,
                            message = "Failed to create application",
                            errors = result.Errors
                        });
                    }

                    request.ApplicationId = result.Application!.Id;
                    _logger.LogInformation("Application created with ID {ApplicationId}", request.ApplicationId);
                }
                else
                {
                    _logger.LogInformation("Submitting existing application {ApplicationId}", request.ApplicationId.Value);
                }

                // Link uploaded documents to the application
                if (request.Application.Documents?.UploadedDocumentIds != null &&
                    request.Application.Documents.UploadedDocumentIds.Any())
                {
                    _logger.LogInformation("Linking {Count} documents to application {ApplicationId}",
                        request.Application.Documents.UploadedDocumentIds.Count,
                        request.ApplicationId.Value);

                    foreach (var fileId in request.Application.Documents.UploadedDocumentIds)
                    {
                        try
                        {
                            await _documentRepository.UpdateApplicationIdAsync(fileId, request.ApplicationId.Value);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to link document {FileId} to application {ApplicationId}",
                                fileId, request.ApplicationId.Value);
                            // Continue with other documents
                        }
                    }
                }

                // Submit the application (FIXED: correct parameter order)
                result = await _applicationService.SubmitApplicationAsync(applicantId, request.ApplicationId.Value);

                if (result.Success)
                {
                    _logger.LogInformation("Application {ApplicationId} submitted successfully by applicant {ApplicantId}",
                        request.ApplicationId.Value, applicantId);

                    // Optional: Send confirmation email
                    // await _emailService.SendApplicationConfirmationAsync(applicantId, request.ApplicationId.Value);

                    return Json(new
                    {
                        success = true,
                        applicationId = request.ApplicationId.Value,
                        message = "Application submitted successfully!"
                    });
                }

                _logger.LogWarning("Failed to submit application {ApplicationId}: {Errors}",
                    request.ApplicationId.Value, string.Join(", ", result.Errors));

                return Json(new
                {
                    success = false,
                    message = "Failed to submit application",
                    errors = result.Errors
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized submission attempt");
                return Unauthorized(new
                {
                    success = false,
                    message = "You are not authorized to submit this application"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation during submission");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error submitting application");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while submitting your application. Please try again or contact support.",
                    error = ex.Message
                });
            }
        }

        // POST: Application/Withdraw/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(int id)
        {
            var applicantId = await GetCurrentApplicantIdAsync();
            if (applicantId == 0)
            {
                TempData["ErrorMessage"] = "Applicant profile not found";
                return RedirectToAction("Index", "Dashboard");
            }

            var result = await _applicationService.WithdrawApplicationAsync(id, applicantId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Application withdrawn successfully";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join(", ", result.Errors);
            }

            return RedirectToAction(nameof(MyApplications));
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        private async Task<int> GetCurrentApplicantIdAsync()
        {
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);
            return user?.ApplicantProfile?.Id ?? 0;
        }
    }

    // Request DTOs
    public class SaveDraftRequest
    {
        public int? ApplicationId { get; set; }
        public CreateJobApplicationDto Application { get; set; } = null!;
    }

    public class SubmitApplicationRequest
    {
        public int? ApplicationId { get; set; }
        public CreateJobApplicationDto Application { get; set; } = null!;
    }
}