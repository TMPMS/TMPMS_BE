using TMPMS.DTOs;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<List<RevenuePointDTO>> GetRevenueReport(RevenueReportRequestDTO dto);
        Task<List<TopSellingMedicineDTO>> GetTopSellingMedicines(DateTime from, DateTime to, int top);
        Task<List<OrderStatusStatDTO>> GetOrderStatusStatistics();
        Task<List<CategoryRevenueDTO>> GetCategoryRevenue(DateTime from, DateTime to);
        Task<DashboardSummaryDTO> GetDashboardSummary();
        Task<List<StaffRevenueDTO>> GetStaffRevenue(DateTime from, DateTime to);
        Task<List<AppointmentStatusStatDTO>> GetAppointmentStatusStatistics();
        Task<List<PrescriptionStatusStatDTO>> GetPrescriptionStatusStatistics();
        Task<List<UserGrowthPointDTO>> GetUserGrowth(DateTime from, DateTime to, string groupBy);
    }
}
