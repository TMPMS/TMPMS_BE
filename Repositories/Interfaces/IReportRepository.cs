using BusinessObjects;

namespace Repositories.Interfaces
{
    public interface IReportRepository
    {
        Task<List<Order>> GetOrdersInRange(DateTime from, DateTime to);
        Task<List<OrderItem>> GetOrderItemsInRange(DateTime from, DateTime to);
        Task<int> CountMedicines();
        Task<int> CountPendingPrescriptions();
        Task<int> CountLowStockMedicines(int threshold);
        Task<List<Order>> GetAllOrders();
    }
}
