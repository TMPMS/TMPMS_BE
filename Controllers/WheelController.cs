using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    public class WheelController : ControllerBase
    {
        private readonly IWheelService _service;

        public WheelController(IWheelService service)
        {
            _service = service;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        private bool IsBlockedRole() => User.IsInRole("Admin") || User.IsInRole("Pharmacy");

        // GET /vouchers/wheel/prizes
        [HttpGet("vouchers/wheel/prizes")]
        [HttpGet("api/vouchers/wheel/prizes")]
        public async Task<IActionResult> GetPrizes()
        {
            var prizes = await _service.GetPrizesAsync();
            return Ok(prizes);
        }

        // GET /vouchers/wheel/status
        [HttpGet("vouchers/wheel/status")]
        [HttpGet("api/vouchers/wheel/status")]
        [Authorize]
        public async Task<IActionResult> GetStatus()
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var status = await _service.GetStatusAsync(currentUserId.Value, IsBlockedRole());

            return Ok(new
            {
                canSpinToday = status.CanSpinToday,
                nextResetAtUtc = status.NextResetAtUtc,
                blocked = status.Blocked,
                lastSpin = status.LastSpinDate == null ? null : new
                {
                    spinDate = status.LastSpinDate,
                    voucher = status.LastSpinVoucher
                }
            });
        }

        // POST /vouchers/wheel/spin
        [HttpPost("vouchers/wheel/spin")]
        [HttpPost("api/vouchers/wheel/spin")]
        [Authorize]
        public async Task<IActionResult> Spin()
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();
            if (IsBlockedRole()) return Forbid();

            var result = await _service.SpinAsync(currentUserId.Value, isBlockedRole: false);

            if (!result.Success)
            {
                return result.AlreadySpunToday
                    ? Conflict(new { error = result.Error })
                    : BadRequest(new { error = result.Error });
            }

            return Ok(new { voucher = result.Voucher, prizeIndex = result.PrizeIndex });
        }
    }
}
