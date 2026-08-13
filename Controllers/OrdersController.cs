using Microsoft.AspNetCore.Mvc;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;
using TMPMS.Utils;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _service;
        private readonly IAuditLogService _auditLogService;

        public OrdersController(IOrderService service, IAuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : (int?)null;
        }

        private bool CanProxyOrder()
        {
            return User.IsInRole("Admin") || User.IsInRole("Staff") || User.IsInRole("Pharmacy");
        }

        private IActionResult ToActionResult(OrderActionResult result)
        {
            return result.ErrorType switch
            {
                OrderErrorType.NotFound => NotFound(new { error = result.Error }),
                OrderErrorType.Conflict => Conflict(new { error = result.Error }),
                OrderErrorType.Forbidden => Forbid(),
                OrderErrorType.ServerError => StatusCode(500, new { error = result.Error }),
                _ => BadRequest(new { error = result.Error }),
            };
        }

        [HttpPost("orders")]
        [HttpPost("~/orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequestDto request)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            // Người dùng thường chỉ được tạo đơn cho CHÍNH MÌNH.
            // Admin/Staff/Pharmacy được phép tạo đơn thay mặt khách hàng (proxy).
            if (!CanProxyOrder())
            {
                if (request.UserId != currentUserId.Value)
                {
                    return Forbid();
                }
                request.UserId = currentUserId.Value;
            }
            else if (request.UserId <= 0)
            {
                request.UserId = currentUserId.Value;
            }

            // Admin/Pharmacy vẫn được tạo đơn THAY MẶT khách hàng (proxy, xem CanProxyOrder ở trên),
            // nhưng không được tự mua hàng cho chính tài khoản nhân viên của mình.
            if (request.UserId == currentUserId.Value && (User.IsInRole("Admin") || User.IsInRole("Pharmacy")))
            {
                return BadRequest(new { message = "Tài khoản Admin/Nhân viên Nhà thuốc không thể tự mua hàng." });
            }

            var result = await _service.CreateOrderAsync(request, currentUserId);
            if (!result.Success)
            {
                return ToActionResult(result);
            }

            await this.LogAuditAsync(_auditLogService, "Order", "Create", result.Order!.Id.ToString(), $"Tạo đơn hàng #{result.Order.Id} cho user #{result.Order.UserId} — tổng tiền {result.Order.TotalAmount:N0}đ");

            return StatusCode(201, result.Order);
        }

        [HttpGet("user-orders/{userId}")]
        [HttpGet("~/user-orders/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            // Admin/Staff/Pharmacy được xem đơn của mọi user; user thường chỉ xem đơn của mình.
            if (!CanProxyOrder() && userId != currentUserId.Value)
            {
                return Forbid();
            }

            var orders = await _service.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("admin/orders")]
        [HttpGet("~/admin/orders")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetAdminOrders()
        {
            var orders = await _service.GetAdminOrdersAsync();
            return Ok(orders);
        }

        [HttpPost("orders/{id}/cancel")]
        [HttpPost("~/orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _service.CancelOrderAsync(id, currentUserId.Value, CanProxyOrder());
            if (!result.Success) return ToActionResult(result);

            await this.LogAuditAsync(_auditLogService, "Order", "Cancel", id.ToString(), $"Hủy đơn hàng #{id}");

            return Ok(result.Order);
        }

        public class ReturnRequestInput
        {
            public string Reason { get; set; } = "";
        }

        [HttpPost("orders/{id}/return-request")]
        [HttpPost("~/orders/{id}/return-request")]
        public async Task<IActionResult> RequestReturn(int id, [FromBody] ReturnRequestInput input)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var result = await _service.RequestReturnAsync(id, input.Reason, currentUserId.Value, CanProxyOrder());
            if (!result.Success) return ToActionResult(result);

            await this.LogAuditAsync(_auditLogService, "Order", "ReturnRequest", id.ToString(), $"Yêu cầu trả hàng đơn #{id} — lý do: {result.Order!.ReturnReason}");

            return Ok(result.Order);
        }

        [HttpPatch("admin/orders/{id}")]
        [HttpPatch("~/admin/orders/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequestDto request)
        {
            var result = await _service.UpdateStatusAsync(id, request, GetUserId());
            if (!result.Success) return ToActionResult(result);

            if (request.Status != null && request.Status != result.PreviousStatus)
            {
                await this.LogAuditAsync(_auditLogService, "Order", "UpdateStatus", id.ToString(), $"Đổi trạng thái đơn #{id}: {result.PreviousStatus} → {request.Status}");
            }

            return Ok(result.Order);
        }
    }
}
