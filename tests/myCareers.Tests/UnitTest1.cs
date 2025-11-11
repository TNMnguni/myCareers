using Xunit;
using Moq;
using FluentAssertions;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs;
using myCareers.Application.Services;
using AutoMapper;
using Microsoft.Extensions.Logging;
using myCareers.Application.Interfaces;


namespace myCareers.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IJwtTokenService> _jwtServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _jwtServiceMock = new Mock<IJwtTokenService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<AuthenticationService>>();

            _authService = new AuthenticationService(
                _userRepoMock.Object,
                _jwtServiceMock.Object,
                _mapperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task LoginAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var user = new User
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                IsActive = true,
                Role = UserRole.Applicant
            };

            _userRepoMock.Setup(r => r.GetByEmailAsync(loginDto.Email.ToLower()))
                         .ReturnsAsync(user);

            _jwtServiceMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>()))
                           .Returns("fake-jwt-token");

            _jwtServiceMock.Setup(j => j.GenerateRefreshToken())
                           .Returns("fake-refresh-token");

            _mapperMock.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
                       .Returns(new UserDto { Email = user.Email });

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be("fake-jwt-token");
            result.RefreshToken.Should().Be("fake-refresh-token");
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("test@example.com");
        }
    }
}
