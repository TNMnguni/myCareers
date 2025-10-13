using AutoMapper;
using Microsoft.Extensions.Logging;
using myCareers.Application.DTOs;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.Interfaces;
using myCareers.Core.Enterfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                // Check if user already exists
                if (await _userRepository.EmailExistsAsync(registerDto.Email))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Errors = new List<string> { "User with this email already exists" }
                    };
                }

                // Create new user
                var user = new User
                {
                    Email = registerDto.Email.ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    PhoneNumber = registerDto.PhoneNumber,
                    Role = registerDto.Role,
                    CreatedDate = DateTime.UtcNow
                };

                // Create role-specific profile
                if (registerDto.Role == UserRole.Recruiter)
                {
                    user.Recruiter = new Recruiter { User = user };
                }
                else if (registerDto.Role == UserRole.Applicant)
                {
                    user.Applicant = new Applicant{ User = user };
                }

                var createdUser = await _userRepository.CreateAsync(user);

                var token = _jwtTokenService.GenerateAccessToken(createdUser);
                var refreshToken = _jwtTokenService.GenerateRefreshToken();

                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);

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
                _logger.LogError(ex, "Error during user registration for email: {Email}", registerDto.Email);
                return new AuthenticationResult
                {
                    Success = false,
                    Errors = new List<string> { "An error occurred during registration" }
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
    }
}
