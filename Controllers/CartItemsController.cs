using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using TMPMS.Data;
using System;
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

            var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == cartId);

            var items = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .Include(ci => ci.Medicine)
                .ToListAsync();

            var result = new List<object>();
            foreach (var ci in items)
            {
                int? allowedQuantity = null;
                if (ci.Medicine.RequiresPrescription && cart != null)
                {
                    var (_, maxAllowed) = await GetRxAllowanceAsync(cart.UserId, ci.MedicineId);
                    allowedQuantity = Math.Max(0, maxAllowed);
                }
                result.Add(new {
                    Id = ci.Id,
                    CartId = ci.CartId,
                    MedicineId = ci.MedicineId,
                    Quantity = ci.Quantity,
                    AllowedQuantity = allowedQuantity,
                    Medicine = ci.Medicine
                });
            }

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CartItemInput input)
        {
            var medicine = await _context.Medicines.FindAsync(input.MedicineId);
            if (medicine == null || medicine.Price == null)
            {
                return BadRequest(new { error = "Vị thuốc này chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." });
            }

            if (medicine.RequiresPrescription)
            {
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == input.CartId);
                var cartUserId = cart?.UserId ?? 0;
                var (allowed, maxAllowed) = await GetRxAllowanceAsync(cartUserId, input.MedicineId);
                if (!allowed)
                {
                    return StatusCode(403, new { error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." });
                }
                if (maxAllowed <= 0)
                {
                    return BadRequest(new { error = $"Số lượng {medicine.Name} đã đạt tối đa trong đơn thuốc của bạn." });
                }
                var existingForRx = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == input.CartId && ci.MedicineId == input.MedicineId);
                var wouldHave = (existingForRx?.Quantity ?? 0) + input.Quantity;
                if (wouldHave > maxAllowed)
                {
                    return BadRequest(new { error = $"Số lượng {medicine.Name} vượt quá liều lượng trong đơn thuốc (tối đa {maxAllowed})." });
                }
            }

            var existing = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == input.CartId && ci.MedicineId == input.MedicineId);

            if (existing != null)
            {
                existing.Quantity += input.Quantity;
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

            if (item.Quantity != input.Quantity)
            {
                var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                if (medicine != null && medicine.RequiresPrescription)
                {
                    var cart = await _context.Carts.FirstOrDefaultAsync(c => c.Id == item.CartId);
                    var cartUserId = cart?.UserId ?? 0;
                    var (allowed, maxAllowed) = await GetRxAllowanceAsync(cartUserId, item.MedicineId);
                    if (!allowed)
                    {
                        return StatusCode(403, new { error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." });
                    }
                    if (input.Quantity > Math.Max(0, maxAllowed))
                    {
                        return BadRequest(new { error = $"Số lượng {medicine.Name} vượt quá liều lượng trong đơn thuốc (tối đa {Math.Max(0, maxAllowed)})." });
                    }
                }
            }

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

                if (medicine.RequiresPrescription)
                {
                    var (allowed, maxAllowed) = await GetRxAllowanceAsync(input.p_user_id, item.medicine_id);
                    if (!allowed)
                    {
                        return StatusCode(403, new { error = "Sản phẩm này cần được Dược sĩ kê đơn trước khi mua." });
                    }
                    if (item.quantity > Math.Max(0, maxAllowed))
                    {
                        return BadRequest(new { error = $"Số lượng {medicine.Name} vượt quá liều lượng trong đơn thuốc (tối đa {Math.Max(0, maxAllowed)})." });
                    }
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

        // Returns (Allowed, MaxAllowed):
        // Allowed = user has an Approved/Fulfilled prescription containing the medicine.
        // MaxAllowed = total prescribed quantity minus what has already been purchased
        // in non-cancelled orders (does NOT subtract the current cart quantity).
        private async Task<(bool Allowed, int MaxAllowed)> GetRxAllowanceAsync(int userId, int medicineId)
        {
            var prescribed = await _context.PrescriptionItems
                .Where(pi => pi.MedicineId == medicineId && pi.Prescription.UserId == userId)
                .Where(pi => pi.Prescription.Status == "Approved" || pi.Prescription.Status == "Fulfilled")
                .SumAsync(pi => pi.Quantity);

            if (prescribed <= 0) return (false, 0);

            var purchased = await _context.OrderItems
                .Where(oi => oi.MedicineId == medicineId && oi.Order.UserId == userId && oi.Order.Status != "Cancelled")
                .SumAsync(oi => oi.Quantity);

            return (true, Math.Max(0, prescribed - purchased));
        }
    }
}
