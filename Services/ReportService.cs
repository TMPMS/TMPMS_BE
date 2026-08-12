using BusinessObjects;
using Repositories.Interfaces;
using Services.Interfaces;
using TMPMS.DTOs;
using TMPMS.Repositories.Interfaces;

namespace TMPMS.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repo;
        private readonly IInventoryRepository _inventoryRepo;
        private readonly IPatientRepository _patientRepo;
        private const int LowStockThreshold = 20;

        public ReportService(IReportRepository repo, IInventoryRepository inventoryRepo, IPatientRepository patientRepo)
        {
            _repo = repo;
            _inventoryRepo = inventoryRepo;
            _patientRepo = patientRepo;
        }

        private static string DateKey(DateTime d, string groupBy) => groupBy switch
        {
            "Month" => d.ToString("yyyy-MM"),
            "Year" => d.ToString("yyyy"),
            _ => d.ToString("yyyy-MM-dd")
        };

        public async Task<List<RevenuePointDTO>> GetRevenueReport(RevenueReportRequestDTO dto)
        {
            var orders = await _repo.GetOrdersInRange(dto.FromDate, dto.ToDate);
            var paidOrders = orders.Where(o => o.PaymentStatus == "Paid").ToList();

            // Doanh thu tiền cọc lịch hẹn (AppointmentPayments) trước đây KHÔNG được tính vào
            // báo cáo doanh thu — chỉ có doanh thu bán hàng (Orders) được thống kê. Gộp thêm ở đây
            // để dashboard phản ánh đúng toàn bộ dòng tiền vào (bán hàng + đặt cọc khám).
            var appointments = await _repo.GetAppointmentsInRange(dto.FromDate, dto.ToDate);
            var paidAppointments = appointments.Where(a => a.PaymentStatus == "Paid").ToList();

            var buckets = new Dictionary<string, RevenuePointDTO>();

            foreach (var g in paidOrders.GroupBy(o => DateKey(o.CreatedAt, dto.GroupBy)))
            {
                buckets[g.Key] = new RevenuePointDTO
                {
                    Period = g.Key,
                    ProductRevenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                };
            }

            foreach (var g in paidAppointments.GroupBy(a => DateKey(a.CreatedAt, dto.GroupBy)))
            {
                var depositSum = g.SelectMany(a => a.AppointmentPayments)
                    .Where(p => p.Status == "Paid")
                    .Sum(p => p.Amount);

                if (!buckets.TryGetValue(g.Key, out var point))
                {
                    point = new RevenuePointDTO { Period = g.Key };
                    buckets[g.Key] = point;
                }
                point.AppointmentDepositRevenue = depositSum;
            }

            foreach (var point in buckets.Values)
            {
                point.Revenue = point.ProductRevenue + point.AppointmentDepositRevenue;
            }

            return buckets.Values.OrderBy(p => p.Period).ToList();
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
            var activePatients = await _patientRepo.GetAllPatientsAsync();

            var allAppointments = await _repo.GetAllAppointments();
            var paidAppointments = allAppointments.Where(a => a.PaymentStatus == "Paid").ToList();
            var productSalesRevenue = paidOrders.Sum(o => o.TotalAmount);
            var appointmentDepositRevenue = paidAppointments
                .SelectMany(a => a.AppointmentPayments)
                .Where(p => p.Status == "Paid")
                .Sum(p => p.Amount);

            return new DashboardSummaryDTO
            {
                TotalRevenue = productSalesRevenue + appointmentDepositRevenue,
                ProductSalesRevenue = productSalesRevenue,
                AppointmentDepositRevenue = appointmentDepositRevenue,
                TotalOrders = allOrders.Count,
                TotalAppointments = allAppointments.Count,
                PaidAppointments = paidAppointments.Count,
                TotalCustomers = activePatients.Count,
                TotalMedicines = await _repo.CountMedicines(),
                PendingPrescriptions = await _repo.CountPendingPrescriptions(),
                LowStockCount = await _repo.CountLowStockMedicines(LowStockThreshold),
                RevenueTrend = revenueTrend,
                TopSellingMedicines = topSelling
            };
        }

        public async Task<List<StaffRevenueDTO>> GetStaffRevenue(DateTime from, DateTime to)
        {
            var orders = await _repo.GetOrdersInRange(from, to);
            var paidStaffOrders = orders.Where(o => o.PaymentStatus == "Paid" && o.ProcessedByStaffId != null).ToList();

            var appointments = await _repo.GetAppointmentsInRange(from, to);
            var paidStaffAppointments = appointments
                .Where(a => a.PaymentStatus == "Paid" && (a.ConfirmedByStaffId ?? a.StaffId) != null)
                .ToList();

            var staffIds = paidStaffOrders.Select(o => o.ProcessedByStaffId!.Value)
                .Concat(paidStaffAppointments.Select(a => (a.ConfirmedByStaffId ?? a.StaffId)!.Value))
                .Distinct()
                .ToList();
            var names = await _repo.GetStaffNames(staffIds);

            var result = new Dictionary<int, StaffRevenueDTO>();
            StaffRevenueDTO GetOrAdd(int staffId)
            {
                if (!result.TryGetValue(staffId, out var dto))
                {
                    dto = new StaffRevenueDTO { StaffId = staffId, StaffName = names.GetValueOrDefault(staffId, $"NV #{staffId}") };
                    result[staffId] = dto;
                }
                return dto;
            }

            foreach (var g in paidStaffOrders.GroupBy(o => o.ProcessedByStaffId!.Value))
            {
                GetOrAdd(g.Key).ProductRevenue = g.Sum(o => o.TotalAmount);
            }

            foreach (var g in paidStaffAppointments.GroupBy(a => (a.ConfirmedByStaffId ?? a.StaffId)!.Value))
            {
                GetOrAdd(g.Key).AppointmentDepositRevenue = g.SelectMany(a => a.AppointmentPayments)
                    .Where(p => p.Status == "Paid")
                    .Sum(p => p.Amount);
            }

            foreach (var dto in result.Values)
            {
                dto.TotalRevenue = dto.ProductRevenue + dto.AppointmentDepositRevenue;
            }

            return result.Values.OrderByDescending(d => d.TotalRevenue).ToList();
        }

        public async Task<List<AppointmentStatusStatDTO>> GetAppointmentStatusStatistics()
        {
            var appointments = await _repo.GetAllAppointments();
            return appointments.GroupBy(a => a.Status)
                .Select(g => new AppointmentStatusStatDTO { Status = g.Key, Count = g.Count() })
                .ToList();
        }

        public async Task<List<PrescriptionStatusStatDTO>> GetPrescriptionStatusStatistics()
        {
            var statuses = await _repo.GetAllPrescriptionStatuses();
            return statuses.GroupBy(s => s)
                .Select(g => new PrescriptionStatusStatDTO { Status = g.Key, Count = g.Count() })
                .ToList();
        }

        public async Task<List<UserGrowthPointDTO>> GetUserGrowth(DateTime from, DateTime to, string groupBy)
        {
            var dates = await _repo.GetUserRegistrationDatesInRange(from, to);
            return dates.GroupBy(d => DateKey(d, groupBy))
                .Select(g => new UserGrowthPointDTO { Period = g.Key, NewUsers = g.Count() })
                .OrderBy(p => p.Period)
                .ToList();
        }
    }
}
