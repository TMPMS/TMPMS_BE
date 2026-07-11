using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ControllerBase
    {
        private readonly IUserService _userService;

        public ProfileController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/profile/1
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetProfile(int userId)
        {
            var profile = await _userService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        // PUT: api/profile/1
        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateProfile(int userId, UpdateProfileDto dto)
        {
            var result = await _userService.UpdateProfileAsync(userId, dto);

            if (!result)
                return NotFound();

            return Ok(new
            {
                message = "Profile updated successfully."
            });
        }
    }
}
