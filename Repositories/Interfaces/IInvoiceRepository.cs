using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<Invoice> Create(Invoice invoice);
        Task<Invoice> GetByOrderId(int orderId);
        Task<Order> GetOrderWithDetails(int orderId);
    }
}
