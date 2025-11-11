using AutoMapper;
using Microsoft.Extensions.Logging;
using myCareers.Application.DTOs;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs.Profile;
using myCareers.Application.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;


namespace myCareers.Application.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IUserRepository userRepository,
            IJwtTokenService jwtTokenService,
            IMapper mapper,
            ILogger<AuthenticationService> logger)
        {
            _userRepository = userRepository;
            _jwtTokenService = jwtTokenService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AuthenticationResult> RegisterAsync(RegisterDto registerDto)
        {
            try
            {
                //  Check if email already exists
                if (await _userRepository.EmailExistsAsync(registerDto.Email))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "A user with this email already exists." }
                    };
                }

                //  Create new user entity
                var user = new User
                {
                    Email = registerDto.Email.Trim().ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    FirstName = registerDto.FirstName.Trim(),
                    LastName = registerDto.LastName.Trim(),
                    PhoneNumber = registerDto.PhoneNumber,
                    Role = registerDto.Role,
                    CreatedDate = DateTime.UtcNow
                };

                // Attach role-specific entity
                switch (registerDto.Role)
                {
                    case UserRole.Recruiter:
                        user.Recruiter = new Recruiter
                        {
                            User = user,
                            Department = "Human Resources", // Optional default value
                            JobTitle = "HR Manager"
                        };
                        break;

                    case UserRole.Applicant:
                        user.Applicant = new Applicant
                        {
                            User = user
                        };
                        break;
                }

                // Persist the new user and related entity
                var createdUser = await _userRepository.CreateAsync(user);

                // Generate tokens
                var token = _jwtTokenService.GenerateAccessToken(createdUser);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                _logger.LogInformation("✅ User registered successfully: {Email}", registerDto.Email);

                // Return successful result
                return new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = _mapper.Map<UserDto>(createdUser)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during registration for {Email}", registerDto.Email);
                return new AuthenticationResult
                {
                    Success = false,
                    Errors = new List<string> { "An unexpected error occurred during registration. Please try again." }
                };
            }
        }

        public async Task<AuthenticationResult> LoginAsync(LoginDto loginDto)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(loginDto.Email.ToLower());

                if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Invalid email or password" }
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "Account is deactivated" }
                    };
                }

                // Update last login date
                user.LastLoginDate = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                var token = _jwtTokenService.GenerateAccessToken(user);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

                return new AuthenticationResult
                {
                    Success = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = _mapper.Map<UserDto>(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
                return new AuthenticationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred during login" }
                };
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string token, string refreshToken)
        {
            // Implementation for refresh token logic
            throw new NotImplementedException("Refresh token logic will be implemented in next iteration");
        }

        public async Task LogoutAsync(string token)
        {
            // Implementation for token blacklisting if needed
            _logger.LogInformation("User logged out");
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            return _jwtTokenService.ValidateToken(token);
        }

        public async Task<UserDto?> GetUserProfileAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) return null;

                var userDto = _mapper.Map<UserDto>(user);
                return userDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user {UserId}", userId);
                return null;
            }
        }

        public async Task<ProfileResult> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ProfileResult
                    {
                        Success = false,
                        Errors = new List<string> { "User not found" }
                    };
                }

                // Update basic user info
                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.PhoneNumber = dto.PhoneNumber;
                user.UpdatedDate = DateTime.UtcNow;

                // Update recruiter-specific info if user is a recruiter
                if (user.Role == UserRole.Recruiter && user.Recruiter != null)
                {
                    user.Recruiter.Department = dto.Department;
                    user.Recruiter.JobTitle = dto.JobTitle;
                    user.Recruiter.EmployeeId = dto.EmployeeId;
                }

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

                return new ProfileResult
                {
                    Success = true,
                    Message = "Profile updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return new ProfileResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while updating your profile" }
                };
            }
        }

        public async Task<ProfileResult> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new ProfileResult
                    {
                        Success = false,
                        Errors = new List<string> { "User not found" }
                    };
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                {
                    return new ProfileResult
                    {
                        Success = false,
                        Errors = new List<string> { "Current password is incorrect" }
                    };
                }

                // Hash new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                user.UpdatedDate = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password changed successfully for user {UserId}", userId);

                return new ProfileResult
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new ProfileResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred while changing your password" }
                };
            }
        }
    }
}
