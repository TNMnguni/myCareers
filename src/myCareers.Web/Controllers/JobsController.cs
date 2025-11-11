using myCareers.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    public class JobsController : Controller
    {
        private readonly IJobPostingService _jobPostingService;
        private readonly IJobApplicationService _jobApplicationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            IJobPostingService jobPostingService,
            IJobApplicationService jobApplicationService,
            IAuthenticationService authenticationService,
            ILogger<JobsController> logger)
        {
            _jobPostingService = jobPostingService;
            _jobApplicationService = jobApplicationService;
            _authenticationService = authenticationService;
            _logger = logger;
        }

        // GET: /Jobs/Browse
        [HttpGet]
        public async Task<IActionResult> Browse(string search = "")
        {
            var jobs = string.IsNullOrWhiteSpace(search)
                ? await _jobPostingService.GetActiveJobPostingsAsync()
                : await _jobPostingService.SearchJobPostingsAsync(search);

            ViewBag.SearchTerm = search;
            ViewBag.TotalJobs = jobs.Count;

            return View(jobs);
        }

        // GET: /Jobs/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var job = await _jobPostingService.GetJobPostingByIdAsync(id);

            if (job == null || !job.IsActive)
            {
                TempData["ErrorMessage"] = "Job posting not found or no longer available.";
                return RedirectToAction(nameof(Browse));
            }

            // Check if current user is an applicant and has already applied
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Applicant"))
            {
                var userId = GetCurrentUserId();
                var user = await _authenticationService.GetUserProfileAsync(userId);
                var applicantId = user?.ApplicantProfile?.Id ?? 0;

                if (applicantId > 0)
                {
                    var hasApplied = await _jobApplicationService.HasAppliedToJobAsync(applicantId, id);
                    ViewBag.HasApplied = hasApplied;
                }
            }

            // Check if current user is the recruiter who posted this job
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Recruiter"))
            {
                var userId = GetCurrentUserId();
                ViewBag.IsOwner = job.RecruiterId == userId;
            }

            return View(job);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}