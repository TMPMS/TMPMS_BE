using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TMPMS.Data;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly TMPMSDbContext _context;

        public OrdersController(TMPMSDbContext context)
        {
            _context = context;
        }

        public class OrderItemInput
        {
            public int MedicineId { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        public class CheckoutRequest
        {
            public int UserId { get; set; }
            public string ShippingAddress { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public List<OrderItemInput> Items { get; set; } = new();
            public string DeliveryMethod { get; set; } = "Giao hàng hỏa tốc (Ship 2 Giờ)";
            public decimal ShippingFee { get; set; }
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

        [HttpPost("orders")]
        public async Task<IActionResult> CreateOrder([FromBody] CheckoutRequest request)
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

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate stock quantity & price
                foreach (var item in request.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine == null)
                    {
                        return NotFound(new { error = $"Không tìm thấy thuốc với mã ID {item.MedicineId}." });
                    }
                    if (medicine.Price == null)
                    {
                        return BadRequest(new { error = $"Vị thuốc '{medicine.Name}' chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn." });
                    }
                    if (medicine.StockQuantity < item.Quantity)
                    {
                        return BadRequest(new { error = $"Sản phẩm '{medicine.Name}' hiện đã hết hàng hoặc không đủ số lượng tồn kho (Hiện còn: {medicine.StockQuantity})." });
                    }
                }

                // 2. Create order
                var order = new Order
                {
                    UserId = request.UserId,
                    TotalAmount = request.TotalAmount,
                    Status = "Pending",
                    ShippingAddress = request.ShippingAddress,
                    PaymentStatus = "Unpaid",
                    DeliveryMethod = request.DeliveryMethod,
                    ShippingFee = request.ShippingFee,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 3. Add order items & decrement stock quantity
                foreach (var item in request.Items)
                {
                    var medicine = await _context.Medicines.FindAsync(item.MedicineId);
                    if (medicine != null)
                    {
                        medicine.StockQuantity -= item.Quantity;
                    }

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        Price = item.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // 4. Add payment
                // KHÔNG tự đánh dấu "Paid" khi tạo đơn dựa trên chuỗi PaymentMethod.
                // Mọi đơn luôn khởi tạo ở trạng thái chờ thanh toán (Unpaid/Pending);
                // chỉ được chuyển sang Paid qua cổng thanh toán THẬT (PayOS webhook/verify)
                // hoặc do Admin/Accountant đối soát qua PUT /api/Payment/{id}/status.
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = request.PaymentMethod,
                    TransactionCode = "TXN-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = request.TotalAmount,
                    Status = "Pending",
                    PaidAt = null
                };
                _context.Payments.Add(payment);

                // 5. Clear cart items
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == request.UserId);
                if (cart != null)
                {
                    var cartItems = await _context.CartItems.Where(ci => ci.CartId == cart.Id).ToListAsync();
                    _context.CartItems.RemoveRange(cartItems);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return StatusCode(201, order);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("user-orders/{userId}")]
        public async Task<IActionResult> GetUserOrders(int userId)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            // Admin/Staff/Pharmacy được xem đơn của mọi user; user thường chỉ xem đơn của mình.
            if (!CanProxyOrder() && userId != currentUserId.Value)
            {
                return Forbid();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.UserId,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PaymentStatus,
                    o.DeliveryMethod,
                    o.ShippingFee,
                    o.CreatedAt,
                    o.ReturnReason,
                    PaymentId = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => (int?)p.Id).FirstOrDefault(),
                    PaymentMethod = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Method).FirstOrDefault(),
                    PaymentStatusDetail = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Status).FirstOrDefault(),
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new {
                                oi.Id,
                                oi.OrderId,
                                oi.MedicineId,
                                oi.Quantity,
                                oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("admin/orders")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> GetAdminOrders()
        {
            var orders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.UserId,
                    o.TotalAmount,
                    o.Status,
                    o.ShippingAddress,
                    o.PaymentStatus,
                    o.DeliveryMethod,
                    o.ShippingFee,
                    o.CreatedAt,
                    o.ReturnReason,
                    Username = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.UserName).FirstOrDefault(),
                    Email = _context.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault(),
                    PaymentId = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => (int?)p.Id).FirstOrDefault(),
                    PaymentMethod = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Method).FirstOrDefault(),
                    PaymentStatusDetail = _context.Payments.Where(p => p.OrderId == o.Id).OrderByDescending(p => p.Id).Select(p => p.Status).FirstOrDefault(),
                    Items = _context.OrderItems
                        .Where(oi => oi.OrderId == o.Id)
                        .Join(_context.Medicines,
                            oi => oi.MedicineId,
                            m => m.Id,
                            (oi, m) => new {
                                oi.Id,
                                oi.OrderId,
                                oi.MedicineId,
                                oi.Quantity,
                                oi.Price,
                                MedicineName = m.Name,
                                ImageUrl = m.ImageUrl
                            })
                        .ToList()
                })
                .ToListAsync();

            return Ok(orders);
        }

        private async Task RestockOrderItemsAsync(int orderId)
        {
            var items = await _context.OrderItems.Where(oi => oi.OrderId == orderId).ToListAsync();
            foreach (var oi in items)
            {
                var med = await _context.Medicines.FindAsync(oi.MedicineId);
                if (med != null)
                {
                    med.StockQuantity += oi.Quantity;
                }
            }
        }

        [HttpPost("orders/{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");
            if (!CanProxyOrder() && order.UserId != currentUserId.Value) return Forbid();

            if (order.Status != "Pending")
            {
                return BadRequest(new { error = "Chỉ có thể hủy đơn đang ở trạng thái chờ xử lý." });
            }

            await RestockOrderItemsAsync(id);
            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Ok(order);
        }

        public class ReturnRequestInput
        {
            public string Reason { get; set; } = "";
        }

        [HttpPost("orders/{id}/return-request")]
        public async Task<IActionResult> RequestReturn(int id, [FromBody] ReturnRequestInput input)
        {
            var currentUserId = GetUserId();
            if (currentUserId == null) return Unauthorized();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");
            if (!CanProxyOrder() && order.UserId != currentUserId.Value) return Forbid();

            if (order.Status != "Delivered")
            {
                return BadRequest(new { error = "Chỉ có thể yêu cầu trả hàng với đơn đã giao thành công." });
            }
            if (string.IsNullOrWhiteSpace(input.Reason))
            {
                return BadRequest(new { error = "Vui lòng nhập lý do trả hàng." });
            }

            order.Status = "ReturnRequested";
            order.ReturnReason = input.Reason.Trim();
            await _context.SaveChangesAsync();
            return Ok(order);
        }

        public class UpdateStatusRequest
        {
            public string? Status { get; set; }
            public string? PaymentStatus { get; set; }
        }

        [HttpPatch("admin/orders/{id}")]
        [Authorize(Roles = "Admin,Staff,Pharmacy")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound("Order not found");

            var previousStatus = order.Status;
            if (request.Status != null)
            {
                order.Status = request.Status;
            }
            if (request.PaymentStatus != null)
            {
                order.PaymentStatus = request.PaymentStatus;
            }

            // Hoàn kho khi đơn bị hủy hoặc đã duyệt trả hàng (chỉ 1 lần, không double-restock).
            if (request.Status is "Cancelled" or "Returned" &&
                previousStatus != "Cancelled" && previousStatus != "Returned")
            {
                await RestockOrderItemsAsync(id);
            }

            // Duyệt trả hàng: đồng bộ Payment.Status = Refunded + ghi nhận trên đơn.
            if (request.Status == "Returned")
            {
                var payment = await _context.Payments
                    .Where(p => p.OrderId == id)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync();
                if (payment != null)
                {
                    payment.Status = "Refunded";
                }
                order.PaymentStatus = "Refunded";
            }

            // Từ chối trả hàng: đơn quay về đã giao, xóa lý do.
            if (previousStatus == "ReturnRequested" && request.Status == "Delivered")
            {
                order.ReturnReason = null;
            }

            await _context.SaveChangesAsync();
            return Ok(order);
        }
    }
}
