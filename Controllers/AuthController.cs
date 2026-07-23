using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) => _authService = authService;

        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] RegisterRequestDTO dto)
        {
            try
            {
                var result = await _authService.Register(dto);
                return Ok(result);
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login([FromBody] LoginRequestDTO dto)
        {
            try
            {
                var result = await _authService.Login(dto, GetIp());
                if (result == null) return Unauthorized("Email hoặc mật khẩu không đúng.");
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return StatusCode(423, ex.Message); } // Locked
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO dto)
        {
            try
            {
                var result = await _authService.RefreshToken(dto.RefreshToken, GetIp());
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<ActionResult> RevokeToken([FromBody] RevokeTokenRequestDTO dto)
        {
            var ok = await _authService.RevokeToken(dto.RefreshToken, GetIp());
            if (!ok) return NotFound("Token không tồn tại hoặc đã bị thu hồi.");
            return Ok(new { message = "Đăng xuất thành công." });
        }

        [HttpPost("assign-role")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> AssignRole([FromBody] AssignRoleRequestDTO dto)
        {
            var ok = await _authService.AssignRole(dto);
            if (!ok) return BadRequest("Không thể gán role cho user.");
            return Ok(new { message = "Gán role thành công." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult> GetProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var profile = await _authService.GetProfile(userId);
            if (profile == null) return NotFound();
            return Ok(profile);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDTO dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            try
            {
                var ok = await _authService.ChangePassword(userId, dto);
                if (!ok) return NotFound();
                return Ok(new { message = "Đổi mật khẩu thành công." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
