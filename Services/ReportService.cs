using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;

namespace TMPMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repo;
        private readonly IInventoryRepository _inventoryRepo;
        private const int LowStockThreshold = 20;

        public ReportService(IReportRepository repo, IInventoryRepository inventoryRepo)
        {
            _repo = repo;
            _inventoryRepo = inventoryRepo;
        }

        public async Task<List<RevenuePointDTO>> GetRevenueReport(RevenueReportRequestDTO dto)
        {
            var orders = await _repo.GetOrdersInRange(dto.FromDate, dto.ToDate);
            var paidOrders = orders.Where(o => o.PaymentStatus == "Paid").ToList();

            Func<Order, string> keySelector = dto.GroupBy switch
            {
                "Month" => o => o.CreatedAt.ToString("yyyy-MM"),
                "Year" => o => o.CreatedAt.ToString("yyyy"),
                _ => o => o.CreatedAt.ToString("yyyy-MM-dd")
            };

            return paidOrders
                .GroupBy(keySelector)
                .OrderBy(g => g.Key)
                .Select(g => new RevenuePointDTO
                {
                    Period = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                }).ToList();
        }

        public async Task<List<TopSellingMedicineDTO>> GetTopSellingMedicines(DateTime from, DateTime to, int top)
        {
            var items = await _repo.GetOrderItemsInRange(from, to);
            return items
                .GroupBy(oi => new { oi.MedicineId, Name = oi.Medicine?.Name })
                .Select(g => new TopSellingMedicineDTO
                {
                    MedicineId = g.Key.MedicineId,
                    MedicineName = g.Key.Name,
                    TotalQuantitySold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.Price)
                })
                .OrderByDescending(x => x.TotalQuantitySold)
                .Take(top)
                .ToList();
        }

        public async Task<List<OrderStatusStatDTO>> GetOrderStatusStatistics()
        {
            var orders = await _repo.GetAllOrders();
            return orders.GroupBy(o => o.Status)
                .Select(g => new OrderStatusStatDTO { Status = g.Key, Count = g.Count() })
                .ToList();
        }

        public async Task<List<CategoryRevenueDTO>> GetCategoryRevenue(DateTime from, DateTime to)
        {
            var items = await _repo.GetOrderItemsInRange(from, to);
            return items
                .Where(i => i.Medicine?.Category != null)
                .GroupBy(i => i.Medicine.Category.Name)
                .Select(g => new CategoryRevenueDTO
                {
                    CategoryName = g.Key,
                    TotalRevenue = g.Sum(x => x.Quantity * x.Price),
                    TotalQuantitySold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();
        }

        public async Task<DashboardSummaryDTO> GetDashboardSummary()
        {
            var now = DateTime.Now;
            var last30Days = now.AddDays(-30);

            var revenueTrend = await GetRevenueReport(new RevenueReportRequestDTO
            {
                FromDate = last30Days,
                ToDate = now,
                GroupBy = "Day"
            });

            var topSelling = await GetTopSellingMedicines(last30Days, now, 5);
            var allOrders = await _repo.GetAllOrders();
            var paidOrders = allOrders.Where(o => o.PaymentStatus == "Paid").ToList();

            return new DashboardSummaryDTO
            {
                TotalRevenue = paidOrders.Sum(o => o.TotalAmount),
                TotalOrders = allOrders.Count,
                TotalMedicines = await _repo.CountMedicines(),
                PendingPrescriptions = await _repo.CountPendingPrescriptions(),
                LowStockCount = await _repo.CountLowStockMedicines(LowStockThreshold),
                RevenueTrend = revenueTrend,
                TopSellingMedicines = topSelling
            };
        }
    }
}
