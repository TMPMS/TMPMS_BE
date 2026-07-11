using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using TMPMS.DTOs;
using TMPMS.DTOs.TMPMS_BE.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public UserService(
            IUserRepository userRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> CreateUserAsync(CreateUserDto dto)
        {
            
            if (await _userRepository.IsUsernameExistAsync(dto.Username))
            {
                throw new Exception("Tên tài khoản này đã được sử dụng.");
            }

            
            var newUser = new User
            {
                UserName = dto.Username,       
                Email = dto.Email,             
                PhoneNumber = dto.PhoneNumber, 
                IsActive = true,               
                CreatedAt = DateTime.Now       
            };

          
            var result = await _userManager.CreateAsync(newUser, dto.Password);

            if (!result.Succeeded)
            {
                
                var error = result.Errors.FirstOrDefault()?.Description ?? "Tạo tài khoản thất bại.";
                throw new Exception(error);
            }

            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
            {
                await _roleManager.CreateAsync(new IdentityRole<int>(dto.RoleName));
            }

            await _userManager.AddToRoleAsync(newUser, dto.RoleName);

            return true;
        }
        public async Task<List<UserListDto>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllUsersAsync();
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(int id)
        {
            return await _userRepository.GetUserByIdAsync(id);
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            return await _userRepository.UpdateUserAsync(id, dto);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            return await _userRepository.DeleteUserAsync(id);
        }

        public async Task<bool> AssignRoleAsync(AssignRoleDto dto)
        {
            return await _userRepository.AssignRoleAsync(dto);
        }

        public async Task<bool> LockUserAsync(int id)
        {
            return await _userRepository.LockUserAsync(id);
        }

        public async Task<bool> UnlockUserAsync(int id)
        {
            return await _userRepository.UnlockUserAsync(id);
        }

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            return await _userRepository.GetProfileAsync(userId);
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            return await _userRepository.UpdateProfileAsync(userId, dto);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            return await _userRepository.ChangePasswordAsync(userId, dto);
        }
    }
}
