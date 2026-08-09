using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System.Security.Claims;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("carts")]
    [Route("api/carts")]
    [Authorize]
    public class CartsController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public CartsController(TMPMSDbContext context)
        {
            _context = context;
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

            var carts = await _context.Carts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(carts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCart([FromBody] CartCreateInput input)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && input.UserId != currentUserId.Value) return Forbid();

            var cart = new Cart
            {
                UserId = CanProxy() ? input.UserId : currentUserId.Value
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
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
