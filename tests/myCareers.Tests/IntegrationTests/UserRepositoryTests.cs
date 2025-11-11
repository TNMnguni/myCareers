using Xunit;
using FluentAssertions;
using myCareers.Infrastructure.Data;
using myCareers.Infrastructure.Repositories;
using myCareers.Core.Entities;
using myCareers.Core.Enums;
using myCareers.Core.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace myCareers.Tests.IntegrationTests
{
    public class UserRepositoryTests : IDisposable
    {
        private readonly myCareersDbContext _context;
        private readonly UserRepository _repository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<myCareersDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new myCareersDbContext(options);
            _repository = new UserRepository(_context);
        }

        [Fact]
        public async Task CreateAsync_ShouldAddUserToDatabase()
        {
            // Arrange
            var user = new User
            {
                Email = "newuser@dirco.gov.za",
                PasswordHash = "hashedpassword",
                FirstName = "New",
                LastName = "User",
                Role = UserRole.Applicant,
                IsActive = true
            };

            // Act
            var result = await _repository.CreateAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);

            var userInDb = await _context.Users.FindAsync(result.Id);
            userInDb.Should().NotBeNull();
            userInDb.Email.Should().Be("newuser@dirco.gov.za");
        }

        [Fact]
        public async Task GetByEmailAsync_WithExistingEmail_ShouldReturnUser()
        {
            // Arrange
            var user = new User
            {
                Email = "existing@dirco.gov.za",
                PasswordHash = "hash",
                FirstName = "Existing",
                LastName = "User",
                Role = UserRole.Recruiter
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByEmailAsync("existing@dirco.gov.za");

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("existing@dirco.gov.za");
            result.FirstName.Should().Be("Existing");
        }

        [Fact]
        public async Task GetByEmailAsync_WithNonExistingEmail_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByEmailAsync("nonexistent@dirco.gov.za");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task EmailExistsAsync_WithExistingEmail_ShouldReturnTrue()
        {
            // Arrange
            var user = new User
            {
                Email = "test@dirco.gov.za",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Applicant
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var exists = await _repository.EmailExistsAsync("test@dirco.gov.za");

            // Assert
            exists.Should().BeTrue();
        }

        [Fact]
        public async Task EmailExistsAsync_WithNonExistingEmail_ShouldReturnFalse()
        {
            // Act
            var exists = await _repository.EmailExistsAsync("nonexistent@dirco.gov.za");

            // Assert
            exists.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyUserInDatabase()
        {
            // Arrange
            var user = new User
            {
                Email = "update@dirco.gov.za",
                PasswordHash = "hash",
                FirstName = "Original",
                LastName = "Name",
                Role = UserRole.Applicant
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            user.FirstName = "Updated";
            user.LastName = "NewName";
            await _repository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            updatedUser.FirstName.Should().Be("Updated");
            updatedUser.LastName.Should().Be("NewName");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldIncludeProfiles()
        {
            // Arrange
            var user = new User
            {
                Email = "withprofile@dirco.gov.za",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Applicant
            };

            user.Applicant = new Applicant
            {
                User = user,
                IdNumber = "1234567890"
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();

            // Act
            var result = await _repository.GetByIdAsync(user.Id);

            // Assert
            result.Should().NotBeNull();
            result.Applicant.Should().NotBeNull();
            result.Applicant.IdNumber.Should().Be("1234567890");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}