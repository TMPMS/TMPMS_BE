using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System.Text.Json.Serialization;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("cart_items")]
    public class CartItemsController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public CartItemsController(TMPMSDbContext context)
        {
            _context = context;
        }

        public class CartItemInput
        {
            [JsonPropertyName("cart_id")]
            public int CartId { get; set; }

            [JsonPropertyName("medicine_id")]
            public int MedicineId { get; set; }

            [JsonPropertyName("quantity")]
            public int Quantity { get; set; }
        }

        public class UpdateQtyInput
        {
            public int Quantity { get; set; }
        }

        public class SyncInputItem
        {
            public int medicine_id { get; set; }
            public int quantity { get; set; }
        }

        public class SyncInput
        {
            public int p_user_id { get; set; }
            public List<SyncInputItem> p_items { get; set; } = new();
        }

        [HttpGet]
        public async Task<IActionResult> GetCartItems([FromQuery(Name = "cart_id")] string? cartIdStr)
        {
            if (string.IsNullOrEmpty(cartIdStr)) return BadRequest("Missing cart_id");
            var cleanId = cartIdStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int cartId)) return BadRequest("Invalid cart_id format");

            var items = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Include(ci => ci.Medicine)
                .Select(ci => new {
                    Id = ci.Id,
                    CartId = ci.CartId,
                    MedicineId = ci.MedicineId,
                    Quantity = ci.Quantity,
                    Medicine = ci.Medicine
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CartItemInput input)
        {
            var medicine = await _context.Medicines.FindAsync(input.MedicineId);
            if (medicine == null || medicine.Price == null)
            {
                return BadRequest(new { error = "Vị thuốc này chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." });
            }

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == input.CartId && ci.MedicineId == input.MedicineId);

            if (existing != null)
            {
                existing.Quantity = input.Quantity;
                await _context.SaveChangesAsync();
                return Ok(existing);
            }

            var newItem = new CartItem
            {
                CartId = input.CartId,
                MedicineId = input.MedicineId,
                Quantity = input.Quantity
            };
            _context.CartItems.Add(newItem);
            await _context.SaveChangesAsync();
            return StatusCode(201, newItem);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateCartItem([FromQuery(Name = "id")] string? idStr, [FromBody] UpdateQtyInput input)
        {
            if (string.IsNullOrEmpty(idStr)) return BadRequest("Missing id");
            var cleanId = idStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int itemId)) return BadRequest("Invalid id format");

            var item = await _context.CartItems.FindAsync(itemId);
            if (item == null) return NotFound("Cart item not found");

            item.Quantity = input.Quantity;
            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCartItem([FromQuery(Name = "id")] string? idStr)
        {
            if (string.IsNullOrEmpty(idStr)) return BadRequest("Missing id");
            var cleanId = idStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int itemId)) return BadRequest("Invalid id");

            var item = await _context.CartItems.FindAsync(itemId);
            if (item == null) return NotFound();

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Deleted" });
        }

        [HttpPost]
        [Route("~/rpc/sync_cart_items")]
        public async Task<IActionResult> SyncCartItems([FromBody] SyncInput input)
        {
            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == input.p_user_id);
            if (cart == null)
            {
                cart = new Cart { UserId = input.p_user_id };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            foreach (var item in input.p_items)
            {
                var medicine = await _context.Medicines.FindAsync(item.medicine_id);
                if (medicine == null || medicine.Price == null)
                {
                    return BadRequest(new { error = $"Vị thuốc '{medicine?.Name ?? item.medicine_id.ToString()}' chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." });
                }

                var existing = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.MedicineId == item.medicine_id);
                
                if (existing != null)
                {
                    existing.Quantity += item.quantity;
                }
                else
                {
                    _context.CartItems.Add(new CartItem
                    {
                        CartId = cart.Id,
                        MedicineId = item.medicine_id,
                        Quantity = item.quantity
                    });
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Synced successfully" });
        }
    }
}
