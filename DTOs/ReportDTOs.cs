using System;
using System.Collections.Generic;

namespace TMPMS.DTOs
{
    public class RevenueReportRequestDTO
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        // Day, Month, Year
        public string GroupBy { get; set; } = "Day";
    }

    public class RevenuePointDTO
    {
        public string Period { get; set; }
        // Tổng doanh thu = ProductRevenue + AppointmentDepositRevenue (giữ field Revenue để
        // không phá các nơi FE đang đọc trực tiếp reportData.revenueTrend[i].revenue).
        public decimal Revenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal AppointmentDepositRevenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TopSellingMedicineDTO
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderStatusStatDTO
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class CategoryRevenueDTO
    {
        public string CategoryName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalQuantitySold { get; set; }
    }

    public class DashboardSummaryDTO
    {
        // TotalRevenue = ProductSalesRevenue + AppointmentDepositRevenue.
        public decimal TotalRevenue { get; set; }
        public decimal ProductSalesRevenue { get; set; }
        public decimal AppointmentDepositRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalAppointments { get; set; }
        public int PaidAppointments { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalMedicines { get; set; }
        public int PendingPrescriptions { get; set; }
        public int LowStockCount { get; set; }
        public List<RevenuePointDTO> RevenueTrend { get; set; } = new();
        public List<TopSellingMedicineDTO> TopSellingMedicines { get; set; } = new();
    }

    public class StaffRevenueDTO
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal AppointmentDepositRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AppointmentStatusStatDTO
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class PrescriptionStatusStatDTO
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class UserGrowthPointDTO
    {
        public string Period { get; set; }
        public int NewUsers { get; set; }
    }
}
