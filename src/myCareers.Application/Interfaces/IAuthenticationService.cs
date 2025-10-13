using myCareers.Application.DTOs.Authentication;


namespace myCareers.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> RegisterAsync(RegisterDto registerDto);
        Task<AuthenticationResult> LoginAsync(LoginDto loginDto);
        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken);
        Task LogoutAsync(string token);
        Task<bool> ValidateTokenAsync(string token);
    }
}
