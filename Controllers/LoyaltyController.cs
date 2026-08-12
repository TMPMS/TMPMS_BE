using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Authorize]
    public class LoyaltyController : ControllerBase
    {
        private readonly ILoyaltyService _service;
        public LoyaltyController(ILoyaltyService service) => _service = service;

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        [HttpGet("~/loyalty/me")]
        [HttpGet("~/api/loyalty/me")]
        public async Task<IActionResult> GetMySummary()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            return Ok(await _service.GetSummaryAsync(userId.Value));
        }

        [HttpPost("~/loyalty/redeem")]
        [HttpPost("~/api/loyalty/redeem")]
        public async Task<IActionResult> Redeem([FromBody] RedeemPointsDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();
            try
            {
                var result = await _service.RedeemAsync(userId.Value, dto.Points);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
