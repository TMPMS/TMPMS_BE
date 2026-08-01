using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        public PaymentService(IPaymentRepository repo) => _repo = repo;

        public async Task<PaymentResponseDTO> CreatePayment(PaymentCreateDTO dto)
        {
            var order = await _repo.GetOrderById(dto.OrderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại.");

            var allowedMethods = new[] { "Cash", "COD", "BankTransfer", "CreditCard", "MoMo", "VNPay", "PayOS" };
            if (!allowedMethods.Contains(dto.Method))
                throw new ArgumentException("Phương thức thanh toán không hợp lệ.");

            var payment = new Payment
            {
                OrderId = dto.OrderId,
                Method = dto.Method,
                Amount = dto.Amount,
                Status = "Pending",
                TransactionCode = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()
            };

            var created = await _repo.Create(payment);
            return Map(created);
        }

        public async Task<PaymentResponseDTO> GetById(int id)
        {
            var entity = await _repo.GetById(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<List<PaymentResponseDTO>> GetByOrder(int orderId)
        {
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

            payment.Status = dto.Status;
            if (!string.IsNullOrEmpty(dto.TransactionCode))
                payment.TransactionCode = dto.TransactionCode;

            if (dto.Status == "Success")
            {
                payment.PaidAt = DateTime.Now;
                if (payment.Order != null)
                    payment.Order.PaymentStatus = "Paid";
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
