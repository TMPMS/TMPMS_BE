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
            return await _context.OrderItems
                .Include(oi => oi.Medicine).ThenInclude(m => m.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
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
    }
}
