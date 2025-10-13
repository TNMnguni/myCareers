using Microsoft.AspNetCore.Mvc;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.Interfaces;

namespace myCareers.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authenticationService.RegisterAsync(model);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    user = result.User
                });
            }

            return BadRequest(new { success = false, errors = result.Errors });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authenticationService.LoginAsync(model);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    token = result.Token,
                    refreshToken = result.RefreshToken,
                    user = result.User
                });
            }

            return Unauthorized(new { success = false, errors = result.Errors });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
        {
            var result = await _authenticationService.RefreshTokenAsync(model.Token, model.RefreshToken);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    token = result.Token,
                    refreshToken = result.RefreshToken
                });
            }

            return Unauthorized(new { success = false, errors = result.Errors });
        }
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
