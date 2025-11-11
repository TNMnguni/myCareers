using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Application.Interfaces;
using System.Security.Claims;
using myCareers.Core.Enums;
using myCareers.Application.DTOs;

namespace myCareers.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IJobPostingService _jobPostingService;
        private readonly IJobApplicationService _applicationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IJobPostingService jobPostingService,
            IJobApplicationService applicationService,
            IAuthenticationService authenticationService,
            ILogger<DashboardController> logger)
        {
            _jobPostingService = jobPostingService;
            _applicationService = applicationService;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        // GET: Dashboard
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User profile not found for user ID: {UserId}", userId);
                return RedirectToAction("Login", "Account");
            }

            ViewBag.UserName = $"{user.FirstName} {user.LastName}";

            // Route based on role
            if (user.Role == Core.Enums.UserRole.Recruiter)
            {
                return await RecruiterDashboard(user);
            }
            else if (user.Role == Core.Enums.UserRole.Applicant)
            {
                return await ApplicantDashboard(user);
            }
            else if (user.Role == Core.Enums.UserRole.Administrator)
            {
                return View("AdminDashboard");
            }

            return View();
        }

        // GET: Dashboard/RecruiterDashboard
        [Authorize(Roles = "Recruiter")]
        public async Task<IActionResult> RecruiterDashboard()
        {
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);

            if (user?.RecruiterProfile == null)
            {
                TempData["ErrorMessage"] = "Recruiter profile not found";
                return RedirectToAction("Index", "Home");
            }

            return await RecruiterDashboard(user);
        }

        // GET: Dashboard/ApplicantDashboard
        [Authorize(Roles = "Applicant")]
        public async Task<IActionResult> ApplicantDashboard()
        {
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);

            if (user?.ApplicantProfile == null)
            {
                TempData["ErrorMessage"] = "Applicant profile not found";
                return RedirectToAction("Index", "Home");
            }

            return await ApplicantDashboard(user);
        }

        // Private helper for Recruiter Dashboard
        private async Task<IActionResult> RecruiterDashboard(UserDto user)
        {
            try
            {
                var recruiterId = user.RecruiterProfile.Id;

                // Get job statistics
                var allJobs = await _jobPostingService.GetJobPostingsByUserAsync(user.Id);
                var activeJobs = allJobs.Count(j => j.IsActive && !j.IsClosed);
                var draftJobs = allJobs.Count(j => !j.IsActive);

                // Get all applications across all jobs for this recruiter
                var totalApplications = 0;
                var submittedCount = 0;
                var underReviewCount = 0;
                var shortlistedCount = 0;
                var interviewScheduledCount = 0;
                var acceptedCount = 0;
                var rejectedCount = 0;
                var recentApplicationsCount = 0;
                var mostRecentJobId = 0;

                foreach (var job in allJobs)
                {
                    var applications = await _applicationService.GetApplicationsByJobPostingAsync(job.Id);
                    totalApplications += applications.Count;

                    submittedCount += applications.Count(a => a.Status == ApplicationStatus.Submitted);
                    underReviewCount += applications.Count(a => a.Status == ApplicationStatus.UnderReview);
                    shortlistedCount += applications.Count(a => a.Status == ApplicationStatus.Shortlisted);
                    interviewScheduledCount += applications.Count(a => a.Status == ApplicationStatus.InterviewScheduled);
                    acceptedCount += applications.Count(a => a.Status == ApplicationStatus.Accepted);
                    rejectedCount += applications.Count(a => a.Status == ApplicationStatus.Rejected);

                    // Count recent applications (last 7 days)
                    var recentForJob = applications.Count(a =>
                        a.Status == ApplicationStatus.Submitted &&
                        a.AppliedDate >= DateTime.UtcNow.AddDays(-7));

                    if (recentForJob > 0 && mostRecentJobId == 0)
                    {
                        mostRecentJobId = job.Id;
                    }

                    recentApplicationsCount += recentForJob;

                    // Add application count to job object for display
                    job.ApplicationCount = applications.Count;
                }

                // Get recent jobs (last 5)
                var recentJobs = allJobs
                    .OrderByDescending(j => j.CreatedDate)
                    .Take(5)
                    .ToList();

                // Populate ViewBag
                ViewBag.UserName = $"{user.FirstName} {user.LastName}";
                ViewBag.TotalJobs = allJobs.Count;
                ViewBag.ActiveJobs = activeJobs;
                ViewBag.DraftJobs = draftJobs;
                ViewBag.TotalApplications = totalApplications;
                ViewBag.RecentJobs = recentJobs;

                // Sprint 4: Application statistics
                ViewBag.SubmittedCount = submittedCount;
                ViewBag.UnderReviewCount = underReviewCount;
                ViewBag.ShortlistedCount = shortlistedCount;
                ViewBag.InterviewScheduledCount = interviewScheduledCount;
                ViewBag.AcceptedCount = acceptedCount;
                ViewBag.RejectedCount = rejectedCount;
                ViewBag.RecentApplicationsCount = recentApplicationsCount;
                ViewBag.MostRecentJobId = mostRecentJobId;

                return View("RecruiterDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recruiter dashboard for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "An error occurred while loading your dashboard";
                return View("RecruiterDashboard");
            }
        }

        // Private helper for Applicant Dashboard
        private async Task<IActionResult> ApplicantDashboard(UserDto user)
        {
            try
            {
                var applicantId = user.ApplicantProfile.Id;

                // Get application statistics
                var statistics = await _applicationService.GetApplicationStatisticsAsync(applicantId);

                // Get recent applications
                var applications = await _applicationService.GetApplicationsByApplicantAsync(applicantId);
                var recentApplications = applications
                    .OrderByDescending(a => a.AppliedDate)
                    .Take(5)
                    .ToList();

                // Populate ViewBag
                ViewBag.UserName = $"{user.FirstName} {user.LastName}";
                ViewBag.TotalApplications = statistics.TotalApplications;
                ViewBag.UnderReviewCount = statistics.UnderReviewCount;
                ViewBag.InterviewsScheduledCount = statistics.InterviewsScheduledCount;
                ViewBag.AvailableJobsCount = statistics.AvailableJobsCount;
                ViewBag.RecentApplications = recentApplications;

                return View("ApplicantDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading applicant dashboard for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "An error occurred while loading your dashboard";
                return View("ApplicantDashboard");
            }
        }

        // Helper method
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
