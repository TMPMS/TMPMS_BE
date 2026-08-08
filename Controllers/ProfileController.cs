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
    [Route("profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<User> _userManager;

        public ProfileController(IUserService userService, UserManager<User> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out var id) ? id : (int?)null;
        }

        private bool CanProxy()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }

        // GET: api/profile/me
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var profile = await _userService.GetProfileAsync(userId.Value);
            if (profile == null) return NotFound();

            return Ok(profile);
        }

        // PATCH: api/profile/me
        [HttpPatch("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _userService.UpdateProfileAsync(userId.Value, dto);
            if (!result) return NotFound();

            var profile = await _userService.GetProfileAsync(userId.Value);
            return Ok(profile);
        }

        // GET: api/profile/5
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            // Người dùng thường chỉ xem profile của chính mình; Admin/Staff/Pharmacy xem được mọi người.
            if (!CanProxy() && userId != currentUserId.Value)
            {
                return Forbid();
            }

            var profile = await _userService.GetProfileAsync(userId);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        // PUT: api/profile/5
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(int userId, UpdateProfileDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            if (!CanProxy() && userId != currentUserId.Value)
            {
                return Forbid();
            }

            var result = await _userService.UpdateProfileAsync(userId, dto);
            if (!result)
                return NotFound();

            return Ok(new { message = "Profile updated successfully." });
        }

        // PUT: api/profile/change-password
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDTO dto)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return Unauthorized();

            var result = await _userService.ChangePasswordAsync(userId.Value, dto);
            if (!result)
            {
                return BadRequest(new { message = "Old password is incorrect." });
            }

            return Ok(new { message = "Password changed successfully." });
        }
    }
}
