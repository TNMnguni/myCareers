using myCareers.Application.DTOs.JobPosting;
using myCareers.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    [Authorize(Roles = "Recruiter")]
    public class JobPostingController : Controller
    {
        private readonly IJobPostingService _jobPostingService;
        private readonly ILogger<JobPostingController> _logger;

        public JobPostingController(
            IJobPostingService jobPostingService,
            ILogger<JobPostingController> logger)
        {
            _jobPostingService = jobPostingService;
            _logger = logger;
        }

        // GET: /JobPosting
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var jobs = await _jobPostingService.GetJobPostingsByUserAsync(userId);

            ViewBag.TotalJobs = jobs.Count;
            ViewBag.ActiveJobs = jobs.Count(j => j.IsActive && !j.IsClosed);
            ViewBag.DraftJobs = jobs.Count(j => j.Status == Core.Enums.JobStatus.Draft);
            ViewBag.ClosedJobs = jobs.Count(j => j.IsClosed);

            return View(jobs);
        }

        // GET: /JobPosting/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /JobPosting/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPostingDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            var result = await _jobPostingService.CreateJobPostingAsync(model, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = model.PublishImmediately
                    ? "Job posting created and published successfully!"
                    : "Job posting created as draft. Publish it when ready!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(model);
        }

        // GET: /JobPosting/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = GetCurrentUserId();
            var job = await _jobPostingService.GetJobPostingByIdForUserAsync(id, userId);

            if (job == null)
            {
                TempData["ErrorMessage"] = "Job posting not found or access denied.";
                return RedirectToAction(nameof(Index));
            }

            var updateDto = new UpdateJobPostingDto
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                Department = job.Department,
                Location = job.Location,
                SalaryRange = job.SalaryRange,
                EmploymentType = job.EmploymentType,
                ExperienceLevel = job.ExperienceLevel,
                ClosingDate = job.ClosingDate
            };

            return View(updateDto);
        }

        // POST: /JobPosting/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateJobPostingDto model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetCurrentUserId();
            var result = await _jobPostingService.UpdateJobPostingForUserAsync(model, userId);

            if (result.Success)
            {
                TempData["SuccessMessage"] = "Job posting updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error);

            return View(model);
        }

        // GET: /JobPosting/Details/5
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var job = await _jobPostingService.GetJobPostingByIdAsync(id);

            if (job == null)
            {
                TempData["ErrorMessage"] = "Job posting not found.";
                return RedirectToAction("Browse", "Jobs");
            }

            return View(job);
        }

        // POST: /JobPosting/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _jobPostingService.DeleteJobPostingForUserAsync(id, userId);

            if (result.Success)
                TempData["SuccessMessage"] = "Job posting deleted successfully!";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        // POST: /JobPosting/Publish/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _jobPostingService.PublishJobPostingForUserAsync(id, userId);

            if (result.Success)
                TempData["SuccessMessage"] = "Job posting published successfully!";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        // POST: /JobPosting/Deactivate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _jobPostingService.DeactivateJobPostingForUserAsync(id, userId);

            if (result.Success)
                TempData["InfoMessage"] = "Job posting deactivated.";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        // POST: /JobPosting/Activate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var userId = GetCurrentUserId();
            var result = await _jobPostingService.ActivateJobPostingForUserAsync(id, userId);

            if (result.Success)
                TempData["SuccessMessage"] = "Job posting activated!";
            else
                TempData["ErrorMessage"] = string.Join(", ", result.Errors);

            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }
    }
}
