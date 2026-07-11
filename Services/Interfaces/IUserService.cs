using TMPMS.DTOs;
using TMPMS.DTOs.TMPMS_BE.DTOs;

namespace TMPMS.Services.Interfaces
{
    public interface IUserService
    {
        Task<bool> CreateUserAsync(CreateUserDto dto);
        Task<List<UserListDto>> GetAllUsersAsync();
        Task<UserDetailDto?> GetUserByIdAsync(int id);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> AssignRoleAsync(AssignRoleDto dto);
        Task<bool> LockUserAsync(int id);

        Task<bool> UnlockUserAsync(int id);
        Task<ProfileDto?> GetProfileAsync(int userId);

        Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto);
    }
}
