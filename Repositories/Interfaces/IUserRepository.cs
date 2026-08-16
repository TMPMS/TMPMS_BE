using BusinessObjects;
using TMPMS.DTOs;

namespace TMPMS.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> IsUsernameExistAsync(string username);
        Task<List<UserListDto>> GetAllUsersAsync();
        Task<UserDetailDto?> GetUserByIdAsync(int id);
        Task<bool> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ForceDeleteUserAsync(int id);
        Task<bool> AssignRoleAsync(AssignRoleDto dto);
        Task<bool> LockUserAsync(int id);

        Task<bool> UnlockUserAsync(int id);
        Task<ProfileDto?> GetProfileAsync(int userId);

        Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto);
    }
    }
