using System.Threading.Tasks;
using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDTO> GenerateInvoice(int orderId, int currentUserId, bool isAdminOrStaff);
        Task<InvoiceResponseDTO> GetByOrderId(int orderId, int currentUserId, bool isAdminOrStaff);
    }
}
