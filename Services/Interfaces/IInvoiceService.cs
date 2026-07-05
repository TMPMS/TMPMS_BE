using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDTO> GenerateInvoice(int orderId);
        Task<InvoiceResponseDTO> GetByOrderId(int orderId);
    }
}
