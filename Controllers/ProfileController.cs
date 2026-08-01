using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using BusinessObjects;
using System.Threading.Tasks;
using System.Linq;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;

        public ProfileController(IUserService userService, UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var headerValue) && int.TryParse(headerValue, out int userId))
            {
                var profile = await _userService.GetProfileAsync(userId);
                if (profile != null) return Ok(profile);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdClaim, out int tokenUserId))
            {
                var profile = await _userService.GetProfileAsync(tokenUserId);
                if (profile != null) return Ok(profile);
            }

            return Unauthorized("Unauthorized profile access.");
        }

        // PATCH: api/profile/me
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            int userId = 0;
            if (Request.Headers.TryGetValue("X-User-Id", out var headerValue) && int.TryParse(headerValue, out int headerUserId))
            {
                userId = headerUserId;
            }
            else
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdClaim, out userId))
                {
                    return Unauthorized();
                }
            }

            var result = await _userService.UpdateProfileAsync(userId, dto);
            if (!result) return NotFound();

            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        // GET: api/profile/5
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        // PUT: api/profile/5
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(int userId, UpdateProfileDto dto)
        {
            var result = await _userService.UpdateProfileAsync(userId, dto);
            if (!result)
                return NotFound();

            return Ok(new { message = "Profile updated successfully." });
        }

        // PUT: api/profile/change-password
        [Authorize]
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId)) return Unauthorized();

            var result = await _userService.ChangePasswordAsync(userId, dto);
            if (!result)
            {
                return BadRequest(new { message = "Old password is incorrect." });
            }

            return Ok(new { message = "Password changed successfully." });
        }

        }
}