using myCareers.Application.DTOs;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs.Profile;


namespace myCareers.Application.Interfaces
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> RegisterAsync(RegisterDto registerDto);
        Task<AuthenticationResult> LoginAsync(LoginDto loginDto);
        Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken);
        Task LogoutAsync(string token);
        Task<bool> ValidateTokenAsync(string token);

        // Profile Management
        Task<UserDto?> GetUserProfileAsync(int userId);
        Task<ProfileResult> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<ProfileResult> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    }
}
