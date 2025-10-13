using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Core.Enums;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ILogger<DashboardController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            ViewBag.UserName = userName;
            ViewBag.UserRole = userRole;

            switch (userRole)
            {
                case nameof(UserRole.Administrator):
                    return View("AdminDashboard");
                case nameof(UserRole.Recruiter):
                    return View("RecruiterDashboard");
                case nameof(UserRole.Applicant):
                    return View("ApplicantDashboard");
                default:
                    return View();
            }
        }
    }
}
