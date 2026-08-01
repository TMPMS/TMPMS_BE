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
        public decimal Revenue { get; set; }
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
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalMedicines { get; set; }
        public int PendingPrescriptions { get; set; }
        public int LowStockCount { get; set; }
        public List<RevenuePointDTO> RevenueTrend { get; set; } = new();
        public List<TopSellingMedicineDTO> TopSellingMedicines { get; set; } = new();
    }
}
