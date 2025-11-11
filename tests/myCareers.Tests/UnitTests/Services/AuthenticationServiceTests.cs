using Xunit;
using Moq;
using FluentAssertions;
using myCareers.Application.Services;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.DTOs;
using myCareers.Application.Interfaces;
using myCareers.Core.Interfaces;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace myCareers.Tests.UnitTests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IJwtTokenService> _mockJwtTokenService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
        private readonly AuthenticationService _authService;

        public AuthenticationServiceTests()
        {
            _mockUserRepository = new Mock<IUserRepository>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AuthenticationService>>();

            _authService = new AuthenticationService(
                _mockUserRepository.Object,
                _mockJwtTokenService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region Registration Tests

        [Fact]
        public async Task RegisterAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@dirco.gov.za",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Applicant,
                PhoneNumber = "0123456789"
            };

            var createdUser = new User
            {
                Id = 1,
                Email = registerDto.Email.ToLower(),
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Role = registerDto.Role
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                Role = createdUser.Role
            };

            _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("test-access-token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("test-refresh-token");

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(userDto);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be("test-access-token");
            result.RefreshToken.Should().Be("test-refresh-token");
            result.User.Should().NotBeNull();
            result.User.Email.Should().Be("newuser@dirco.gov.za");
            result.Errors.Should().BeEmpty();

            _mockUserRepository.Verify(x => x.EmailExistsAsync(registerDto.Email), Times.Once);
            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailure()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@dirco.gov.za",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Applicant
            };

            _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("User with this email already exists");
            result.Token.Should().BeEmpty();

            _mockUserRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithApplicantRole_ShouldCreateApplicantProfile()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "applicant@dirco.gov.za",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "Test",
                LastName = "Applicant",
                Role = UserRole.Applicant
            };

            User capturedUser = null;

            _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .ReturnsAsync((User user) => user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh");

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(new UserDto());

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Success.Should().BeTrue();
            capturedUser.Should().NotBeNull();
            capturedUser.Applicant.Should().NotBeNull();
            capturedUser.Recruiter.Should().BeNull();
        }

        [Fact]
        public async Task RegisterAsync_WithRecruiterRole_ShouldCreateRecruiterProfile()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "recruiter@dirco.gov.za",
                Password = "Test@123",
                ConfirmPassword = "Test@123",
                FirstName = "Test",
                LastName = "Recruiter",
                Role = UserRole.Recruiter
            };

            User capturedUser = null;

            _mockUserRepository.Setup(x => x.EmailExistsAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockUserRepository.Setup(x => x.CreateAsync(It.IsAny<User>()))
                .Callback<User>(user => capturedUser = user)
                .ReturnsAsync((User user) => user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh");

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(new UserDto());

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            result.Success.Should().BeTrue();
            capturedUser.Should().NotBeNull();
            capturedUser.Recruiter.Should().NotBeNull();
            capturedUser.Applicant.Should().BeNull();
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccess()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@dirco.gov.za",
                Password = "Test@123"
            };

            var user = new User
            {
                Id = 1,
                Email = "user@dirco.gov.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                Role = UserRole.Applicant
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("test-token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh-token");

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(userDto);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
            result.User.Should().NotBeNull();
            result.Errors.Should().BeEmpty();

            _mockUserRepository.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidEmail_ShouldReturnFailure()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@dirco.gov.za",
                Password = "Test@123"
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Invalid email or password");
            result.Token.Should().BeEmpty();
        }

        [Fact]
        public async Task LoginAsync_WithInvalidPassword_ShouldReturnFailure()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@dirco.gov.za",
                Password = "WrongPassword@123"
            };

            var user = new User
            {
                Id = 1,
                Email = "user@dirco.gov.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                IsActive = true
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Invalid email or password");
        }

        [Fact]
        public async Task LoginAsync_WithInactiveUser_ShouldReturnFailure()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "inactive@dirco.gov.za",
                Password = "Test@123"
            };

            var user = new User
            {
                Id = 1,
                Email = "inactive@dirco.gov.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                IsActive = false
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Errors.Should().Contain("Account is deactivated");
        }

        [Fact]
        public async Task LoginAsync_ShouldUpdateLastLoginDate()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user@dirco.gov.za",
                Password = "Test@123"
            };

            var user = new User
            {
                Id = 1,
                Email = "user@dirco.gov.za",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                IsActive = true,
                LastLoginDate = null
            };

            _mockUserRepository.Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _mockUserRepository.Setup(x => x.UpdateAsync(It.IsAny<User>()))
                .ReturnsAsync(user);

            _mockJwtTokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>()))
                .Returns("token");

            _mockJwtTokenService.Setup(x => x.GenerateRefreshToken())
                .Returns("refresh");

            _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
                .Returns(new UserDto());

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            result.Success.Should().BeTrue();
            user.LastLoginDate.Should().NotBeNull();
            user.LastLoginDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        #endregion
    }
}

