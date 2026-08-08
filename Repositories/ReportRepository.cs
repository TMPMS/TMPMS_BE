using BusinessObjects;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using TMPMS.Data;

namespace TMPMS.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly TMPMSDbContext _context;
        public ReportRepository(TMPMSDbContext context) => _context = context;

        public async Task<List<Order>> GetOrdersInRange(DateTime from, DateTime to)
        {
            return await _context.Orders
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
                .ToListAsync();
        }

        public async Task<List<OrderItem>> GetOrderItemsInRange(DateTime from, DateTime to)
        {
            // Chỉ tính các OrderItem thuộc đơn đã thanh toán ("Paid") — đồng nhất với cách
            // GetRevenueReport/GetDashboardSummary tính doanh thu, tránh lệch số giữa các báo cáo.
            return await _context.OrderItems
                .Include(oi => oi.Medicine).ThenInclude(m => m.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to && oi.Order.PaymentStatus == "Paid")
                .ToListAsync();
        }

        public async Task<int> CountMedicines()
        {
            return await _context.Medicines.CountAsync();
        }

        public async Task<int> CountPendingPrescriptions()
        {
            return await _context.Prescriptions.CountAsync(p => p.Status == "Pending");
        }

        public async Task<int> CountLowStockMedicines(int threshold)
        {
            return await _context.Medicines.CountAsync(m => m.StockQuantity <= threshold);
        }

        public async Task<List<Order>> GetAllOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<List<Appointment>> GetAppointmentsInRange(DateTime from, DateTime to)
        {
            return await _context.Appointments
                .Include(a => a.AppointmentPayments)
                .Where(a => a.CreatedAt >= from && a.CreatedAt <= to)
                .ToListAsync();
        }

        public async Task<List<Appointment>> GetAllAppointments()
        {
            return await _context.Appointments
                .Include(a => a.AppointmentPayments)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string>> GetStaffNames(IEnumerable<int> staffIds)
        {
            var ids = staffIds.Distinct().ToList();
            return await _context.Users
                .Where(u => ids.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.UserName ?? $"NV #{u.Id}");
        }
    }
}
