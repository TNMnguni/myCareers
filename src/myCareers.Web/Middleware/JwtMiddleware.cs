using myCareers.Application.Interfaces;
using myCareers.Core.Interfaces;
using System.Security.Claims;

namespace myCareers.Web.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtTokenService jwtTokenService, IUserRepository userRepository)
        {
            var token = context.Request.Cookies["AuthToken"] ??
                       context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token) && jwtTokenService.ValidateToken(token))
            {
                try
                {
                    var userId = jwtTokenService.GetUserIdFromToken(token);
                    var user = await userRepository.GetByIdAsync(userId);

                    if (user != null && user.IsActive)
                    {
                        var claims = new List<Claim>
                        {
                            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                            new(ClaimTypes.Email, user.Email),
                            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                            new(ClaimTypes.Role, user.Role.ToString()),
                            new("firstName", user.FirstName),
                            new("lastName", user.LastName)
                        };

                        var identity = new ClaimsIdentity(claims, "jwt");
                        context.User = new ClaimsPrincipal(identity);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing JWT token");
                }
            }

            await _next(context);
        }
    }
}
