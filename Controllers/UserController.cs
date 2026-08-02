using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPMS.DTOs;
using TMPMS.DTOs.TMPMS_BE.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // Endpoint: POST https://localhost:xxxx/api/users/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var result = await _userService.CreateUserAsync(dto);
                if (result)
                {
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

                return Ok(new
                {
                    message = "Delete user successfully."
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
        public async Task<IActionResult> LockUser(int id)
        {
            var result = await _userService.LockUserAsync(id);

            if (!result)
                return NotFound(new { message = "User not found." });

            return Ok(new { message = "User locked successfully." });
        }

        // PUT: api/users/unlock/5
        [HttpPut("unlock/{id}")]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var result = await _userService.UnlockUserAsync(id);

            if (!result)
                return NotFound(new { message = "User not found." });

            return Ok(new { message = "User unlocked successfully." });
        }

        // PUT: api/users/change-password/1
        [HttpPut("change-password/{id}")]
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
