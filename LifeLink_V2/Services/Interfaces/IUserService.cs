using LifeLink_V2.DTOs.User;
using LifeLink_V2.Helpers;

namespace LifeLink_V2.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApiResponse> GetUserProfileAsync(int userId);
        Task<ApiResponse> UpdateUserProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<ApiResponse> GetUserByIdAsync(int userId);
        Task<ApiResponse> GetUsersByRoleAsync(string roleName, int page = 1, int pageSize = 20);
        Task<ApiResponse> UpdateUserStatusAsync(int userId, bool isActive);
    }
}