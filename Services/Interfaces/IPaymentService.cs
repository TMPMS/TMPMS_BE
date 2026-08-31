using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        // currentUserId/canProxy: chỉ chủ đơn hàng hoặc Admin/Accountant/Pharmacy mới được tạo/xem thanh
        // toán của 1 đơn — trước đây không kiểm tra, cho phép đọc/tạo payment của người khác (IDOR).
        Task<PaymentResponseDTO> CreatePayment(PaymentCreateDTO dto, int currentUserId, bool canProxy);
        Task<PaymentResponseDTO> GetById(int id, int currentUserId, bool canProxy);
        Task<List<PaymentResponseDTO>> GetByOrder(int orderId, int currentUserId, bool canProxy);
        Task<PaymentResponseDTO> UpdateStatus(int id, PaymentUpdateStatusDTO dto);
    }
}
