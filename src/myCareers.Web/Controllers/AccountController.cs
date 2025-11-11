using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs.Profile;
using myCareers.Application.Interfaces;
using System.Security.Claims;

namespace myCareers.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AccountController> _logger;
        private readonly IPasswordResetService _passwordResetService;

        public AccountController(
            IAuthenticationService authenticationService,
            IPasswordResetService passwordResetService,
            ILogger<AccountController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
            _passwordResetService = passwordResetService;
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _passwordResetService.SendPasswordResetEmailAsync(model);
            TempData["InfoMessage"] = result.Message;
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Invalid password reset link.";
                return RedirectToAction(nameof(Login));
            }

            var isValid = await _passwordResetService.ValidateResetTokenAsync(token, email);
            if (!isValid)
            {
                TempData["ErrorMessage"] = "This password reset link is invalid or has expired.";
                return RedirectToAction(nameof(Login));
            }

            var model = new ResetPasswordDto
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _passwordResetService.ResetPasswordAsync(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Login));
            }

            TempData["ErrorMessage"] = result.Message;
            return View(model);
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
            HttpContext.Session.Clear(); // Clear session
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
                SetAuthenticationCookie(result.Token, false); // Don't persist by default

                // Set session for tracking
                HttpContext.Session.SetString("UserId", result.User?.Id.ToString() ?? "0");
                HttpContext.Session.SetString("UserEmail", result.User?.Email ?? "");
                HttpContext.Session.SetString("RememberMe", "false");

                _logger.LogInformation("Auth cookie set for new user: {Email}", model.Email);
                TempData["InfoMessage"] = $"Welcome {result.User?.FirstName}! Your account has been created successfully.";

                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogWarning("Registration failed for user: {Email}. Errors: {Errors}",
                model.Email, string.Join(", ", result.Errors));

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

                // Set authentication cookie with RememberMe setting
                SetAuthenticationCookie(result.Token, model.RememberMe);

                // Set session data
                HttpContext.Session.SetString("UserId", result.User?.Id.ToString() ?? "0");
                HttpContext.Session.SetString("UserEmail", result.User?.Email ?? "");
                HttpContext.Session.SetString("RememberMe", model.RememberMe.ToString());
                HttpContext.Session.SetString("UserName", $"{result.User?.FirstName} {result.User?.LastName}");

                _logger.LogInformation("Auth cookie and session set for user: {Email} with RememberMe: {RememberMe}",
                    model.Email, model.RememberMe);

                TempData["SuccessMessage"] = $"Welcome back, {result.User?.FirstName}!";

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Dashboard");
            }

            _logger.LogWarning("Login failed for user: {Email}. Errors: {Errors}",
                model.Email, string.Join(", ", result.Errors));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var token = Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                await _authenticationService.LogoutAsync(token);
            }

            // Clear cookie and session
            Response.Cookies.Delete("AuthToken");
            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }

        // Add GET version for auto-logout from JavaScript
        [HttpGet]
        public async Task<IActionResult> AutoLogout()
        {
            var token = Request.Cookies["AuthToken"];
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    await _authenticationService.LogoutAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during auto-logout");
                }
            }

            Response.Cookies.Delete("AuthToken");
            HttpContext.Session.Clear();

            TempData["InfoMessage"] = "Your session has expired. Please login again.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = GetCurrentUserId();
            var user = await _authenticationService.GetUserProfileAsync(userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User profile not found";
                return RedirectToAction("Index", "Dashboard");
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Department = user.RecruiterProfile?.Department,
                JobTitle = user.RecruiterProfile?.JobTitle,
                EmployeeId = user.RecruiterProfile?.EmployeeId
            };

            ViewBag.Email = user.Email;
            ViewBag.Role = user.Role.ToString();
            ViewBag.UserName = $"{user.FirstName} {user.LastName}";

            return View(updateDto);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                var user = await _authenticationService.GetUserProfileAsync(userId);
                ViewBag.Email = user?.Email;
                ViewBag.Role = user?.Role.ToString();
                ViewBag.UserName = $"{user?.FirstName} {user?.LastName}";
                return View(model);
            }

            var currentUserId = GetCurrentUserId();
            var result = await _authenticationService.UpdateProfileAsync(currentUserId, model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var currentUser = await _authenticationService.GetUserProfileAsync(currentUserId);
            ViewBag.Email = currentUser?.Email;
            ViewBag.Role = currentUser?.Role.ToString();
            ViewBag.UserName = $"{currentUser?.FirstName} {currentUser?.LastName}";

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            var result = await _authenticationService.ChangePasswordAsync(userId, model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private void SetAuthenticationCookie(string token, bool rememberMe)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // Set to true in production with HTTPS
                SameSite = SameSiteMode.Lax,
                // Set expiry based on RememberMe
                Expires = rememberMe
                    ? DateTime.UtcNow.AddDays(1)
                    : DateTime.UtcNow.AddMinutes(2),
                Path = "/"
            };

            Response.Cookies.Append("AuthToken", token, cookieOptions);
            _logger.LogInformation("Cookie set: AuthToken with expiry: {Expiry}, RememberMe: {RememberMe}",
                cookieOptions.Expires, rememberMe);
        }
    }
}