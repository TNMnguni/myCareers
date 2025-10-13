using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.Interfaces;
using myCareers.Core.Enums;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthenticationService authenticationService, ILogger<AccountController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpGet]
        public IActionResult ClearSession()
        {
            // Clear authentication cookie
            Response.Cookies.Delete("AuthToken");
            TempData["SuccessMessage"] = "Session cleared successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration validation failed. ModelState errors: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            var result = await _authenticationService.RegisterAsync(model);

            if (result.Success)
            {
                _logger.LogInformation("Registration successful for user: {Email}", model.Email);

                // Set JWT token in cookie for web application
                SetAuthenticationCookie(result.Token);

                _logger.LogInformation("Auth cookie set for new user: {Email}", model.Email);

                TempData["InfoMessage"] = $"Welcome {result.User?.FirstName}! Your account has been created successfully.";
                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogWarning("Registration failed for user: {Email}. Errors: {Errors}",
                model.Email, string.Join(", ", result.Errors));

            // Add errors to ModelState for inline display
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fix the validation errors.";
                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var result = await _authenticationService.LoginAsync(model);

            if (result.Success)
            {
                _logger.LogInformation("Login successful for user: {Email}", model.Email);
                SetAuthenticationCookie(result.Token);

                // Log cookie setting
                _logger.LogInformation("Auth cookie set for user: {Email}", model.Email);

                TempData["SuccessMessage"] = $"Welcome back, {result.User?.FirstName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogWarning("Login failed for user: {Email}. Errors: {Errors}", model.Email, string.Join(", ", result.Errors));

            // Add errors to ModelState so they show on the form
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                await _authenticationService.LogoutAsync(token);
            }

            Response.Cookies.Delete("AuthToken");
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userRole == UserRole.Recruiter.ToString())
            {
                return RedirectToAction("Profile", "Recruiter");
            }
            else if (userRole == UserRole.Applicant.ToString())
            {
                return RedirectToAction("Profile", "Applicant");
            }

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void SetAuthenticationCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to false for development (HTTP)
                SameSite = SameSiteMode.Lax, // Changed from Strict to Lax
                Expires = DateTime.UtcNow.AddHours(1),
                Path = "/"
            };

            Response.Cookies.Append("AuthToken", token, cookieOptions);
            _logger.LogInformation("Cookie set: AuthToken with expiry: {Expiry}", cookieOptions.Expires);
        }
    }
}
