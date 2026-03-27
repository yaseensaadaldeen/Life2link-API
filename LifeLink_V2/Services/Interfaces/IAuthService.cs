using LifeLink_V2.DTOs.Auth;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IAuthService
    {
        // Existing methods
        Task<AuthResponseDto> RegisterPatientAsync(RegisterPatientDto registerDto);
        Task<AuthResponseDto> RegisterProviderAsync(RegisterProviderDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> NationalIdExistsAsync(string nationalId);
        Task<bool> IsValidRoleAsync(int roleId);

        // ADD THESE NEW METHODS:
        Task<AuthResponseDto> ForgotPasswordAsync(string email);
        Task<AuthResponseDto> ResetPasswordAsync(string token, string newPassword);
        Task<AuthResponseDto> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}