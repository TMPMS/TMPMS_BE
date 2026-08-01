using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResponseDTO> CreatePayment(PaymentCreateDTO dto);
        Task<PaymentResponseDTO> GetById(int id);
        Task<List<PaymentResponseDTO>> GetByOrder(int orderId);
        Task<PaymentResponseDTO> UpdateStatus(int id, PaymentUpdateStatusDTO dto);
    }
}
