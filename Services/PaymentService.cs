using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Services.Interfaces;

namespace TMPMS.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        private readonly ILoyaltyService _loyaltyService;
        public PaymentService(IPaymentRepository repo, ILoyaltyService loyaltyService)
        {
            _repo = repo;
            _loyaltyService = loyaltyService;
        }

        public async Task<PaymentResponseDTO> CreatePayment(PaymentCreateDTO dto, int currentUserId, bool canProxy)
        {
            var order = await _repo.GetOrderById(dto.OrderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại.");
            if (!canProxy && order.UserId != currentUserId)
                throw new UnauthorizedAccessException("Không có quyền tạo thanh toán cho đơn hàng này.");

            var allowedMethods = new[] { "Cash", "COD", "BankTransfer", "CreditCard", "MoMo", "MOMO", "ZaloPay", "ZALOPAY", "VNPay", "PayOS" };
            if (!allowedMethods.Contains(dto.Method))
                throw new ArgumentException("Phương thức thanh toán không hợp lệ.");

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Method = dto.Method,
                // Không tin số tiền client gửi — luôn ghi theo tổng tiền thực tế của đơn hàng ở server,
                // tránh chèn bản ghi Payment với số tiền tùy ý cho đơn của người khác.
                Amount = order.TotalAmount,
                Status = "Pending",
                TransactionCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()
            };

            var created = await _repo.Create(payment);
            return Map(created);
        }

        public async Task<PaymentResponseDTO> GetById(int id, int currentUserId, bool canProxy)
        {
            var entity = await _repo.GetById(id);
            if (entity == null) return null;
            if (!canProxy && entity.Order != null && entity.Order.UserId != currentUserId)
                throw new UnauthorizedAccessException("Không có quyền xem thanh toán này.");
            return Map(entity);
        }

        public async Task<List<PaymentResponseDTO>> GetByOrder(int orderId, int currentUserId, bool canProxy)
        {
            if (!canProxy)
            {
                var order = await _repo.GetOrderById(orderId);
                if (order == null || order.UserId != currentUserId)
                    throw new UnauthorizedAccessException("Không có quyền xem thanh toán của đơn hàng này.");
            }

            var list = await _repo.GetByOrder(orderId);
            return list.Select(Map).ToList();
        }

        // Cập nhật trạng thái thanh toán; nếu Success thì đồng bộ trạng thái đơn hàng
        public async Task<PaymentResponseDTO> UpdateStatus(int id, PaymentUpdateStatusDTO dto)
        {
            var payment = await _repo.GetById(id);
            if (payment == null) return null;

            var allowedStatuses = new[] { "Pending", "Success", "Failed", "Refunded" };
            if (!allowedStatuses.Contains(dto.Status))
                throw new ArgumentException("Trạng thái thanh toán không hợp lệ.");

            // Đối chiếu số tiền trước khi cho phép xác nhận "Success" — trước đây không kiểm tra gì,
            // Admin/Kế toán có thể xác nhận một Payment có Amount thấp hơn TotalAmount thật của đơn
            // (vd bản ghi Payment cũ/trùng còn sót lại, hoặc đơn đã đổi tổng tiền sau khi tạo Payment)
            // mà đơn vẫn bị đánh dấu "Paid" toàn bộ dù số tiền ghi nhận chưa đủ.
            if (dto.Status == "Success" && payment.Order != null && payment.Amount < payment.Order.TotalAmount)
                throw new ArgumentException($"Số tiền thanh toán ({payment.Amount:N0}đ) chưa khớp với tổng tiền đơn hàng ({payment.Order.TotalAmount:N0}đ). Vui lòng đối soát lại trước khi xác nhận.");

            payment.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.TransactionCode))
                payment.TransactionCode = dto.TransactionCode;

            if (dto.Status == "Success")
            {
                payment.PaidAt = DateTime.Now;
                if (payment.Order != null)
                {
                    payment.Order.PaymentStatus = "Paid";
                    // Đơn COD: hàng giao trước, thu tiền sau (nút "Xác nhận thu tiền" riêng biệt với
                    // "Xác nhận đã giao"). OrderService.UpdateStatusAsync giờ chỉ cộng điểm loyalty khi
                    // Delivered VÀ đã Paid cùng lúc — nên với COD, thời điểm thực sự "đủ điều kiện cộng
                    // điểm" là ở ĐÂY (khi xác nhận thu tiền xong, nếu đơn đã Delivered từ trước).
                    // AwardForOrderAsync tự chống cộng trùng nên gọi lại vô hại nếu đã cộng qua đường khác.
                    if (payment.Order.Status == "Delivered")
                        await _loyaltyService.AwardForOrderAsync(payment.OrderId);
                }
            }
            else if (dto.Status == "Failed")
            {
                if (payment.Order != null)
                    payment.Order.PaymentStatus = "Failed";
            }

            var updated = await _repo.Update(payment);
            return Map(updated);
        }

        private PaymentResponseDTO Map(Payment p) => new PaymentResponseDTO
        {
            Id = p.Id,
            OrderId = p.OrderId,
            Method = p.Method,
            TransactionCode = p.TransactionCode,
            Amount = p.Amount,
            Status = p.Status,
            PaidAt = p.PaidAt
        };
    }
}
