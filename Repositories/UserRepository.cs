using BusinessObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TMPMS.Data;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly TMPMSDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;

        public UserRepository(TMPMSDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> IsUsernameExistAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<List<UserListDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            var result = new List<UserListDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserListDto
                {
                    Id = user.Id,
                    Username = user.UserName,
                    Email = user.Email,
                    Phone = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    RoleName = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return result;
        }

        public async Task<UserDetailDto?> GetUserByIdAsync(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserDetailDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                RoleName = roles.FirstOrDefault() ?? ""
            };
        }

        public async Task<bool> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return false;

            // Kiểm tra Email đã tồn tại chưa
            var emailExists = await _userManager.Users
                .AnyAsync(x => x.Email == dto.Email && x.Id != id);

            if (emailExists)
                throw new Exception("Email already exists.");

            // Kiểm tra Username đã tồn tại chưa
            var usernameExists = await _userManager.Users
                .AnyAsync(x => x.UserName == dto.Username && x.Id != id);

            if (usernameExists)
                throw new Exception("Username already exists.");

            user.UserName = dto.Username;
            user.Email = dto.Email;
            user.PhoneNumber = dto.Phone;
            user.IsActive = dto.IsActive;

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return false;

            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("Không thể xóa tài khoản này vì đang có dữ liệu liên quan (đơn thuốc, hồ sơ...). Hãy khóa tài khoản thay vì xóa.");
            }

            return true;
        }

        // Không xóa cứng được vì có đơn thuốc/hồ sơ liên quan (ràng buộc Restrict để giữ hồ sơ y tế) —
        // ẩn danh hóa thông tin cá nhân + khóa vĩnh viễn thay vì xóa, để không phá vỡ dữ liệu y tế đã lưu.
        public async Task<bool> ForceDeleteUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return false;

            await _userManager.SetUserNameAsync(user, $"deleted_user_{id}");
            await _userManager.SetEmailAsync(user, $"deleted_{id}@deleted.local");
            user.PhoneNumber = null;
            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<bool> AssignRoleAsync(AssignRoleDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId.ToString());

            if (user == null)
                return false;

            // Kiểm tra role có tồn tại
            if (!await _roleManager.RoleExistsAsync(dto.RoleName))
                throw new Exception("Role does not exist.");

            // Lấy role hiện tại
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Xóa role cũ (nếu có)
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    return false;
            }

            // Gán role mới
            var addResult = await _userManager.AddToRoleAsync(user, dto.RoleName);

            return addResult.Succeeded;
        }

        public async Task<bool> LockUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return false;

            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<bool> UnlockUserAsync(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
                return false;

            user.LockoutEnd = null;
            user.LockoutEnabled = false;
            user.IsActive = true;

            var result = await _userManager.UpdateAsync(user);

            return result.Succeeded;
        }

        public async Task<ProfileDto?> GetProfileAsync(int userId)
        {
            var user = await _context.Users
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return null;

            return new ProfileDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                Phone = user.PhoneNumber,
                IsActive = user.IsActive,
                Address = user.Addresses.FirstOrDefault()?.AddressLine ?? user.Address ?? "",
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender
            };
        }
        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _context.Users
                .Include(x => x.Addresses)
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return false;

            user.UserName = dto.Username;
            user.Email = dto.Email;
            user.PhoneNumber = dto.Phone;
            user.FullName = dto.FullName;
            user.AvatarUrl = dto.AvatarUrl;
            user.DateOfBirth = dto.DateOfBirth;
            user.Gender = dto.Gender;
            user.Address = dto.Address;

            var address = user.Addresses.FirstOrDefault();

            if (address != null)
            {
                address.AddressLine = dto.Address;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDTO dto)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                return false;

            var result = await _userManager.ChangePasswordAsync(
                user,
                dto.OldPassword,
                dto.NewPassword);

            return result.Succeeded;
        }


    }
}
