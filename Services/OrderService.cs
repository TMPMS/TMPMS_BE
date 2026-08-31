using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IEmailService _emailService;
        private readonly TMPMS.Data.TMPMSDbContext _context; // dùng cho VoucherResolver (helper dùng chung với VouchersController)

        public OrderService(
            IOrderRepository repo,
            IInventoryService inventoryService,
            IShippingFeeService shippingFeeService,
            ILoyaltyService loyaltyService,
            IEmailService emailService,
            TMPMS.Data.TMPMSDbContext context)
        {
            _repo = repo;
            _inventoryService = inventoryService;
            _shippingFeeService = shippingFeeService;
            _loyaltyService = loyaltyService;
            _emailService = emailService;
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

                // Làm tròn về đồng nguyên ngay tại đây (VNĐ không có phần lẻ) — trước đây TotalAmount có
                // thể lẻ do voucher % không làm tròn (vd giảm 10% của 99.999đ = 9.999,9đ). PayOS chỉ nhận
                // số nguyên nên link thanh toán bị làm tròn riêng ở PayOSController, còn webhook lại so
                // khớp với TotalAmount gốc chưa làm tròn — không bao giờ khớp, khiến đơn không được xác
                // nhận "Paid" dù khách đã trả đúng số tiền đã làm tròn.
                var finalAmount = Math.Round(Math.Max(0m, subtotal + serverShippingFee - productDiscount - shippingDiscount), 0, MidpointRounding.AwayFromZero);

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

                // 5. Xoá khỏi giỏ hàng CHỈ những dòng vừa đặt — khách có thể chỉ tick chọn một phần
                // giỏ hàng để mua (FE gửi lên đúng request.Items), phần chưa chọn phải còn nguyên
                // trong giỏ để mua sau, không bị xoá sạch toàn bộ giỏ như trước.
                var orderedMedicineIds = request.Items.Select(i => i.MedicineId).ToHashSet();
                var cart = await _repo.GetCartByUserIdAsync(request.UserId);
                if (cart != null)
                {
                    var cartItems = await _repo.GetCartItemsByCartIdAsync(cart.Id);
                    var itemsToRemove = cartItems.Where(ci => orderedMedicineIds.Contains(ci.MedicineId)).ToList();
                    _repo.RemoveCartItems(itemsToRemove);
                }

                await _repo.SaveChangesAsync();
                await transaction.CommitAsync();

                await SendOrderConfirmationEmailAsync(order, request, medicinesById, subtotal, serverShippingFee, productDiscount, shippingDiscount, finalAmount);

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

        private async Task SendOrderConfirmationEmailAsync(Order order, CheckoutRequestDto request, Dictionary<int, Medicine> medicinesById,
            decimal subtotal, decimal shippingFee, decimal productDiscount, decimal shippingDiscount, decimal finalAmount)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null || string.IsNullOrWhiteSpace(user.Email)) return;

            var itemsHtml = string.Join("", request.Items.Select(item =>
            {
                var medicine = medicinesById[item.MedicineId];
                return "<tr>" +
                    $"<td style=\"padding:4px 8px;\">{System.Net.WebUtility.HtmlEncode(medicine.Name)}</td>" +
                    $"<td style=\"padding:4px 8px;text-align:center;\">x{item.Quantity}</td>" +
                    $"<td style=\"padding:4px 8px;text-align:right;\">{(medicine.Price!.Value * item.Quantity):N0}đ</td>" +
                    "</tr>";
            }));

            var discountsHtml =
                (productDiscount > 0 ? $"Giảm giá sản phẩm: -{productDiscount:N0}đ<br/>" : "") +
                (shippingDiscount > 0 ? $"Giảm giá vận chuyển: -{shippingDiscount:N0}đ<br/>" : "");

            await _emailService.SendEmailAsync(
                user.Email,
                $"Đã xác nhận đơn hàng #{order.Id} - TMPMS",
                $"<p>Xin chào {System.Net.WebUtility.HtmlEncode(user.FullName ?? user.UserName)},</p>" +
                $"<p>Đơn hàng <b>#{order.Id}</b> của bạn đã được xác nhận và đang chờ xử lý.</p>" +
                $"<table style=\"border-collapse:collapse;width:100%;\">{itemsHtml}</table>" +
                "<p>" +
                $"Tạm tính: {subtotal:N0}đ<br/>" +
                $"Phí vận chuyển: {shippingFee:N0}đ<br/>" +
                discountsHtml +
                $"<b>Tổng cộng: {finalAmount:N0}đ</b>" +
                "</p>" +
                $"<p>Địa chỉ giao hàng: {System.Net.WebUtility.HtmlEncode(order.ShippingAddress)}</p>" +
                "<p>Chúng tôi sẽ gửi thông báo ngay khi đơn hàng được giao.</p>");
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

        // Tự động hủy + hoàn kho các đơn "Pending"/"Unpaid" quá lâu (khách bỏ ngang, không qua PayOS
        // nên không có webhook nào tự hủy) — gọi định kỳ từ StaleOrderBackgroundService. Dùng lại đúng
        // logic hoàn kho/voucher của CancelOrderAsync để không lệch hành vi giữa hủy thủ công và tự động.
        public async Task<int> AutoCancelStaleOrdersAsync(TimeSpan staleAfter)
        {
            var cutoff = DateTime.UtcNow - staleAfter;
            var staleIds = await _repo.GetStalePendingOrderIdsAsync(cutoff);
            var cancelledCount = 0;

            foreach (var id in staleIds)
            {
                var order = await _repo.GetByIdAsync(id);
                if (order == null || order.Status != "Pending" || order.PaymentStatus != "Unpaid") continue;

                await RestockOrderItemsAsync(id);
                await RollbackVoucherUsageAsync(order);
                order.Status = "Cancelled";
                await _repo.SaveChangesAsync();
                cancelledCount++;
            }

            return cancelledCount;
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

            // Đơn đã thanh toán (vd PayOS xác nhận xong nhưng staff chưa kịp chuyển Status khỏi Pending)
            // không được hủy qua đường này — hủy ở đây chỉ hoàn kho/voucher, KHÔNG có bước hoàn tiền, sẽ
            // làm tiền khách đã trả bị "kẹt" ngoài quy trình. Phải đi qua Yêu cầu trả hàng (RequestReturnAsync)
            // để staff duyệt và đồng bộ Payment.Status/PaymentStatus = Refunded đúng quy trình.
            if (order.PaymentStatus == "Paid")
            {
                return Error(OrderErrorType.BadRequest, "Đơn đã thanh toán — vui lòng dùng chức năng Yêu cầu trả hàng để được hoàn tiền đúng quy trình.");
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
