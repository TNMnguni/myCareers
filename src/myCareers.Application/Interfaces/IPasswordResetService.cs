using myCareers.Application.DTOs.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCareers.Application.Interfaces
{
    public interface IPasswordResetService
    {
        Task<(bool Success, string Message)> SendPasswordResetEmailAsync(ForgotPasswordDto dto);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordDto dto);
        Task<bool> ValidateResetTokenAsync(string token, string email);
    }
}
