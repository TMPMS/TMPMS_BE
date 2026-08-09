using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;
using TMPMS.DTOs;

namespace TMPMS.Controllers
{
    [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        private string GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        // Đặt token vào httpOnly cookie thay vì chỉ trả trong JSON body — JS phía client không
        // đọc được nữa, giảm hẳn rủi ro XSS đánh cắp token. SameSite=Lax vì FE/BE cùng origin
        // (xem Program.cs CORS) nên đã đủ chặn CSRF cho các request state-changing, không cần
        // thêm cơ chế CSRF-token riêng.
        private void SetAuthCookies(AuthResponseDTO result)
        {
            Response.Cookies.Append("access_token", result.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = result.AccessTokenExpiresAt,
                Path = "/"
            });

            var refreshTokenDays = int.TryParse(_configuration["JWT:RefreshTokenExpiryDays"], out var d) ? d : 7;
            Response.Cookies.Append("refresh_token", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(refreshTokenDays),
                Path = "/"
            });
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("access_token", new CookieOptions { Path = "/" });
            Response.Cookies.Delete("refresh_token", new CookieOptions { Path = "/" });
        }

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
                SetAuthCookies(result);
                return Ok(result);
            }
            catch (InvalidOperationException ex) { return StatusCode(423, ex.Message); } // Locked
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("google-login")]
        public async Task<ActionResult> GoogleLogin([FromBody] GoogleLoginRequestDTO dto)
        {
            try
            {
                var result = await _authService.GoogleLogin(dto, GetIp());
                SetAuthCookies(result);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); } // ID token giả/hết hạn
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }      // tài khoản bị khóa / đã liên kết Google khác
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO? dto = null)
        {
            var refreshToken = !string.IsNullOrEmpty(dto?.RefreshToken) ? dto.RefreshToken : Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken)) return Unauthorized("Không có refresh token.");

            try
            {
                var result = await _authService.RefreshToken(refreshToken, GetIp());
                SetAuthCookies(result);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(ex.Message); }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<ActionResult> RevokeToken([FromBody] RevokeTokenRequestDTO? dto = null)
        {
            var refreshToken = !string.IsNullOrEmpty(dto?.RefreshToken) ? dto.RefreshToken : Request.Cookies["refresh_token"];
            ClearAuthCookies();
            if (string.IsNullOrEmpty(refreshToken)) return Ok(new { message = "Đăng xuất thành công." });

            var ok = await _authService.RevokeToken(refreshToken, GetIp());
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

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO dto)
        {
            try
            {
                await _authService.SendPasswordResetOtp(dto);
                return Ok(new { message = "Nếu email đã đăng ký, mã xác nhận sẽ được gửi trong ít phút." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPost("reset-password")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO dto)
        {
            try
            {
                await _authService.ResetPassword(dto);
                return Ok(new { message = "Đặt lại mật khẩu thành công." });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }
    }
}
