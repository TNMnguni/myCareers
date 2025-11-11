using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Application.Interfaces;
using myCareers.Core.Enums;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
  

        [Authorize(Roles = "Recruiter")]
        public class RecruiterApplicationController : Controller
        {
            private readonly IJobApplicationService _applicationService;
            private readonly IJobPostingService _jobPostingService;
            private readonly IAuthenticationService _authenticationService;
            private readonly ILogger<RecruiterApplicationController> _logger;

            public RecruiterApplicationController(
                IJobApplicationService applicationService,
                IJobPostingService jobPostingService,
                IAuthenticationService authenticationService,
                ILogger<RecruiterApplicationController> logger)
            {
                _applicationService = applicationService;
                _jobPostingService = jobPostingService;
                _authenticationService = authenticationService;
                _logger = logger;
            }

            // GET: RecruiterApplication/JobApplications/5
            /// <summary>
            /// View all applications for a specific job posting
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> JobApplications(int jobId)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        TempData["ErrorMessage"] = "Recruiter profile not found";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    // Verify job belongs to recruiter
                    var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
                    if (job == null)
                    {
                        TempData["ErrorMessage"] = "Job posting not found";
                        return RedirectToAction("Index", "JobPosting");
                    }

                    if (job.RecruiterId != recruiterId)
                    {
                        TempData["ErrorMessage"] = "You are not authorized to view applications for this job";
                        return RedirectToAction("Index", "JobPosting");
                    }

                    var applications = await _applicationService.GetApplicationsByJobPostingAsync(jobId);

                    ViewBag.JobTitle = job.Title;
                    ViewBag.JobId = jobId;
                    ViewBag.Department = job.Department;

                    return View(applications);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading applications for job {JobId}", jobId);
                    TempData["ErrorMessage"] = "An error occurred while loading applications";
                    return RedirectToAction("Index", "JobPosting");
                }
            }

            // GET: RecruiterApplication/ViewApplication/5
            /// <summary>
            /// View application details and automatically mark as "Under Review"
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> ViewApplication(int id)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        TempData["ErrorMessage"] = "Recruiter profile not found";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        TempData["ErrorMessage"] = "Application not found";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    // Verify job belongs to recruiter
                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        TempData["ErrorMessage"] = "You are not authorized to view this application";
                        return RedirectToAction("Index", "Dashboard");
                    }

                    // Auto-update status to UnderReview if currently Submitted
                    if (application.Status == ApplicationStatus.Submitted)
                    {
                        await _applicationService.UpdateApplicationStatusAsync(id, ApplicationStatus.UnderReview);
                        application.Status = ApplicationStatus.UnderReview;
                        application.ReviewedDate = DateTime.UtcNow;

                        _logger.LogInformation("Application {ApplicationId} automatically marked as Under Review by recruiter {RecruiterId}",
                            id, recruiterId);
                    }

                    return View(application);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error viewing application {ApplicationId}", id);
                    TempData["ErrorMessage"] = "An error occurred while loading the application";
                    return RedirectToAction("Index", "Dashboard");
                }
            }

            // POST: RecruiterApplication/RejectApplication
            /// <summary>
            /// Manually reject a single application
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> RejectApplication(int id, string? rejectionReason)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.RejectApplicationAsync(id, rejectionReason);

                    if (result.Success)
                    {
                        _logger.LogInformation("Application {ApplicationId} rejected by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = "Application rejected successfully" });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rejecting application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while rejecting the application" });
                }
            }

            // POST: RecruiterApplication/BulkReject
            /// <summary>
            /// Automatically reject multiple applications at once
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> BulkReject(int jobId, List<int> applicationIds, string? rejectionReason)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify job belongs to recruiter
                    var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.BulkRejectApplicationsAsync(jobId, applicationIds, rejectionReason);

                    if (result.Success)
                    {
                        _logger.LogInformation("{Count} applications rejected for job {JobId} by recruiter {RecruiterId}",
                            applicationIds.Count, jobId, recruiterId);
                        return Json(new { success = true, message = result.Message });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error bulk rejecting applications for job {JobId}", jobId);
                    return Json(new { success = false, message = "An error occurred during bulk rejection" });
                }
            }

            // POST: RecruiterApplication/AcceptApplication
            /// <summary>
            /// Accept application and optionally schedule interview
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AcceptApplication(int id, DateTime? interviewDate, string? interviewNotes)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.AcceptApplicationAsync(id, interviewDate, interviewNotes);

                    if (result.Success)
                    {
                        _logger.LogInformation("Application {ApplicationId} accepted by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = result.Message });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while accepting the application" });
                }
            }

            // POST: RecruiterApplication/ScheduleInterview
            /// <summary>
            /// Schedule or reschedule interview for an accepted application
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> ScheduleInterview(int id, DateTime interviewDate, string? interviewNotes)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.ScheduleInterviewAsync(id, interviewDate, interviewNotes);

                    if (result.Success)
                    {
                        _logger.LogInformation("Interview scheduled for application {ApplicationId} by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = "Interview scheduled successfully" });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scheduling interview for application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while scheduling the interview" });
                }
            }

            // POST: RecruiterApplication/Shortlist
            /// <summary>
            /// Move application to shortlisted status
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Shortlist(int id)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.UpdateApplicationStatusAsync(id, ApplicationStatus.Shortlisted);

                    if (result.Success)
                    {
                        _logger.LogInformation("Application {ApplicationId} shortlisted by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = "Application shortlisted successfully" });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error shortlisting application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while shortlisting the application" });
                }
            }

            // DELETE: RecruiterApplication/DeleteApplication/5
            /// <summary>
            /// Permanently delete an application (use with caution)
            /// </summary>
            [HttpDelete]
            public async Task<IActionResult> DeleteApplication(int id)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.DeleteApplicationAsync(id);

                    if (result.Success)
                    {
                        _logger.LogWarning("Application {ApplicationId} deleted by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = "Application deleted successfully" });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while deleting the application" });
                }
            }

            // POST: RecruiterApplication/AddReviewNotes
            /// <summary>
            /// Add or update review notes for an application
            /// </summary>
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> AddReviewNotes(int id, string reviewNotes)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify authorization
                    var application = await _applicationService.GetApplicationByIdAsync(id);
                    if (application == null)
                    {
                        return Json(new { success = false, message = "Application not found" });
                    }

                    var job = await _jobPostingService.GetJobPostingByIdAsync(application.JobPostingId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var result = await _applicationService.UpdateReviewNotesAsync(id, reviewNotes);

                    if (result.Success)
                    {
                        _logger.LogInformation("Review notes added to application {ApplicationId} by recruiter {RecruiterId}",
                            id, recruiterId);
                        return Json(new { success = true, message = "Review notes saved successfully" });
                    }

                    return Json(new { success = false, message = string.Join(", ", result.Errors) });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding review notes to application {ApplicationId}", id);
                    return Json(new { success = false, message = "An error occurred while saving review notes" });
                }
            }

            // GET: RecruiterApplication/ApplicationStatistics/5
            /// <summary>
            /// Get application statistics for a job posting
            /// </summary>
            [HttpGet]
            public async Task<IActionResult> ApplicationStatistics(int jobId)
            {
                try
                {
                    var recruiterId = await GetCurrentRecruiterIdAsync();
                    if (recruiterId == 0)
                    {
                        return Json(new { success = false, message = "Recruiter profile not found" });
                    }

                    // Verify job belongs to recruiter
                    var job = await _jobPostingService.GetJobPostingByIdAsync(jobId);
                    if (job == null || job.RecruiterId != recruiterId)
                    {
                        return Json(new { success = false, message = "Unauthorized" });
                    }

                    var statistics = await _applicationService.GetJobApplicationStatisticsAsync(jobId);

                    return Json(new { success = true, data = statistics });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting statistics for job {JobId}", jobId);
                    return Json(new { success = false, message = "An error occurred while loading statistics" });
                }
            }

            // Helper methods
            private int GetCurrentUserId()
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(userIdClaim, out int userId) ? userId : 0;
            }

            private async Task<int> GetCurrentRecruiterIdAsync()
            {
                var userId = GetCurrentUserId();
                var user = await _authenticationService.GetUserProfileAsync(userId);
                return user?.RecruiterProfile?.Id ?? 0;
            }
        }
    }

