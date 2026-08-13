using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessObjects;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("cart_items")]
    [Route("api/cart_items")]
    [Authorize]
    public class CartItemsController : ControllerBase
    {
        private readonly ICartItemService _service;
        private readonly IAuditLogService _auditLogService;

        public CartItemsController(ICartItemService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
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

            var cart = await GetOwnedCartAsync(cartId);
            if (cart == null) return Forbid();

            var items = await _service.GetItemsAsync(cart);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CartItemInput input)
        {
            var cart = await GetOwnedCartAsync(input.CartId);
            if (cart == null) return Forbid();

            var result = await _service.AddItemAsync(cart, input.MedicineId, input.Quantity);
            if (!result.Success)
            {
                return result.RequiresPrescription ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
            }

            await LogIfProxyAsync(cart, "Add", $"Thêm sản phẩm #{input.MedicineId} (SL {input.Quantity}) vào giỏ hàng của user #{cart.UserId}");

            return result.Created ? StatusCode(201, result.Item) : Ok(result.Item);
        }

        [HttpPatch]
        public async Task<IActionResult> UpdateCartItem([FromQuery(Name = "id")] string? idStr, [FromBody] UpdateQtyInput input)
        {
            if (string.IsNullOrEmpty(idStr)) return BadRequest("Missing id");
            var cleanId = idStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int itemId)) return BadRequest("Invalid id format");

            var item = await _service.GetItemByIdAsync(itemId);
            if (item == null) return NotFound("Cart item not found");

            var cart = await GetOwnedCartAsync(item.CartId);
            if (cart == null) return Forbid();

            var result = await _service.UpdateItemAsync(cart, item, input.Quantity);
            if (!result.Success)
            {
                return result.RequiresPrescription ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
            }

            await LogIfProxyAsync(cart, "Update", $"Sửa số lượng sản phẩm #{item.MedicineId} thành {input.Quantity} trong giỏ hàng của user #{cart.UserId}");

            return Ok(result.Item);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCartItem([FromQuery(Name = "id")] string? idStr)
        {
            if (string.IsNullOrEmpty(idStr)) return BadRequest("Missing id");
            var cleanId = idStr.Replace("eq.", "");
            if (!int.TryParse(cleanId, out int itemId)) return BadRequest("Invalid id");

            var item = await _service.GetItemByIdAsync(itemId);
            if (item == null) return NotFound();

            var cart = await GetOwnedCartAsync(item.CartId);
            if (cart == null) return Forbid();

            await _service.RemoveItemAsync(item);

            await LogIfProxyAsync(cart, "Delete", $"Xóa sản phẩm #{item.MedicineId} khỏi giỏ hàng của user #{cart.UserId}");

            return Ok(new { message = "Deleted" });
        }

        [HttpPost]
        [Route("~/rpc/sync_cart_items")]
        [Route("~/api/rpc/sync_cart_items")]
        public async Task<IActionResult> SyncCartItems([FromBody] SyncInput input)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();
            if (!CanProxy() && input.p_user_id != currentUserId.Value) return Forbid();

            var syncItems = input.p_items.Select(i => new CartSyncItemInput { MedicineId = i.medicine_id, Quantity = i.quantity }).ToList();
            var result = await _service.SyncAsync(input.p_user_id, syncItems);
            if (!result.Success)
            {
                return result.RequiresPrescription ? StatusCode(403, new { error = result.Error }) : BadRequest(new { error = result.Error });
            }

            if (currentUserId.Value != input.p_user_id)
            {
                await this.LogAuditAsync(_auditLogService, "Cart", "Sync", result.Cart!.Id.ToString(), $"Đồng bộ giỏ hàng khách (offline) cho user #{input.p_user_id}");
            }

            return Ok(new { message = "Synced successfully" });
        }

        // Chỉ ghi audit log khi Admin/Staff/Pharmacy thao tác THAY MẶT khách hàng khác —
        // khách tự sửa giỏ hàng của chính mình là hành vi bình thường, không cần audit trail.
        private async Task LogIfProxyAsync(Cart cart, string action, string description)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId != null && currentUserId.Value != cart.UserId)
            {
                await this.LogAuditAsync(_auditLogService, "CartItem", action, cart.Id.ToString(), description);
            }
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

        private async Task<Cart?> GetOwnedCartAsync(int cartId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return null;
            return await _service.GetOwnedCartAsync(cartId, currentUserId.Value, CanProxy());
        }
    }
}
