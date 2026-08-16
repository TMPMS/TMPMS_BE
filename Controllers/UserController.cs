using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPMS.DTOs;
using TMPMS.DTOs.TMPMS_BE.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;

        public UserController(IUserService userService, IAuditLogService auditLogService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
        }

        // Endpoint: POST https://localhost:xxxx/api/users/create
        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _userService.CreateUserAsync(dto);
                if (result)
                {
                    await this.LogAuditAsync(_auditLogService, "User", "Create", dto.Username, $"Tạo tài khoản '{dto.Username}' với vai trò {dto.RoleName}");
                    return Ok(new { message = "Tạo tài khoản thành công!" });
                }
                return BadRequest(new { message = "Không thể tạo tài khoản." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("list")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetUserList()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("detail/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUserDetail(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);

                if (user == null)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("update/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.UpdateUserAsync(id, dto);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                await this.LogAuditAsync(_auditLogService, "User", "Update", id.ToString(), $"Cập nhật thông tin user #{id}");

                return Ok(new
                {
                    message = "User updated successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/users/delete/1
        [HttpDelete("delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                await this.LogAuditAsync(_auditLogService, "User", "Delete", id.ToString(), $"Xóa user #{id}");

                return Ok(new
                {
                    message = "Delete user successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    code = "HAS_RELATED_DATA"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // DELETE: api/users/force-delete/1
        // Dùng khi xóa cứng bị chặn vì tài khoản còn đơn thuốc/hồ sơ liên quan (Restrict FK để giữ hồ
        // sơ y tế) — ẩn danh hóa thông tin cá nhân + khóa vĩnh viễn thay vì xóa cứng.
        [HttpDelete("force-delete/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceDeleteUser(int id)
        {
            try
            {
                var result = await _userService.ForceDeleteUserAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "User not found."
                    });
                }

                await this.LogAuditAsync(_auditLogService, "User", "ForceDelete", id.ToString(), $"Ẩn danh hóa và khóa vĩnh viễn user #{id}");

                return Ok(new
                {
                    message = "Đã ẩn danh hóa và khóa vĩnh viễn tài khoản. Dữ liệu y tế liên quan (đơn thuốc, hồ sơ...) được giữ nguyên."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // POST: api/users/assign-role
        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _userService.AssignRoleAsync(dto);

                if (!result)
                {
                    return NotFound(new
                    {
                        message = "Assign role failed."
                    });
                }

                await this.LogAuditAsync(_auditLogService, "User", "AssignRole", dto.UserId.ToString(), $"Gán vai trò '{dto.RoleName}' cho user #{dto.UserId}");

                return Ok(new
                {
                    message = "Assign role successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        // PUT: api/users/lock/5
        [HttpPut("lock/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LockUser(int id)
        {
            var result = await _userService.LockUserAsync(id);

            if (!result)
                return NotFound(new { message = "User not found." });

            await this.LogAuditAsync(_auditLogService, "User", "Lock", id.ToString(), $"Khóa tài khoản user #{id}");

            return Ok(new { message = "User locked successfully." });
        }

        // PUT: api/users/unlock/5
        [HttpPut("unlock/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var result = await _userService.UnlockUserAsync(id);

            if (!result)
                return NotFound(new { message = "User not found." });

            await this.LogAuditAsync(_auditLogService, "User", "Unlock", id.ToString(), $"Mở khóa tài khoản user #{id}");

            return Ok(new { message = "User unlocked successfully." });
        }

        // PUT: api/users/change-password/1
        [HttpPut("change-password/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangePassword(int id, ChangePasswordDTO dto)
        {
            var result = await _userService.ChangePasswordAsync(id, dto);

            if (!result)
                return BadRequest(new
                {
                    message = "Change password failed."
                });

            return Ok(new
            {
                message = "Password changed successfully."
            });
        }
    }
}
