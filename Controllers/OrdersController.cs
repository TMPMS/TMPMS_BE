using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessObjects;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Services.Interfaces;
using TMPMS.Data;
using TMPMS.Services;
using TMPMS.Services.Interfaces;

namespace TMPMS.Controllers
{
    [ApiController]
    [Route("api/")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly TMPMSDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IShippingFeeService _shippingFeeService;

        public OrdersController(TMPMSDbContext context, IInventoryService inventoryService, IShippingFeeService shippingFeeService)
        {
            _context = context;
            _inventoryService = inventoryService;
            _shippingFeeService = shippingFeeService;
        }

        private async Task<int> GetDefaultWarehouseId()
        {
            var wh = await _context.Warehouses.FirstOrDefaultAsync();
            return wh?.Id ?? 1;
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
            // Tối đa 1 voucher/loại — 1 mã giảm giá sản phẩm + 1 mã giảm phí vận chuyển.
            public string? ProductVoucherCode { get; set; }
            public string? ShippingVoucherCode { get; set; }
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
        [HttpPost("~/orders")]
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

            // Admin/Pharmacy vẫn được tạo đơn THAY MẶT khách hàng (proxy, xem CanProxyOrder ở trên),
            // nhưng không được tự mua hàng cho chính tài khoản nhân viên của mình.
            if (request.UserId == currentUserId.Value && (User.IsInRole("Admin") || User.IsInRole("Pharmacy")))
            {
                return BadRequest(new { message = "Tài khoản Admin/Nhân viên Nhà thuốc không thể tự mua hàng." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Validate stock quantity & lấy giá bán THẬT từ DB — không tin giá do client gửi lên,
                // vì request có thể bị sửa qua devtools/intercept để đặt hàng với giá tuỳ ý.
                var medicinesById = new Dictionary<int, Medicine>();
                decimal subtotal = 0m;
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
                    medicinesById[item.MedicineId] = medicine;
                    subtotal += medicine.Price.Value * item.Quantity;
                }

                // 1.1 Phí ship luôn do SERVER tính lại từ địa chỉ + phương thức giao hàng,
                // không tin số ShippingFee do client gửi lên (tránh gian lận phí ship qua devtools).
                var (serverShippingFee, _, _) = _shippingFeeService.Calculate(request.ShippingAddress, request.DeliveryMethod);

                // 1.2 Áp dụng tối đa 1 voucher sản phẩm + 1 voucher ship — số tiền giảm luôn do
                // SERVER tính lại, không nhận discount do client tự tính. Nếu mã được gửi lên
                // nhưng không hợp lệ thì báo lỗi rõ ràng (không âm thầm bỏ qua).
                Voucher? productVoucher = null;
                Voucher? shippingVoucher = null;
                decimal productDiscount = 0m;
                decimal shippingDiscount = 0m;

                if (!string.IsNullOrWhiteSpace(request.ProductVoucherCode))
                {
                    var result = await VoucherResolver.ResolveAsync(_context, request.ProductVoucherCode, "product", currentUserId);
                    if (result.Voucher == null)
                    {
                        return BadRequest(new { error = result.Error });
                    }
                    if (subtotal < result.Voucher.MinOrderValue)
                    {
                        return BadRequest(new { error = $"Đơn hàng tối thiểu {result.Voucher.MinOrderValue:N0}đ để dùng voucher \"{result.Voucher.Code}\"" });
                    }
                    productVoucher = result.Voucher;
                    productDiscount = VoucherResolver.ComputeDiscount(productVoucher, subtotal);
                    productVoucher.UsedCount += 1;
                }

                if (!string.IsNullOrWhiteSpace(request.ShippingVoucherCode))
                {
                    var result = await VoucherResolver.ResolveAsync(_context, request.ShippingVoucherCode, "shipping", currentUserId);
                    if (result.Voucher == null)
                    {
                        return BadRequest(new { error = result.Error });
                    }
                    if (subtotal < result.Voucher.MinOrderValue)
                    {
                        return BadRequest(new { error = $"Đơn hàng tối thiểu {result.Voucher.MinOrderValue:N0}đ để dùng voucher \"{result.Voucher.Code}\"" });
                    }
                    shippingVoucher = result.Voucher;
                    shippingDiscount = VoucherResolver.ComputeDiscount(shippingVoucher, serverShippingFee);
                    shippingVoucher.UsedCount += 1;
                }

                var finalAmount = Math.Max(0m, subtotal + serverShippingFee - productDiscount - shippingDiscount);

                // 2. Create order
                var order = new Order
                {
                    UserId = request.UserId,
                    TotalAmount = finalAmount,
                    Status = "Pending",
                    ShippingAddress = request.ShippingAddress,
                    PaymentStatus = "Unpaid",
                    DeliveryMethod = request.DeliveryMethod,
                    ShippingFee = serverShippingFee,
                    CreatedAt = DateTime.UtcNow,
                    ProductVoucherId = productVoucher?.Id,
                    ShippingVoucherId = shippingVoucher?.Id
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 3. Add order items (giá lấy từ DB, không lấy từ client) & decrement stock quantity
                // (FEFO — lô hết hạn sớm nhất trừ trước)
                var warehouseId = await GetDefaultWarehouseId();
                foreach (var item in request.Items)
                {
                    await _inventoryService.DeductStockFEFO(item.MedicineId, warehouseId, item.Quantity, $"ORDER-{order.Id}");

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        Price = medicinesById[item.MedicineId].Price.Value
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
                    Amount = finalAmount,
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
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Conflict(new { error = "Voucher vừa hết lượt sử dụng, vui lòng thử lại." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message });
            }
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
        [HttpGet("~/admin/orders")]
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
            var warehouseId = await GetDefaultWarehouseId();
            foreach (var oi in items)
            {
                await _inventoryService.RestoreStockFEFO(oi.MedicineId, warehouseId, oi.Quantity, $"ORDER-{orderId}-RESTOCK");
            }
        }

        // Hoàn tác lượt dùng voucher khi đơn bị hủy/trả — tránh voucher bị "ăn mòn" lượt dùng oan.
        private async Task RollbackVoucherUsageAsync(Order order)
        {
            foreach (var voucherId in new[] { order.ProductVoucherId, order.ShippingVoucherId })
            {
                if (voucherId == null) continue;
                var voucher = await _context.Vouchers.FindAsync(voucherId.Value);
                if (voucher != null)
                {
                    voucher.UsedCount = Math.Max(0, voucher.UsedCount - 1);
                }
            }
        }

        [HttpPost("orders/{id}/cancel")]
        [HttpPost("~/orders/{id}/cancel")]
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
            await RollbackVoucherUsageAsync(order);
            order.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Ok(order);
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
        [HttpPatch("~/admin/orders/{id}")]
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

            // Ghi nhận nhân viên xử lý đơn tại thời điểm giao hàng thành công, dùng cho thống kê
            // doanh thu theo nhân viên. Chỉ gán lần đầu (không ghi đè nếu đã có).
            if (request.Status == "Delivered" && order.ProcessedByStaffId == null)
            {
                order.ProcessedByStaffId = GetUserId();
            }
            if (request.PaymentStatus != null)
            {
                order.PaymentStatus = request.PaymentStatus;
            }

            // Hoàn kho + hoàn lượt dùng voucher khi đơn bị hủy hoặc đã duyệt trả hàng
            // (chỉ 1 lần, không double-restock/double-rollback).
            if (request.Status is "Cancelled" or "Returned" &&
                previousStatus != "Cancelled" && previousStatus != "Returned")
            {
                await RestockOrderItemsAsync(id);
                await RollbackVoucherUsageAsync(order);
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
