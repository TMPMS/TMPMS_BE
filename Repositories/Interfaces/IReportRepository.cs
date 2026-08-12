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
        Task<List<Appointment>> GetAppointmentsInRange(DateTime from, DateTime to);
        Task<List<Appointment>> GetAllAppointments();
        Task<Dictionary<int, string>> GetStaffNames(IEnumerable<int> staffIds);
        Task<List<string>> GetAllPrescriptionStatuses();
        Task<List<DateTime>> GetUserRegistrationDatesInRange(DateTime from, DateTime to);
    }
}
