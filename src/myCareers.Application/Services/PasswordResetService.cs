using Microsoft.Extensions.Logging;
using myCareers.Application.DTOs.Authentication;
using myCareers.Application.Interfaces;
using myCareers.Core.Enterfaces;
using myCareers.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Application.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            IUserRepository userRepository,
            IPasswordResetRepository passwordResetRepository,
            ILogger<PasswordResetService> logger)
        {
            _userRepository = userRepository;
            _passwordResetRepository = passwordResetRepository;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> SendPasswordResetEmailAsync(ForgotPasswordDto dto)
        {
            try
            {
                var user = await _userRepository.GetByEmailAsync(dto.Email);

                if (user == null)
                {
                    // Don't reveal if user exists for security
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
                    return (true, "If an account exists with this email, you will receive a password reset link.");
                }

                if (!user.IsActive)
                {
                    return (false, "This account has been deactivated. Please contact support.");
                }

                // Generate secure reset token
                var token = GenerateSecureToken();

                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    ExpiryDate = DateTime.UtcNow.AddHours(1), // Token valid for 1 hour
                    IsUsed = false
                };

                await _passwordResetRepository.CreateAsync(resetToken);

                // TODO: Send email with reset link
                // For now, we'll log the token (in production, send via email service)
                _logger.LogInformation("Password reset token for {Email}: {Token}", user.Email, token);
                _logger.LogInformation("Reset link: /Account/ResetPassword?token={Token}&email={Email}", token, user.Email);

                return (true, "If an account exists with this email, you will receive a password reset link.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email for: {Email}", dto.Email);
                return (false, "An error occurred. Please try again later.");
            }
        }

        public async Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto)
        {
            try
            {
                var resetToken = await _passwordResetRepository.GetByTokenAsync(dto.Token);

                if (resetToken == null || resetToken.IsUsed)
                {
                    return (false, "Invalid or expired reset token.");
                }

                if (resetToken.ExpiryDate < DateTime.UtcNow)
                {
                    return (false, "This reset token has expired. Please request a new one.");
                }

                var user = await _userRepository.GetByIdAsync(resetToken.UserId);

                if (user == null || user.Email.ToLower() != dto.Email.ToLower())
                {
                    return (false, "Invalid reset request.");
                }

                // Update password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _userRepository.UpdateAsync(user);

                // Mark token as used
                resetToken.IsUsed = true;
                await _passwordResetRepository.UpdateAsync(resetToken);

                _logger.LogInformation("Password reset successfully for user: {Email}", user.Email);

                return (true, "Your password has been reset successfully. You can now login with your new password.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for token: {Token}", dto.Token);
                return (false, "An error occurred. Please try again later.");
            }
        }

        public async Task<bool> ValidateResetTokenAsync(string token, string email)
        {
            var resetToken = await _passwordResetRepository.GetByTokenAsync(token);

            if (resetToken == null || resetToken.IsUsed || resetToken.ExpiryDate < DateTime.UtcNow)
            {
                return false;
            }

            var user = await _userRepository.GetByIdAsync(resetToken.UserId);
            return user != null && user.Email.ToLower() == email.ToLower();
        }

        private string GenerateSecureToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
