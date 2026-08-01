using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("carts")]
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

            var carts = await _context.Carts
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return Ok(carts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCart([FromBody] CartCreateInput input)
        {
            var cart = new Cart
            {
                UserId = input.UserId
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
            return StatusCode(201, cart);
        }
    }
}
