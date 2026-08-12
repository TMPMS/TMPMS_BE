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

        public async Task<List<string>> GetAllPrescriptionStatuses()
        {
            return await _context.Prescriptions.Select(p => p.Status).ToListAsync();
        }

        public async Task<List<DateTime>> GetUserRegistrationDatesInRange(DateTime from, DateTime to)
        {
            return await _context.Users
                .Where(u => u.CreatedAt >= from && u.CreatedAt <= to)
                .Select(u => u.CreatedAt)
                .ToListAsync();
        }

        // Giá trị tồn kho hiện tại theo kho — chỉ tính lô còn hàng ("Active") và có giá vốn ghi nhận.
        public async Task<List<(int WarehouseId, string WarehouseName, int TotalUnits, decimal TotalValue)>> GetInventoryValueByWarehouse()
        {
            var grouped = await _context.StockBatches
                .Include(b => b.Warehouse)
                .Where(b => b.Status == StockBatchStatus.Active && b.QuantityRemaining > 0)
                .GroupBy(b => new { b.WarehouseId, b.Warehouse.Name })
                .Select(g => new
                {
                    g.Key.WarehouseId,
                    WarehouseName = g.Key.Name,
                    TotalUnits = g.Sum(b => b.QuantityRemaining),
                    TotalValue = g.Sum(b => b.QuantityRemaining * (b.UnitCostPrice ?? 0))
                })
                .ToListAsync();

            return grouped.Select(g => (g.WarehouseId, g.WarehouseName, g.TotalUnits, g.TotalValue)).ToList();
        }
    }
}
