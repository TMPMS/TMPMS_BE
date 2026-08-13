using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly IInventoryService _inventoryService;
        private readonly IShippingFeeService _shippingFeeService;
        private readonly ILoyaltyService _loyaltyService;
        private readonly TMPMS.Data.TMPMSDbContext _context; // dùng cho VoucherResolver (helper dùng chung với VouchersController)

        public OrderService(
            IOrderRepository repo,
            IInventoryService inventoryService,
            IShippingFeeService shippingFeeService,
            ILoyaltyService loyaltyService,
            TMPMS.Data.TMPMSDbContext context)
        {
            _repo = repo;
            _inventoryService = inventoryService;
            _shippingFeeService = shippingFeeService;
            _loyaltyService = loyaltyService;
            _context = context;
        }

        public async Task<OrderActionResult> CreateOrderAsync(CheckoutRequestDto request, int? currentUserId)
        {
            using var transaction = await _repo.BeginTransactionAsync();
            try
            {
                // 1. Validate stock quantity & lấy giá bán THẬT từ DB — không tin giá do client gửi lên,
                // vì request có thể bị sửa qua devtools/intercept để đặt hàng với giá tuỳ ý.
                var medicinesById = new Dictionary<int, Medicine>();
                decimal subtotal = 0m;
                foreach (var item in request.Items)
                {
                    var medicine = await _repo.GetMedicineByIdAsync(item.MedicineId);
                    if (medicine == null)
                    {
                        return Error(OrderErrorType.NotFound, $"Không tìm thấy thuốc với mã ID {item.MedicineId}.");
                    }
                    if (medicine.Price == null)
                    {
                        return Error(OrderErrorType.BadRequest, $"Vị thuốc '{medicine.Name}' chưa có giá bán, vui lòng liên hệ Dược sĩ để được tư vấn.");
                    }
                    if (medicine.StockQuantity < item.Quantity)
                    {
                        return Error(OrderErrorType.BadRequest, $"Sản phẩm '{medicine.Name}' hiện đã hết hàng hoặc không đủ số lượng tồn kho (Hiện còn: {medicine.StockQuantity}).");
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
                        return Error(OrderErrorType.BadRequest, result.Error);
                    }
                    if (subtotal < result.Voucher.MinOrderValue)
                    {
                        return Error(OrderErrorType.BadRequest, $"Đơn hàng tối thiểu {result.Voucher.MinOrderValue:N0}đ để dùng voucher \"{result.Voucher.Code}\"");
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
                        return Error(OrderErrorType.BadRequest, result.Error);
                    }
                    if (subtotal < result.Voucher.MinOrderValue)
                    {
                        return Error(OrderErrorType.BadRequest, $"Đơn hàng tối thiểu {result.Voucher.MinOrderValue:N0}đ để dùng voucher \"{result.Voucher.Code}\"");
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
                await _repo.CreateOrderAsync(order);

                // 3. Add order items (giá lấy từ DB, không lấy từ client) & decrement stock quantity
                // (FEFO — lô hết hạn sớm nhất trừ trước)
                var warehouseId = await _repo.GetDefaultWarehouseIdAsync();
                foreach (var item in request.Items)
                {
                    await _inventoryService.DeductStockFEFO(item.MedicineId, warehouseId, item.Quantity, $"ORDER-{order.Id}");

                    _repo.AddOrderItem(new OrderItem
                    {
                        OrderId = order.Id,
                        MedicineId = item.MedicineId,
                        Quantity = item.Quantity,
                        Price = medicinesById[item.MedicineId].Price!.Value
                    });
                }

                // 4. Add payment
                // KHÔNG tự đánh dấu "Paid" khi tạo đơn dựa trên chuỗi PaymentMethod.
                // Mọi đơn luôn khởi tạo ở trạng thái chờ thanh toán (Unpaid/Pending);
                // chỉ được chuyển sang Paid qua cổng thanh toán THẬT (PayOS webhook/verify)
                // hoặc do Admin/Accountant đối soát qua PUT /api/Payment/{id}/status.
                _repo.AddPayment(new Payment
                {
                    OrderId = order.Id,
                    Method = request.PaymentMethod,
                    TransactionCode = "TXN-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Amount = finalAmount,
                    Status = "Pending",
                    PaidAt = null
                });

                // 5. Clear cart items
                var cart = await _repo.GetCartByUserIdAsync(request.UserId);
                if (cart != null)
                {
                    var cartItems = await _repo.GetCartItemsByCartIdAsync(cart.Id);
                    _repo.RemoveCartItems(cartItems);
                }

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OrderActionResult { Success = true, Order = order };
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return Error(OrderErrorType.Conflict, "Voucher vừa hết lượt sử dụng, vui lòng thử lại.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Error(OrderErrorType.ServerError, ex.Message);
            }
        }

        public Task<List<OrderSummaryDto>> GetUserOrdersAsync(int userId) => _repo.GetUserOrdersAsync(userId);
        public Task<List<OrderSummaryDto>> GetAdminOrdersAsync() => _repo.GetAdminOrdersAsync();
        public Task<Order?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        private async Task RestockOrderItemsAsync(int orderId)
        {
            var items = await _repo.GetOrderItemsAsync(orderId);
            var warehouseId = await _repo.GetDefaultWarehouseIdAsync();
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
                var voucher = await _repo.GetVoucherByIdAsync(voucherId.Value);
                if (voucher != null)
                {
                    voucher.UsedCount = Math.Max(0, voucher.UsedCount - 1);
                }
            }
        }

        public async Task<OrderActionResult> CancelOrderAsync(int id, int currentUserId, bool canProxy)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return Error(OrderErrorType.NotFound, "Order not found");
            if (!canProxy && order.UserId != currentUserId) return Error(OrderErrorType.Forbidden, null);

            if (order.Status != "Pending")
            {
                return Error(OrderErrorType.BadRequest, "Chỉ có thể hủy đơn đang ở trạng thái chờ xử lý.");
            }

            await RestockOrderItemsAsync(id);
            await RollbackVoucherUsageAsync(order);
            order.Status = "Cancelled";
            await _repo.SaveChangesAsync();

            return new OrderActionResult { Success = true, Order = order };
        }

        public async Task<OrderActionResult> RequestReturnAsync(int id, string reason, int currentUserId, bool canProxy)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return Error(OrderErrorType.NotFound, "Order not found");
            if (!canProxy && order.UserId != currentUserId) return Error(OrderErrorType.Forbidden, null);

            if (order.Status != "Delivered")
            {
                return Error(OrderErrorType.BadRequest, "Chỉ có thể yêu cầu trả hàng với đơn đã giao thành công.");
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                return Error(OrderErrorType.BadRequest, "Vui lòng nhập lý do trả hàng.");
            }

            order.Status = "ReturnRequested";
            order.ReturnReason = reason.Trim();
            await _repo.SaveChangesAsync();

            return new OrderActionResult { Success = true, Order = order };
        }

        public async Task<OrderActionResult> UpdateStatusAsync(int id, UpdateOrderStatusRequestDto request, int? actingUserId)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order == null) return Error(OrderErrorType.NotFound, "Order not found");

            var previousStatus = order.Status;
            if (request.Status != null)
            {
                order.Status = request.Status;
            }

            // Ghi nhận nhân viên xử lý đơn tại thời điểm giao hàng thành công, dùng cho thống kê
            // doanh thu theo nhân viên. Chỉ gán lần đầu (không ghi đè nếu đã có).
            if (request.Status == "Delivered" && order.ProcessedByStaffId == null)
            {
                order.ProcessedByStaffId = actingUserId;
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
                await _loyaltyService.ReverseForReturnedOrderAsync(id);
            }

            // Tích điểm loyalty khi đơn giao thành công (không cộng lại nếu đã từng Delivered trước đó).
            if (request.Status == "Delivered" && previousStatus != "Delivered")
            {
                await _loyaltyService.AwardForOrderAsync(id);
            }

            // Duyệt trả hàng: đồng bộ Payment.Status = Refunded + ghi nhận trên đơn.
            if (request.Status == "Returned")
            {
                var payment = await _repo.GetLatestPaymentAsync(id);
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

            await _repo.SaveChangesAsync();

            return new OrderActionResult { Success = true, Order = order, PreviousStatus = previousStatus };
        }

        private static OrderActionResult Error(OrderErrorType type, string? message) =>
            new OrderActionResult { Success = false, ErrorType = type, Error = message };
    }
}
