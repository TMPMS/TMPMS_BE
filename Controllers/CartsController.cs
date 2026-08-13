using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("carts")]
    [Route("api/carts")]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _service;
        private readonly IAuditLogService _auditLogService;

        public CartsController(ICartService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        public class CartCreateInput
        {
            public int UserId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> GetCarts([FromQuery(Name = "user_id")] string? userIdStr)
        {
            if (string.IsNullOrEmpty(userIdStr)) return BadRequest("Missing user_id");
            var cleanId = userIdStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int userId)) return BadRequest("Invalid user_id format");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && userId != currentUserId.Value) return Forbid();

            var carts = await _service.GetCartsByUserIdAsync(userId);
            return Ok(carts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCart([FromBody] CartCreateInput input)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && input.UserId != currentUserId.Value) return Forbid();

            var targetUserId = CanProxy() ? input.UserId : currentUserId.Value;
            var cart = await _service.CreateCartAsync(targetUserId);

            if (currentUserId.Value != cart.UserId)
            {
                await this.LogAuditAsync(_auditLogService, "Cart", "Create", cart.Id.ToString(), $"Tạo giỏ hàng thay mặt user #{cart.UserId}");
            }

            return StatusCode(201, cart);
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
    }
}
