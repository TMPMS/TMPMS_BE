using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IPaymentRepository
    {
        Task<Payment> Create(Payment payment);
        Task<Payment> GetById(int id);
        Task<List<Payment>> GetByOrder(int orderId);
        Task<Payment> Update(Payment payment);
        Task<Order> GetOrderById(int orderId);
    }
}
